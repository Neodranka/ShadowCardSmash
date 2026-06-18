using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// GDD §7: evolution. Second player has 3 EP starting turn 4.
/// Manual evolve: +2/+2 and gain Rush. Auto-evolve (via card effect) bypasses EP cost and per-turn limit.
/// </summary>
public static class EvolutionSystem
{
    public const int InitialEvolutionPoints = 3;
    public const int EvolutionAttackBoost = 2;
    public const int EvolutionHealthBoost = 2;
    public const int EvolutionUnlockTurnForSecond = 4;

    public static bool CanManuallyEvolve(GameState state, PlayerSide side)
    {
        if (side != PlayerSide.Second) return false; // only second player has EP in GDD §7.1
        if (state.TurnNumber < EvolutionUnlockTurnForSecond) return false;
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
