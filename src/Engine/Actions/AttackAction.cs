using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

public sealed record AttackAction(
    PlayerSide Issuer,
    InstanceId Attacker,
    InstanceId? TargetMinion,
    PlayerSide? TargetPlayer
) : IGameAction
{
    public ActionResult Validate(GameState state)
    {
        if (state.Phase != GamePhase.Main) return ActionResult.Fail("Not in main phase.");
        if (state.CurrentPlayer != Issuer) return ActionResult.Fail("Not your turn.");

        var attacker = state.GetPlayer(Issuer).FindOnField(Attacker);
        if (attacker is null) return ActionResult.Fail("Attacker not on your field.");
        if (!attacker.CanAttackThisTurn) return ActionResult.Fail("Summoning sickness or already attacked.");
        if (attacker.CurrentAttack <= 0) return ActionResult.Fail("Attacker has 0 attack.");

        if (TargetMinion.HasValue == TargetPlayer.HasValue)
            return ActionResult.Fail("Specify exactly one target (minion XOR player).");

        if (TargetMinion.HasValue)
        {
            var defender = state.GetPlayer(Issuer.Opponent()).FindOnField(TargetMinion.Value);
            if (defender is null) return ActionResult.Fail("Target minion not on enemy field.");

            // Ward enforcement: if any enemy minion has Ward, the target must have Ward.
            if (CombatResolver.EnemyHasWard(state, Issuer) && !defender.HasKeyword(Keyword.Ward))
                return ActionResult.Fail("Must attack ward minion first.");

            // Rush: can attack minions but not face on first turn.
            // Storm: can attack anything immediately. Both are handled in CanAttackThisTurn already.
        }
        else
        {
            if (TargetPlayer != Issuer.Opponent()) return ActionResult.Fail("Must attack enemy hero.");

            // Rush blocks face attack on the summon turn.
            if (attacker.SummonedThisTurn && attacker.HasKeyword(Keyword.Rush) && !attacker.HasKeyword(Keyword.Storm))
                return ActionResult.Fail("Rush cannot attack hero on summon turn.");

            if (CombatResolver.EnemyHasWard(state, Issuer))
                return ActionResult.Fail("Must attack ward minion first.");
        }
        return ActionResult.Ok();
    }

    public void Apply(GameContext ctx)
    {
        var attacker = ctx.State.GetPlayer(Issuer).FindOnField(Attacker)!;
        if (TargetMinion.HasValue)
        {
            var defender = ctx.State.GetPlayer(Issuer.Opponent()).FindOnField(TargetMinion.Value)!;
            CombatResolver.MinionVsMinion(ctx, attacker, defender);
        }
        else
        {
            CombatResolver.MinionVsPlayer(ctx, attacker, TargetPlayer!.Value);
        }
    }
}
