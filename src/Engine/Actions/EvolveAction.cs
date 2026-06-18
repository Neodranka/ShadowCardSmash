using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

public sealed record EvolveAction(PlayerSide Issuer, InstanceId Target) : IGameAction
{
    public ActionResult Validate(GameState state)
    {
        if (state.Phase != GamePhase.Main) return ActionResult.Fail("Not in main phase.");
        if (state.CurrentPlayer != Issuer) return ActionResult.Fail("Not your turn.");
        if (!EvolutionSystem.CanManuallyEvolve(state, Issuer)) return ActionResult.Fail("Cannot evolve right now.");
        var target = state.GetPlayer(Issuer).FindOnField(Target);
        if (target is null) return ActionResult.Fail("Target not on your field.");
        if (target.IsEvolved) return ActionResult.Fail("Already evolved.");
        return ActionResult.Ok();
    }

    public void Apply(GameContext ctx)
    {
        var target = ctx.State.GetPlayer(Issuer).FindOnField(Target)!;
        EvolutionSystem.Evolve(ctx, target, consumesEP: true);
    }
}
