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
        if (amount <= 0 || target.Zone != Zone.Field) return false;

        if (target.BarrierStacks > 0)
        {
            target.BarrierStacks--;
            ctx.Loop.Publish(new MinionDamagedEvent(target.Instance, 0, ctx.Source?.Instance), ctx);
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
        if (amount <= 0) return;
        var p = ctx.State.GetPlayer(target);
        p.Health -= amount;
        ctx.Loop.Publish(new PlayerDamagedEvent(target, amount, ctx.Source?.Instance), ctx);
        ctx.Loop.CheckGameEnd();
    }

    /// <summary>
    /// Vampire signature: damage to your own hero. Tracks counters for synergy conditions.
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
        for (int i = 0; i < count; i++)
        {
            if (p.Deck.Count == 0)
            {
                p.FatigueCounter++;
                ctx.Loop.Publish(new FatigueEvent(side, p.FatigueCounter), ctx);
                DamagePlayer(ctx, side, p.FatigueCounter);
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
            }
        }
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
            if (!p.TryGetEmptyTileIndex(out idx)) return null;
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
            Countdown = script.InitialCountdown,
            CanAttackThisTurn = script.InitialKeywords.HasFlag(Keyword.Storm),
            SummonedThisTurn = true,
        };
        p.Field[idx].Occupant = card;

        if (script.CardType == CardType.Amulet)
            ctx.Loop.Publish(new AmuletPlacedEvent(side, card.Instance, cardId, idx), ctx);
        else
            ctx.Loop.Publish(new MinionSummonedEvent(side, card.Instance, cardId, idx), ctx);

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

        // Run death hook unless silenced (silence removes onDeath/onDeath-like effects).
        if (!target.IsSilenced) script.OnDeath(ctx.WithSource(target));
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
}
