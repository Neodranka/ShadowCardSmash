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
        if (state.Phase != GamePhase.Main) return ActionResult.Fail("不在主要阶段");
        if (state.CurrentPlayer != Issuer) return ActionResult.Fail("不是你的回合");

        var attacker = state.GetPlayer(Issuer).FindOnField(Attacker);
        if (attacker is null) return ActionResult.Fail("攻击随从不在己方场上");
        if (!attacker.CanAttackThisTurn) return ActionResult.Fail("此随从本回合不能攻击");
        if (attacker.CurrentAttack <= 0) return ActionResult.Fail("攻击力为 0，无法攻击");

        if (TargetMinion.HasValue == TargetPlayer.HasValue)
            return ActionResult.Fail("攻击目标必须是随从或英雄之一");

        if (TargetMinion.HasValue)
        {
            var defender = state.GetPlayer(Issuer.Opponent()).FindOnField(TargetMinion.Value);
            if (defender is null) return ActionResult.Fail("目标随从不在敌方场上");

            // Ward enforcement: if any enemy minion has Ward, the target must have Ward.
            if (CombatResolver.EnemyHasWard(state, Issuer) && !defender.HasKeyword(Keyword.Ward))
                return ActionResult.Fail("对方有守护，必须先攻击守护随从");
        }
        else
        {
            if (TargetPlayer != Issuer.Opponent()) return ActionResult.Fail("只能攻击敌方英雄");

            // Rush blocks face attack on the summon turn.
            if (attacker.SummonedThisTurn && attacker.HasKeyword(Keyword.Rush) && !attacker.HasKeyword(Keyword.Storm))
                return ActionResult.Fail("突进随从在召唤回合不能攻击英雄");

            if (CombatResolver.EnemyHasWard(state, Issuer))
                return ActionResult.Fail("对方有守护，必须先攻击守护随从");
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
