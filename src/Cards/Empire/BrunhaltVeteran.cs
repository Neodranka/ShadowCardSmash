using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>
/// 布伦哈尔老兵 — 4 费 4/3 帝国银。每个自己的回合结束时，获得屏障。
/// </summary>
[Card(3008)]
public sealed class BrunhaltVeteran : MinionCard
{
    public override void OnOwnerTurnEnd(GameContext ctx)
    {
        if (ctx.Source is not null) ctx.GiveBarrier(ctx.Source);
    }
}
