using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

public sealed record EndTurnAction(PlayerSide Issuer) : IGameAction
{
    public ActionResult Validate(GameState state)
    {
        if (state.Phase != GamePhase.Main) return ActionResult.Fail("Not in main phase.");
        if (state.CurrentPlayer != Issuer) return ActionResult.Fail("Not your turn.");
        return ActionResult.Ok();
    }

    public void Apply(GameContext ctx) => TurnManager.EndTurn(ctx);
}
