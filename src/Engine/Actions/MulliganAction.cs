using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

public sealed record MulliganAction(PlayerSide Issuer, int[] SwapIndices) : IGameAction
{
    public ActionResult Validate(GameState state)
    {
        if (state.Phase != GamePhase.Mulligan) return ActionResult.Fail("Not in mulligan phase.");
        if (state.Mulligan.Confirmed[(int)Issuer]) return ActionResult.Fail("Already confirmed.");
        var hand = state.GetPlayer(Issuer).Hand;
        var seen = new HashSet<int>();
        foreach (var i in SwapIndices)
        {
            if (i < 0 || i >= hand.Count) return ActionResult.Fail($"Index {i} out of hand.");
            if (!seen.Add(i)) return ActionResult.Fail($"Duplicate index {i}.");
        }
        return ActionResult.Ok();
    }

    public void Apply(GameContext ctx)
    {
        MulliganManager.Apply(ctx, Issuer, SwapIndices);
        if (MulliganManager.BothConfirmed(ctx.State))
            MulliganManager.FinalizeAndStart(ctx);
    }
}
