using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// The closed set of state-mutating primitives. Every card behavior is composed from these.
/// To extend: add a static method here, mutate state, publish a BoardEvent, push to event log.
/// All existing card scripts keep working untouched.
/// </summary>
public static class EffectPrimitives
{
    // ===== Damage =====

    public static bool DamageMinion(GameContext ctx, RuntimeCard target, int amount)
    {
        // 0 伤害也算"受到伤害"：仍然消耗屏障 + 触发事件流程；只有负数被拒。
        if (amount < 0 || target.Zone != Zone.Field) return false;

        if (target.BarrierStacks > 0)
        {
            target.BarrierStacks--;
            // 伤害事件照常触发（amount=0），让"受到伤害"型效果（如反击、抽牌）依然能响应屏障吸收。
            ctx.Loop.Publish(new MinionDamagedEvent(target.Instance, 0, ctx.Source?.Instance), ctx);
            var targetScript = ctx.CardDatabase.Get(target.Card);
            if (!target.IsSilenced) targetScript.OnDamaged(ctx.WithSource(target), 0, ctx.Source?.Instance);

            if (target.BarrierStacks == 0)
            {
                ctx.Loop.Publish(new BarrierLostEvent(target.Instance), ctx);
                if (!target.IsSilenced) targetScript.OnSelfBarrierLost(ctx.WithSource(target));
                DispatchAllyMinionBarrierLost(ctx, target);
            }
            return false;
        }

        target.CurrentHealth -= amount;
        ctx.Loop.Publish(new MinionDamagedEvent(target.Instance, amount, ctx.Source?.Instance), ctx);

        // Run target-side OnDamaged hook so the card can self-react (e.g., "when this is damaged, draw a card").
        var script = ctx.CardDatabase.Get(target.Card);
        script.OnDamaged(ctx.WithSource(target), amount, ctx.Source?.Instance);

        if (target.CurrentHealth <= 0)
        {
            Destroy(ctx, target);
            return true;
        }
        return false;
    }

    public static void DamagePlayer(GameContext ctx, PlayerSide target, int amount)
    {
        if (amount < 0) return;
        var p = ctx.State.GetPlayer(target);
        // Player barrier absorbs one whole damage instance regardless of amount (even 0).
        if (p.BarrierStacks > 0)
        {
            p.BarrierStacks--;
            ctx.Loop.Publish(new PlayerDamagedEvent(target, 0, ctx.Source?.Instance), ctx);
            if (p.BarrierStacks == 0)
                ctx.Loop.Publish(new PlayerBarrierLostEvent(target), ctx);
            return;
        }
        p.Health -= amount;
        ctx.Loop.Publish(new PlayerDamagedEvent(target, amount, ctx.Source?.Instance), ctx);
        ctx.Loop.CheckGameEnd();
    }

    /// <summary>
    /// Forsaken signature: damage to your own hero. Tracks counters for synergy conditions.
    /// </summary>
    public static void SelfDamagePlayer(GameContext ctx, PlayerSide target, int amount)
    {
        if (amount <= 0) return;
        var p = ctx.State.GetPlayer(target);
        p.Health -= amount;
        p.SelfDamageThisTurn += amount;
        p.TotalSelfDamage += amount;
        p.SelfDamageCount++;
        ctx.Loop.Publish(new PlayerDamagedEvent(target, amount, ctx.Source?.Instance), ctx);
        ctx.Loop.CheckGameEnd();
    }

    // ===== Healing =====

    public static void HealMinion(GameContext ctx, RuntimeCard target, int amount)
    {
        if (amount <= 0 || target.Zone != Zone.Field) return;
        int max = target.MaxHealth;
        int newHp = Math.Min(max, target.CurrentHealth + amount);
        int healed = newHp - target.CurrentHealth;
        target.CurrentHealth = newHp;
        if (healed > 0)
            ctx.Loop.Publish(new MinionHealedEvent(target.Instance, healed, ctx.Source?.Instance), ctx);
    }

    public static void HealPlayer(GameContext ctx, PlayerSide target, int amount)
    {
        if (amount <= 0) return;
        var p = ctx.State.GetPlayer(target);
        int newHp = Math.Min(p.MaxHealth, p.Health + amount);
        int healed = newHp - p.Health;
        p.Health = newHp;
        if (healed > 0)
            ctx.Loop.Publish(new PlayerHealedEvent(target, healed, ctx.Source?.Instance), ctx);
    }

    // ===== Card draw =====

    public static void Draw(GameContext ctx, PlayerSide side, int count)
    {
        var p = ctx.State.GetPlayer(side);
        int actuallyDrawn = 0;
        for (int i = 0; i < count; i++)
        {
            if (p.Deck.Count == 0)
            {
                p.FatigueCounter++;
                ctx.Loop.Publish(new FatigueEvent(side, p.FatigueCounter), ctx);
                // 摄政议会 (≥30 议案) redirects fatigue: the opponent eats the hits instead.
                var fatigueTarget = p.FatigueRedirected ? side.Opponent() : side;
                DamagePlayer(ctx, fatigueTarget, p.FatigueCounter);
                if (ctx.State.Result != GameResult.InProgress) return;
                continue;
            }

            var card = p.Deck[^1];
            p.Deck.RemoveAt(p.Deck.Count - 1);
            if (p.Hand.Count >= PlayerState.HandLimit)
            {
                p.Graveyard.Add(card);
                card.Zone = Zone.Graveyard;
                ctx.Loop.Publish(new CardOverdrawnEvent(side, card.Instance, card.Card), ctx);
            }
            else
            {
                card.Zone = Zone.Hand;
                p.Hand.Add(card);
                ctx.Loop.Publish(new CardDrawnEvent(side, card.Instance, card.Card), ctx);
                actuallyDrawn++;
            }
        }
        if (actuallyDrawn > 0) DispatchOwnerCardsDrawnBatch(ctx, side, actuallyDrawn);
    }

    /// <summary>Draw from deck until hand is full. 其余牌库牌不算抽 — 走 MillRestOfDeck 单独处理。</summary>
    public static int DrawUntilHandFull(GameContext ctx, PlayerSide side)
    {
        var p = ctx.State.GetPlayer(side);
        int target = PlayerState.HandLimit - p.Hand.Count;
        if (target <= 0) return 0;
        int before = p.Hand.Count;
        Draw(ctx, side, target);
        return p.Hand.Count - before;
    }

    /// <summary>Move remaining deck cards to graveyard (not a draw). 紧急议案 tail.</summary>
    public static void MillRestOfDeck(GameContext ctx, PlayerSide side)
    {
        var p = ctx.State.GetPlayer(side);
        int count = p.Deck.Count;
        if (count == 0) return;
        for (int i = p.Deck.Count - 1; i >= 0; i--)
        {
            var card = p.Deck[i];
            card.Zone = Zone.Graveyard;
            p.Graveyard.Add(card);
        }
        p.Deck.Clear();
        ctx.Loop.Publish(new DeckMilledEvent(side, count), ctx);
    }

    /// <summary>Random draw from graveyard. n draws; each pick uses ctx.Rng. Stops if graveyard empty.
    /// 复议 core. Cards drawn count toward CardDrawnEvent (triggers 会议记录员).</summary>
    public static int DrawFromGraveyardRandom(GameContext ctx, PlayerSide side, int n)
    {
        var p = ctx.State.GetPlayer(side);
        int drawn = 0;
        for (int i = 0; i < n; i++)
        {
            if (p.Graveyard.Count == 0) break;
            if (p.Hand.Count >= PlayerState.HandLimit) break;
            int idx = ctx.Rng.Next(0,p.Graveyard.Count);
            var card = p.Graveyard[idx];
            p.Graveyard.RemoveAt(idx);
            card.Zone = Zone.Hand;
            p.Hand.Add(card);
            ctx.Loop.Publish(new CardDrawnEvent(side, card.Instance, card.Card), ctx);
            drawn++;
        }
        if (drawn > 0) DispatchOwnerCardsDrawnBatch(ctx, side, drawn);
        return drawn;
    }

    /// <summary>Draw graveyard cards randomly until hand full. Returns drawn count. 复议 body.</summary>
    public static int DrawFromGraveyardUntilHandFull(GameContext ctx, PlayerSide side)
    {
        var p = ctx.State.GetPlayer(side);
        int target = PlayerState.HandLimit - p.Hand.Count;
        if (target <= 0) return 0;
        return DrawFromGraveyardRandom(ctx, side, target);
    }

    /// <summary>Move all remaining graveyard cards back into the deck (shuffle position). 复议 tail.</summary>
    public static void ReturnGraveyardToDeck(GameContext ctx, PlayerSide side)
    {
        var p = ctx.State.GetPlayer(side);
        int moved = p.Graveyard.Count;
        if (moved == 0) return;
        foreach (var c in p.Graveyard)
        {
            c.Zone = Zone.Deck;
            // Insert at random position for deterministic shuffle-in-place.
            int idx = ctx.Rng.Next(0,p.Deck.Count + 1);
            p.Deck.Insert(idx, c);
        }
        p.Graveyard.Clear();
        // Zone changes are internal; a single aggregate event is enough for animation.
        ctx.Loop.Publish(new CardZoneChangedEvent(new InstanceId(0), Zone.Graveyard, Zone.Deck), ctx);
    }

    /// <summary>Return a specific graveyard card to hand (阿尔文 OnEvolve).</summary>
    public static bool ReturnGraveyardCardToHand(GameContext ctx, RuntimeCard target)
    {
        var p = ctx.State.GetPlayer(target.Owner);
        if (!p.Graveyard.Remove(target)) return false;
        if (p.Hand.Count >= PlayerState.HandLimit)
        {
            p.Graveyard.Add(target);
            return false; // 手上限已满就啥都不做（不放墓地不放弃）
        }
        target.Zone = Zone.Hand;
        p.Hand.Add(target);
        ctx.Loop.Publish(new CardZoneChangedEvent(target.Instance, Zone.Graveyard, Zone.Hand), ctx);
        return true;
    }

    /// <summary>Shuffle a hand card back into deck. Triggers OnOwnerCardShuffledIntoDeck for the owner's
    /// field minions (议会秘书). Insertion position is randomized via ctx.Rng.</summary>
    public static void ShuffleHandToDeck(GameContext ctx, RuntimeCard card)
    {
        var p = ctx.State.GetPlayer(card.Owner);
        if (!p.Hand.Remove(card)) return;
        card.Zone = Zone.Deck;
        int idx = ctx.Rng.Next(0,p.Deck.Count + 1);
        p.Deck.Insert(idx, card);
        ctx.Loop.Publish(new CardShuffledIntoDeckEvent(card.Instance, card.Card, card.Owner), ctx);
        DispatchOwnerCardShuffledIntoDeck(ctx, card);
    }

    /// <summary>Put a hand card on top of the deck (last index in our list = draw target). 权衡 branch B.</summary>
    public static void PutHandCardOnTopOfDeck(GameContext ctx, RuntimeCard card)
    {
        var p = ctx.State.GetPlayer(card.Owner);
        if (!p.Hand.Remove(card)) return;
        card.Zone = Zone.Deck;
        p.Deck.Add(card); // last index = top per Draw() convention
        ctx.Loop.Publish(new CardShuffledIntoDeckEvent(card.Instance, card.Card, card.Owner), ctx);
        DispatchOwnerCardShuffledIntoDeck(ctx, card);
    }

    /// <summary>Refund mana: capped at current MaxMana. Fires ManaGainedEvent + OnOwnerManaGained.</summary>
    public static void RefundMana(GameContext ctx, PlayerSide side, int amount, string source)
    {
        if (amount <= 0) return;
        var p = ctx.State.GetPlayer(side);
        int newMana = Math.Min(p.MaxMana, p.Mana + amount);
        int gained = newMana - p.Mana;
        p.Mana = newMana;
        if (gained > 0)
        {
            ctx.Loop.Publish(new ManaChangedEvent(side, p.Mana, p.MaxMana), ctx);
            ctx.Loop.Publish(new ManaGainedEvent(side, gained, source), ctx);
            DispatchOwnerManaGained(ctx, side, gained, source);
        }
    }

    /// <summary>Grant mana: not capped at MaxMana (can exceed, but hard cap at ManaMax=10).
    /// Fires ManaGainedEvent + OnOwnerManaGained.</summary>
    public static void GrantMana(GameContext ctx, PlayerSide side, int amount, string source)
    {
        if (amount <= 0) return;
        var p = ctx.State.GetPlayer(side);
        int newMana = Math.Min(GameState.ManaMax, p.Mana + amount);
        int gained = newMana - p.Mana;
        p.Mana = newMana;
        if (gained > 0)
        {
            ctx.Loop.Publish(new ManaChangedEvent(side, p.Mana, p.MaxMana), ctx);
            ctx.Loop.Publish(new ManaGainedEvent(side, gained, source), ctx);
            DispatchOwnerManaGained(ctx, side, gained, source);
        }
    }

    /// <summary>Briefly flip a hand card face-up to both sides (玛丽斯卡 compare).</summary>
    public static void RevealCard(GameContext ctx, RuntimeCard card)
    {
        ctx.Loop.Publish(new CardRevealedEvent(card.Instance, card.Card, card.Owner), ctx);
    }

    /// <summary>Random-destroy N enemy minions (玛丽斯卡 win branch).</summary>
    public static int DestroyRandomEnemyMinions(GameContext ctx, PlayerSide side, int n)
    {
        var enemy = ctx.State.GetPlayer(side.Opponent());
        var candidates = new List<RuntimeCard>();
        foreach (var t in enemy.Field)
            if (t.Occupant is { } m) candidates.Add(m);
        int destroyed = 0;
        for (int i = 0; i < n && candidates.Count > 0; i++)
        {
            int idx = ctx.Rng.Next(0,candidates.Count);
            var target = candidates[idx];
            candidates.RemoveAt(idx);
            Destroy(ctx, target);
            destroyed++;
        }
        return destroyed;
    }

    /// <summary>Bump a player-side counter (议案). Emits PlayerCounterChangedEvent.</summary>
    public static void ChangePlayerCounter(GameContext ctx, PlayerSide side, string key, int delta)
    {
        var p = ctx.State.GetPlayer(side);
        p.Counters.TryGetValue(key, out int cur);
        int next = Math.Max(0, cur + delta);
        p.Counters[key] = next;
        ctx.Loop.Publish(new PlayerCounterChangedEvent(side, key, next), ctx);
    }

    /// <summary>Set player-side counter to zero (used by 摄政议会 activation).</summary>
    public static int ConsumePlayerCounter(GameContext ctx, PlayerSide side, string key)
    {
        var p = ctx.State.GetPlayer(side);
        if (!p.Counters.TryGetValue(key, out int cur) || cur == 0) return 0;
        p.Counters[key] = 0;
        ctx.Loop.Publish(new PlayerCounterChangedEvent(side, key, 0), ctx);
        return cur;
    }

    public static void Discard(GameContext ctx, PlayerSide side, RuntimeCard card)
    {
        var p = ctx.State.GetPlayer(side);
        if (!p.Hand.Remove(card)) return;
        p.Graveyard.Add(card);
        var from = card.Zone;
        card.Zone = Zone.Graveyard;
        ctx.Loop.Publish(new CardZoneChangedEvent(card.Instance, from, Zone.Graveyard), ctx);
    }

    // ===== Summon / placement =====

    public static InstanceId? Summon(GameContext ctx, CardId cardId, PlayerSide side, int? tileIndex = null)
    {
        var p = ctx.State.GetPlayer(side);
        int idx;
        if (tileIndex.HasValue)
        {
            if (tileIndex.Value < 0 || tileIndex.Value >= PlayerState.FieldSize || !p.Field[tileIndex.Value].IsEmpty)
                return null;
            idx = tileIndex.Value;
        }
        else
        {
            if (!p.TryGetEmptyTileIndex(out idx))
            {
                // 召唤定义：空间不足时无法被召唤的卡牌直接消失（进入放逐区）。
                var ghost = new RuntimeCard
                {
                    Instance = ctx.State.AllocateInstanceId(),
                    Card = cardId,
                    Owner = side,
                    Zone = Zone.Vanished,
                };
                p.Vanished.Add(ghost);
                ctx.Loop.Publish(new MinionVanishedEvent(ghost.Instance, cardId, side), ctx);
                return null;
            }
        }

        var script = ctx.CardDatabase.Get(cardId);
        var card = new RuntimeCard
        {
            Instance = ctx.State.AllocateInstanceId(),
            Card = cardId,
            Owner = side,
            Zone = Zone.Field,
            CurrentAttack = script.BaseAttack,
            CurrentHealth = script.BaseHealth,
            MaxHealth = script.BaseHealth,
            Keywords = script.InitialKeywords,
            BarrierStacks = script.InitialKeywords.HasFlag(Keyword.Barrier) ? 1 : 0,
            Countdown = script.InitialCountdown,
            CanAttackThisTurn = script.InitialKeywords.HasFlag(Keyword.Storm)
                              || script.InitialKeywords.HasFlag(Keyword.Rush),
            SummonedThisTurn = true,
        };
        p.Field[idx].Occupant = card;

        if (script.CardType == CardType.Amulet)
            ctx.Loop.Publish(new AmuletPlacedEvent(side, card.Instance, cardId, idx), ctx);
        else
            ctx.Loop.Publish(new MinionSummonedEvent(side, card.Instance, cardId, idx), ctx);

        if (script.CardType == CardType.Minion) NotifyMinionEntered(ctx, card);

        return card.Instance;
    }

    // ===== Buffs / keywords =====

    public static void Buff(GameContext ctx, RuntimeCard target, int atk, int hp, int duration)
    {
        if (target.Zone != Zone.Field) return;
        target.CurrentAttack = Math.Max(0, target.CurrentAttack + atk);
        target.MaxHealth = Math.Max(1, target.MaxHealth + hp);
        target.CurrentHealth = Math.Max(1, target.CurrentHealth + hp);
        target.Buffs.Add(new BuffData
        {
            AttackDelta = atk,
            HealthDelta = hp,
            DurationTurns = duration,
            Source = ctx.Source?.Instance ?? InstanceId.None,
        });
        ctx.Loop.Publish(new BuffAppliedEvent(target.Instance, atk, hp), ctx);
    }

    public static void GainKeyword(GameContext ctx, RuntimeCard target, Keyword kw)
    {
        if (target.Zone != Zone.Field || target.HasKeyword(kw)) return;
        target.AddKeyword(kw);
        if (kw == Keyword.Storm || kw == Keyword.Rush) target.CanAttackThisTurn = true;
        ctx.Loop.Publish(new KeywordGainedEvent(target.Instance, kw), ctx);
    }

    public static void Silence(GameContext ctx, RuntimeCard target)
    {
        if (target.Zone != Zone.Field) return;
        target.IsSilenced = true;
        target.Keywords = Keyword.None;
        ctx.Loop.Publish(new SilenceAppliedEvent(target.Instance), ctx);
    }

    // ===== Destroy / vanish =====

    public static void Destroy(GameContext ctx, RuntimeCard target)
    {
        if (target.Zone != Zone.Field) return;

        var p = ctx.State.GetPlayer(target.Owner);
        int? tileIdx = null;
        for (int i = 0; i < p.Field.Length; i++)
        {
            if (p.Field[i].Occupant?.Instance == target.Instance)
            {
                tileIdx = i;
                p.Field[i].Occupant = null;
                break;
            }
        }
        // Also clear the dedicated terrain slot if the target was the terrain occupant.
        if (tileIdx is null && p.TerrainSlot.Occupant?.Instance == target.Instance)
        {
            tileIdx = PlayerState.TerrainSlotIndex;
            p.TerrainSlot.Occupant = null;
        }

        var script = ctx.CardDatabase.Get(target.Card);
        target.Zone = Zone.Graveyard;
        p.Graveyard.Add(target);
        p.MinionDestroyedThisTurn = true;

        if (script.CardType == CardType.Amulet || script.CardType == CardType.Terrain)
            ctx.Loop.Publish(new AmuletDestroyedEvent(target.Instance, target.Card, target.Owner, tileIdx ?? -1), ctx);
        else
            ctx.Loop.Publish(new MinionDestroyedEvent(target.Instance, target.Card, target.Owner, tileIdx), ctx);

        // Run death hook unless silenced or explicitly suppressed (spawned copies).
        if (!target.IsSilenced && !target.OnDeathSuppressed) script.OnDeath(ctx.WithSource(target));
    }

    public static void Vanish(GameContext ctx, RuntimeCard target)
    {
        if (target.Zone != Zone.Field) return;
        var p = ctx.State.GetPlayer(target.Owner);
        for (int i = 0; i < p.Field.Length; i++)
            if (p.Field[i].Occupant?.Instance == target.Instance) { p.Field[i].Occupant = null; break; }
        target.Zone = Zone.Vanished;
        p.Vanished.Add(target);
        ctx.Loop.Publish(new MinionVanishedEvent(target.Instance, target.Card, target.Owner), ctx);
    }

    // ===== Mana / hand =====

    public static void GainMana(GameContext ctx, PlayerSide side, int amount, bool thisTurnOnly)
    {
        var p = ctx.State.GetPlayer(side);
        if (thisTurnOnly)
        {
            p.Mana = Math.Min(GameState.ManaMax, p.Mana + amount);
        }
        else
        {
            p.MaxMana = Math.Min(GameState.ManaMax, p.MaxMana + amount);
            p.Mana = Math.Min(GameState.ManaMax, p.Mana + amount);
        }
        ctx.Loop.Publish(new ManaChangedEvent(side, p.Mana, p.MaxMana), ctx);
    }

    public static void AddToHand(GameContext ctx, PlayerSide side, CardId cardId)
    {
        var p = ctx.State.GetPlayer(side);
        var card = new RuntimeCard
        {
            Instance = ctx.State.AllocateInstanceId(),
            Card = cardId,
            Owner = side,
            Zone = Zone.Hand,
        };
        if (p.Hand.Count >= PlayerState.HandLimit)
        {
            card.Zone = Zone.Graveyard;
            p.Graveyard.Add(card);
            ctx.Loop.Publish(new CardOverdrawnEvent(side, card.Instance, cardId), ctx);
            return;
        }
        p.Hand.Add(card);
        ctx.Loop.Publish(new CardDrawnEvent(side, card.Instance, cardId), ctx);
    }

    public static void ApplyTileEffect(GameContext ctx, PlayerSide side, int tileIndex, string key, int value, int duration)
    {
        var p = ctx.State.GetPlayer(side);
        if (tileIndex < 0 || tileIndex >= PlayerState.FieldSize) return;
        p.Field[tileIndex].Effects.Add(new TileEffect
        {
            EffectKey = key,
            Value = value,
            RemainingTurns = duration,
            Source = ctx.Source?.Instance ?? InstanceId.None,
        });
        ctx.Loop.Publish(new TileEffectAppliedEvent(side, tileIndex, key, duration), ctx);
    }

    // ===== Barrier =====
    // V1 设计：屏障是二值（有/无），同时只能 1 层。重复获取无效。
    // BarrierStacks 字段类型保持 int 以便将来扩展为多层屏障，但当前所有路径都 cap 到 1。

    public static void GiveBarrier(GameContext ctx, RuntimeCard target, int stacks = 1)
    {
        if (stacks <= 0 || target.Zone != Zone.Field) return;
        if (target.BarrierStacks > 0) return; // 已有屏障 → no-op
        target.BarrierStacks = 1;
        target.AddKeyword(Keyword.Barrier);
        ctx.Loop.Publish(new BarrierGainedEvent(target.Instance, 1), ctx);
    }

    public static void GiveBarrierToPlayer(GameContext ctx, PlayerSide side, int stacks = 1)
    {
        if (stacks <= 0) return;
        var p = ctx.State.GetPlayer(side);
        if (p.BarrierStacks > 0) return; // 已有屏障 → no-op
        p.BarrierStacks = 1;
        ctx.Loop.Publish(new PlayerBarrierGainedEvent(side, 1), ctx);
    }

    // ===== Search / structured draw =====

    /// <summary>Move the first deck card matching <paramref name="predicate"/> to hand. Returns its instance, or null.</summary>
    public static InstanceId? DrawSpecificFromDeck(GameContext ctx, PlayerSide side,
        System.Func<ICardScript, bool> predicate)
    {
        var p = ctx.State.GetPlayer(side);
        for (int i = p.Deck.Count - 1; i >= 0; i--)
        {
            var card = p.Deck[i];
            if (!predicate(ctx.CardDatabase.Get(card.Card))) continue;
            p.Deck.RemoveAt(i);
            if (p.Hand.Count >= PlayerState.HandLimit)
            {
                card.Zone = Zone.Graveyard;
                p.Graveyard.Add(card);
                ctx.Loop.Publish(new CardOverdrawnEvent(side, card.Instance, card.Card), ctx);
            }
            else
            {
                card.Zone = Zone.Hand;
                p.Hand.Add(card);
                ctx.Loop.Publish(new CardDrawnEvent(side, card.Instance, card.Card), ctx);
            }
            return card.Instance;
        }
        return null;
    }

    /// <summary>
    /// Iterate the owner's deck, pick at most one card per unique CardId matching the predicate, and Summon each
    /// onto the field until tiles run out. Used by 拉多万's OnPlay (unique-named 布伦哈尔 minions).
    /// </summary>
    public static int SummonUniqueFromDeck(GameContext ctx, PlayerSide side, System.Func<ICardScript, bool> predicate)
    {
        var p = ctx.State.GetPlayer(side);
        var seen = new HashSet<CardId>();
        var candidates = new List<RuntimeCard>();
        foreach (var card in p.Deck)
        {
            if (seen.Contains(card.Card)) continue;
            var script = ctx.CardDatabase.Get(card.Card);
            if (script.CardType != CardType.Minion) continue;
            if (!predicate(script)) continue;
            seen.Add(card.Card);
            candidates.Add(card);
        }
        int summoned = 0;
        foreach (var card in candidates)
        {
            if (!p.TryGetEmptyTileIndex(out _)) break;
            p.Deck.Remove(card);
            if (Summon(ctx, card.Card, side) is not null) summoned++;
        }
        return summoned;
    }

    // ===== Dispatch helpers =====

    /// <summary>Iterate ALL field minions on both sides and fire OnFieldMinionEntered on their scripts.</summary>
    public static void NotifyMinionEntered(GameContext ctx, RuntimeCard newcomer)
    {
        for (int s = 0; s < 2; s++)
        {
            foreach (var tile in ctx.State.Players[s].Field)
            {
                if (tile.Occupant is not { } m) continue;
                if (m.IsSilenced) continue;
                var script = ctx.CardDatabase.Get(m.Card);
                script.OnFieldMinionEntered(ctx.WithSource(m), newcomer);
            }
        }
    }

    private static void DispatchAllyMinionBarrierLost(GameContext ctx, RuntimeCard target)
    {
        for (int s = 0; s < 2; s++)
        {
            foreach (var tile in ctx.State.Players[s].Field)
            {
                if (tile.Occupant is not { } m) continue;
                if (m.Instance == target.Instance) continue;
                if (m.IsSilenced) continue;
                var script = ctx.CardDatabase.Get(m.Card);
                script.OnFieldMinionBarrierLost(ctx.WithSource(m), target);
            }
        }
    }

    /// <summary>Fire OnOwnerCardsDrawnBatch on every field card belonging to <paramref name="side"/>.
    /// Fires exactly ONCE regardless of draw count (会议记录员 rule).</summary>
    public static void DispatchOwnerCardsDrawnBatch(GameContext ctx, PlayerSide side, int count)
    {
        var p = ctx.State.GetPlayer(side);
        foreach (var tile in p.Field)
        {
            if (tile.Occupant is not { } m) continue;
            if (m.IsSilenced) continue;
            ctx.CardDatabase.Get(m.Card).OnOwnerCardsDrawnBatch(ctx.WithSource(m), count);
        }
        if (p.TerrainSlot.Occupant is { } tocc && !tocc.IsSilenced)
            ctx.CardDatabase.Get(tocc.Card).OnOwnerCardsDrawnBatch(ctx.WithSource(tocc), count);
    }

    public static void DispatchOwnerCardShuffledIntoDeck(GameContext ctx, RuntimeCard shuffled)
    {
        var p = ctx.State.GetPlayer(shuffled.Owner);
        foreach (var tile in p.Field)
        {
            if (tile.Occupant is not { } m) continue;
            if (m.IsSilenced) continue;
            ctx.CardDatabase.Get(m.Card).OnOwnerCardShuffledIntoDeck(ctx.WithSource(m), shuffled);
        }
        if (p.TerrainSlot.Occupant is { } tocc && !tocc.IsSilenced)
            ctx.CardDatabase.Get(tocc.Card).OnOwnerCardShuffledIntoDeck(ctx.WithSource(tocc), shuffled);
    }

    public static void DispatchOwnerManaGained(GameContext ctx, PlayerSide side, int amount, string source)
    {
        var p = ctx.State.GetPlayer(side);
        foreach (var tile in p.Field)
        {
            if (tile.Occupant is not { } m) continue;
            if (m.IsSilenced) continue;
            ctx.CardDatabase.Get(m.Card).OnOwnerManaGained(ctx.WithSource(m), amount, source);
        }
        if (p.TerrainSlot.Occupant is { } tocc && !tocc.IsSilenced)
            ctx.CardDatabase.Get(tocc.Card).OnOwnerManaGained(ctx.WithSource(tocc), amount, source);
    }
}
