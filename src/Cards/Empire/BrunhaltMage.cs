using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>
/// 布伦哈尔家族法师 — 3 费 2/3 帝国银。
/// 开幕：选择一个友方"布伦哈尔"随从，为其添加屏障。
/// </summary>
[Card(3003)]
public sealed class BrunhaltMage : MinionCard
{
    public override TargetSpec PlayTarget => TargetSpec.SingleAllyMinion;

    public override void OnPlay(GameContext ctx)
    {
        if (ctx.PickedTarget is not { } target) return;
        var script = ctx.CardDatabase.Get(target.Card);
        if (!script.Tags.Contains("布伦哈尔")) return;
        ctx.GiveBarrier(target);
    }
}
