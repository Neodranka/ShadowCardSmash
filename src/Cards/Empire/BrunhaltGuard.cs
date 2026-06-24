using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>
/// 布伦哈尔家族卫队 — 3 费 2/2 帝国铜，自带屏障。失去屏障时获得 +1/+1。
/// </summary>
[Card(3004)]
public sealed class BrunhaltGuard : MinionCard
{
    public override void OnSelfBarrierLost(GameContext ctx)
    {
        if (ctx.Source is not null) ctx.Buff(ctx.Source, +1, +1, duration: -1);
    }
}
