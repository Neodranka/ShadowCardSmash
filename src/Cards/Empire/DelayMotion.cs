using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>拖延议程 — 0 费法术。选择一张手牌洗入牌库，下回合额外抽 2。可叠加。</summary>
[Card(3021)]
public sealed class DelayMotion : SpellCard
{
    public override TargetSpec PlayTarget => TargetSpec.MultipleFromHand;
    public override int TargetsToPick => 1;

    public override void OnPlay(GameContext ctx)
    {
        // Must pick exactly 1 hand card (UI enforces via TargetsToPick when using MultipleFromHand
        // with a positive TargetsToPick constraint — we treat as "up to 1"; if 0 selected, no shuffle,
        // no bonus. Reasonable UX and mirrors user Q13 (0 legal).
        int shuffled = 0;
        foreach (var c in ctx.PickedTargets)
        {
            if (c.Zone != Zone.Hand) continue;
            ctx.ShuffleHandToDeck(c);
            shuffled++;
            if (shuffled >= 1) break;
        }
        if (shuffled == 0) return;
        ctx.Owner.NextTurnBonusDraws += 2;
    }
}
