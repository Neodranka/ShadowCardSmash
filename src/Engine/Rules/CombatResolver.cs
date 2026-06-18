using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// GDD §5: attacker and defender exchange damage simultaneously.
/// Ward enforcement is checked in AttackAction.Validate, not here.
/// </summary>
public static class CombatResolver
{
    public static void MinionVsMinion(GameContext ctx, RuntimeCard attacker, RuntimeCard defender)
    {
        int atk = attacker.CurrentAttack;
        int def = defender.CurrentAttack;

        ctx.Loop.Publish(new MinionAttacksEvent(attacker.Instance, defender.Instance, null), ctx);
        ctx.CardDatabase.Get(attacker.Card).OnAttack(ctx.WithSource(attacker), defender, null);

        // Simultaneous strike.
        EffectPrimitives.DamageMinion(ctx.WithSource(attacker), defender, atk);
        EffectPrimitives.DamageMinion(ctx.WithSource(defender), attacker, def);

        attacker.AttacksThisTurn++;
        attacker.CanAttackThisTurn = false;
    }

    public static void MinionVsPlayer(GameContext ctx, RuntimeCard attacker, PlayerSide defender)
    {
        ctx.Loop.Publish(new MinionAttacksEvent(attacker.Instance, null, defender), ctx);
        ctx.CardDatabase.Get(attacker.Card).OnAttack(ctx.WithSource(attacker), null, defender);

        EffectPrimitives.DamagePlayer(ctx.WithSource(attacker), defender, attacker.CurrentAttack);

        attacker.AttacksThisTurn++;
        attacker.CanAttackThisTurn = false;
    }

    public static bool EnemyHasWard(GameState state, PlayerSide attackerSide)
    {
        var enemy = state.GetPlayer(attackerSide.Opponent());
        foreach (var t in enemy.Field)
            if (t.Occupant is { } m && m.HasKeyword(Keyword.Ward)) return true;
        return false;
    }
}
