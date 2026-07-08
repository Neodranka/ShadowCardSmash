using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// Turn-flow state machine. Called by EndTurnAction (and at game start).
/// GDD §10: TurnStart → Draw → Main → TurnEnd → switch.
/// </summary>
public static class TurnManager
{
    public static void StartTurn(GameContext ctx, PlayerSide side)
    {
        var state = ctx.State;
        state.CurrentPlayer = side;
        state.Phase = GamePhase.TurnStart;
        state.TurnNumber++;

        var p = state.GetPlayer(side);
        p.MinionDestroyedThisTurn = false;
        p.SelfDamageThisTurn = 0;
        p.HasEvolvedThisTurn = false;

        // Mana growth (cap 10).
        if (p.MaxMana < GameState.ManaMax) p.MaxMana++;
        p.Mana = p.MaxMana;
        ctx.Loop.Publish(new ManaChangedEvent(side, p.Mana, p.MaxMana), ctx);

        // Evolution unlock: at start of P2's 4th turn (TurnNumber 8), BOTH players gain 3 EP. One-shot.
        if (state.TurnNumber == EvolutionSystem.EvolutionUnlockTurnAbsolute)
        {
            foreach (var pl in state.Players)
            {
                pl.EvolutionPoints += EvolutionSystem.EvolutionPointsAtUnlock;
                ctx.Loop.Publish(new EvolutionPointsGrantedEvent(pl.Side, EvolutionSystem.EvolutionPointsAtUnlock), ctx);
            }
        }

        // Refresh attack permissions for surviving minions + tick landmark/amulet activate cooldowns.
        foreach (var tile in p.Field)
        {
            if (tile.Occupant is not RuntimeCard m) continue;
            m.SummonedThisTurn = false;
            m.AttacksThisTurn = 0;
            m.CanAttackThisTurn = true;
            if (m.Counters.TryGetValue("activate_cd", out int cd) && cd > 0)
                m.Counters["activate_cd"] = cd - 1;
        }
        if (p.TerrainSlot.Occupant is { } tocc
            && tocc.Counters.TryGetValue("activate_cd", out int tcd) && tcd > 0)
            tocc.Counters["activate_cd"] = tcd - 1;

        // Tick amulet countdowns BEFORE draw (GDD §10 step 1).
        TickAmuletsAndTileEffects(ctx, side);
        if (ctx.Loop.IsGameOver) return;

        ctx.Loop.Publish(new TurnStartedEvent(side, state.TurnNumber), ctx);

        // Invoke OnOwnerTurnStart for every friendly minion still on the field.
        foreach (var tile in p.Field)
        {
            if (tile.Occupant is not { } m) continue;
            if (m.IsSilenced) continue;
            ctx.CardDatabase.Get(m.Card).OnOwnerTurnStart(ctx.WithSource(m));
        }

        // Draw phase. First apply any delayed bonus draws (拖延议程), then the normal 1 draw.
        state.Phase = GamePhase.Draw;
        if (p.NextTurnBonusDraws > 0)
        {
            int bonus = p.NextTurnBonusDraws;
            p.NextTurnBonusDraws = 0;
            EffectPrimitives.Draw(ctx, side, bonus);
            if (ctx.Loop.IsGameOver) return;
        }
        EffectPrimitives.Draw(ctx, side, 1);
        if (ctx.Loop.IsGameOver) return;

        state.Phase = GamePhase.Main;
        ctx.Loop.Publish(new PhaseChangedEvent(GamePhase.Main, side), ctx);
    }

    public static void EndTurn(GameContext ctx)
    {
        var side = ctx.State.CurrentPlayer;
        var p = ctx.State.GetPlayer(side);

        ctx.State.Phase = GamePhase.TurnEnd;

        foreach (var tile in p.Field)
        {
            if (tile.Occupant is not { } m) continue;
            if (m.IsSilenced) continue;
            ctx.CardDatabase.Get(m.Card).OnOwnerTurnEnd(ctx.WithSource(m));
        }

        // Decay duration-limited buffs.
        DecayBuffs(p);

        ctx.Loop.Publish(new TurnEndedEvent(side, ctx.State.TurnNumber), ctx);
        if (ctx.Loop.IsGameOver) return;

        StartTurn(ctx, side.Opponent());
    }

    private static void TickAmuletsAndTileEffects(GameContext ctx, PlayerSide side)
    {
        var p = ctx.State.GetPlayer(side);
        for (int i = 0; i < p.Field.Length; i++)
        {
            var tile = p.Field[i];

            // Tile effects countdown.
            for (int j = tile.Effects.Count - 1; j >= 0; j--)
            {
                var e = tile.Effects[j];
                e.RemainingTurns--;
                if (e.RemainingTurns <= 0) tile.Effects.RemoveAt(j);
            }

            // Amulet countdown.
            if (tile.Occupant is { } m && m.Countdown > 0)
            {
                m.Countdown--;
                ctx.Loop.Publish(new CountdownTickedEvent(m.Instance, m.Countdown), ctx);
                if (m.Countdown == 0)
                {
                    if (!m.IsSilenced)
                        ctx.CardDatabase.Get(m.Card).OnCountdownReachZero(ctx.WithSource(m));
                    EffectPrimitives.Destroy(ctx, m);
                }
            }
        }
    }

    private static void DecayBuffs(PlayerState p)
    {
        foreach (var tile in p.Field)
        {
            if (tile.Occupant is not { } m) continue;
            for (int i = m.Buffs.Count - 1; i >= 0; i--)
            {
                var b = m.Buffs[i];
                if (b.DurationTurns <= 0) continue; // permanent
                b.DurationTurns--;
                if (b.DurationTurns == 0)
                {
                    m.CurrentAttack = Math.Max(0, m.CurrentAttack - b.AttackDelta);
                    m.MaxHealth = Math.Max(1, m.MaxHealth - b.HealthDelta);
                    m.CurrentHealth = Math.Min(m.CurrentHealth, m.MaxHealth);
                    m.Buffs.RemoveAt(i);
                }
            }
        }
    }
}
