using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// GDD §7: evolution. Second player has 3 EP starting turn 4.
/// Manual evolve: +2/+2 and gain Rush. Auto-evolve (via card effect) bypasses EP cost and per-turn limit.
/// </summary>
public static class EvolutionSystem
{
    /// <summary>EP granted at unlock time to BOTH players.</summary>
    public const int EvolutionPointsAtUnlock = 3;
    public const int EvolutionAttackBoost = 2;
    public const int EvolutionHealthBoost = 2;
    /// <summary>
    /// Absolute TurnNumber at which both players' EP unlock — corresponds to the second player's 4th turn
    /// (TurnNumber 1=P1 turn 1, 2=P2 turn 1, ..., 8=P2 turn 4).
    /// </summary>
    public const int EvolutionUnlockTurnAbsolute = 8;

    public static bool CanManuallyEvolve(GameState state, PlayerSide side)
    {
        var p = state.GetPlayer(side);
        if (p.EvolutionPoints <= 0) return false;
        if (p.HasEvolvedThisTurn) return false;
        return true;
    }

    public static void Evolve(GameContext ctx, RuntimeCard target, bool consumesEP)
    {
        if (target.IsEvolved) return;
        var script = ctx.CardDatabase.Get(target.Card);
        if (script.CardType != CardType.Minion) return;

        target.IsEvolved = true;
        target.CurrentAttack = Math.Max(script.EvolvedAttack, target.CurrentAttack + EvolutionAttackBoost);
        target.MaxHealth = Math.Max(script.EvolvedHealth, target.MaxHealth + EvolutionHealthBoost);
        target.CurrentHealth = Math.Min(target.MaxHealth, target.CurrentHealth + EvolutionHealthBoost);
        target.AddKeyword(Keyword.Rush);
        // Evolving fully lifts summoning sickness — minion may attack anything immediately (including hero).
        target.SummonedThisTurn = false;
        target.CanAttackThisTurn = true;

        if (consumesEP)
        {
            var p = ctx.State.GetPlayer(target.Owner);
            p.EvolutionPoints--;
            p.HasEvolvedThisTurn = true;
        }

        ctx.Loop.Publish(new MinionEvolvedEvent(target.Instance), ctx);

        if (!target.IsSilenced) script.OnEvolve(ctx.WithSource(target));
    }
}
