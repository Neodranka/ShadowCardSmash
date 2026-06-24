using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Neutral;

/// <summary>
/// 护盾术 — 2 费法术。选择一个友方随从，为其添加屏障(强化4)，并使我方玩家获得屏障(1层)。
/// </summary>
[Card(1011)]
public sealed class ShieldSpell : SpellCard
{
    public override TargetSpec PlayTarget => TargetSpec.SingleAllyMinion;

    public override void OnPlay(GameContext ctx)
    {
        if (ctx.PickedTarget is { } target) ctx.GiveBarrier(target, stacks: 4);
        ctx.GiveBarrier(ctx.SourceSide, stacks: 1);
    }
}
