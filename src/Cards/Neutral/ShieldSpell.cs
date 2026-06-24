using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Neutral;

/// <summary>
/// 护盾术 — 2 费中立法术。选择一个友方随从为其添加屏障。
/// 强化 4：剩余费用 &gt; 4 时本卡费用变 4，效果额外让我方玩家也获得屏障。
/// </summary>
[Card(1011)]
public sealed class ShieldSpell : SpellCard
{
    public override TargetSpec PlayTarget => TargetSpec.SingleAllyMinion;
    public override int EnhanceCost => 4;

    public override void OnPlay(GameContext ctx)
    {
        if (ctx.PickedTarget is { } target) ctx.GiveBarrier(target);
        if (ctx.IsEnhanced) ctx.GiveBarrier(ctx.SourceSide);
    }
}
