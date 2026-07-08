using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>会议记录员 — 2 费 2/2 银。每当你（一次）抽卡时 +1/+1（按抽卡次数，不按张数）。</summary>
[Card(3020)]
public sealed class MinuteKeeper : MinionCard
{
    public override void OnOwnerCardsDrawnBatch(GameContext ctx, int count)
    {
        if (ctx.Source is null || count <= 0) return;
        ctx.Buff(ctx.Source, +1, +1, duration: -1);
    }
}
