using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>塔尔莫维奇商队 — 3 费 1/3 铜。开幕：选择任意张手牌洗回牌库，回复等量费用。</summary>
[Card(3018)]
public sealed class TalmovichCaravan : MinionCard
{
    public override TargetSpec PlayTarget => TargetSpec.MultipleFromHand;
    public override int TargetsToPick => int.MaxValue; // "任意张手牌"

    public override void OnPlay(GameContext ctx)
    {
        // ctx.PickedTargets contains the hand cards the player selected (0..N legal).
        int count = 0;
        foreach (var c in ctx.PickedTargets)
        {
            if (c.Zone != Zone.Hand) continue;
            if (c.Instance == ctx.Source?.Instance) continue; // safety: don't shuffle self
            ctx.ShuffleHandToDeck(c);
            count++;
        }
        if (count > 0) ctx.RefundMana(ctx.SourceSide, count, source: "caravan");
    }
}
