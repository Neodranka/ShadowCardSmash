using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>塔尔莫维奇财务官 — 3 费 2/3 铜。
/// 每当你获得"额外费用"（回复/获得，非回合开始涨费）时 +1/+1。
/// 启动 1（冷却 1）：获得 1 点费用（不封顶）。</summary>
[Card(3019)]
public sealed class TalmovichBursar : MinionCard
{
    public override bool CanActivate => true;
    public override int ActivateCost => 1;

    public override void OnActivate(GameContext ctx)
    {
        if (ctx.Source is null) return;
        // Set cooldown before granting mana so the OnManaGained hook (which fires on grant) can't
        // re-enter this card's activation.
        ctx.Source.Counters["activate_cd"] = 1;
        ctx.GrantMana(ctx.SourceSide, 1, source: "bursar");
    }

    public override void OnOwnerManaGained(GameContext ctx, int amount, string source)
    {
        // Any refund/grant except own activation grants +1/+1. (Own activation excluded to avoid
        // the "gain 1 mana → +1/+1 → next turn gain 1 mana again → +1/+1" degenerate loop
        // sneaking through in future edits; per-turn CD already blocks re-activation but this
        // extra guard keeps semantics clean.)
        if (source == "bursar") return;
        if (ctx.Source is not null) ctx.Buff(ctx.Source, +1, +1, duration: -1);
    }
}
