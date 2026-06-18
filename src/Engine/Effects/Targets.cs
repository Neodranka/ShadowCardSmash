using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// Function library for selecting minions/players from a GameContext.
/// Cards call these in their hooks: ctx.AllEnemyMinions(), Targets.RandomMinion(ctx, side), etc.
/// </summary>
public static class Targets
{
    public static IEnumerable<RuntimeCard> AllMinions(GameContext ctx, PlayerSide side)
    {
        var p = ctx.State.GetPlayer(side);
        foreach (var t in p.Field) if (t.Occupant is { } m) yield return m;
    }

    public static IEnumerable<RuntimeCard> AllMinions(GameContext ctx)
    {
        foreach (var m in AllMinions(ctx, PlayerSide.First)) yield return m;
        foreach (var m in AllMinions(ctx, PlayerSide.Second)) yield return m;
    }

    public static IEnumerable<RuntimeCard> OtherFriendlyMinions(GameContext ctx)
    {
        foreach (var m in AllMinions(ctx, ctx.SourceSide))
        {
            if (m.Instance != ctx.Source?.Instance) yield return m;
        }
    }

    public static IEnumerable<RuntimeCard> EnemyMinions(GameContext ctx)
        => AllMinions(ctx, ctx.SourceSide.Opponent());

    public static IEnumerable<RuntimeCard> FriendlyMinions(GameContext ctx)
        => AllMinions(ctx, ctx.SourceSide);

    public static RuntimeCard? RandomMinion(GameContext ctx, PlayerSide side)
    {
        var list = AllMinions(ctx, side).ToList();
        if (list.Count == 0) return null;
        return list[ctx.Rng.Next(0, list.Count)];
    }

    public static RuntimeCard? FindByInstance(GameContext ctx, InstanceId id)
    {
        foreach (var m in AllMinions(ctx))
            if (m.Instance == id) return m;
        return null;
    }
}
