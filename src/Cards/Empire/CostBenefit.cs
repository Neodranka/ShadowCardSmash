using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>
/// 利害权衡 — 1 费法术。查看我方牌库顶的三张牌，可选择一张移动到牌库顶。
/// 或是选择一张手牌置入牌库顶，并恢复1点费用。
///
/// 无论哪个分支都会先看这三张牌。UI 使用 ScryPopup 展示后二选一：
///   Branch A (ChoiceIndices[0]=0): 选一张作为新的牌库顶（其余顺序不变）
///   Branch B (ChoiceIndices[0]=1): 顶 3 张洗回牌库随机位置 + ExtraTargets[0] 手牌置牌库顶 + 回费 1
/// </summary>
[Card(3015)]
public sealed class CostBenefit : SpellCard
{
    public override TargetSpec PlayTarget => TargetSpec.ScryTop3;

    public override void OnPlay(GameContext ctx)
    {
        var p = ctx.Owner;
        int scryCount = Math.Min(3, p.Deck.Count);
        if (scryCount == 0) return;

        int branch = ctx.PickedChoices.Count > 0 ? ctx.PickedChoices[0] : 0;

        if (branch == 0)
        {
            // Branch A: reorder top scryCount so chosen index becomes topmost.
            // chosenIdx 0 = current top, 1 = 2nd from top, 2 = 3rd. UI order matches.
            int chosenIdx = ctx.PickedChoices.Count > 1 ? ctx.PickedChoices[1] : 0;
            if (chosenIdx < 0 || chosenIdx >= scryCount) chosenIdx = 0;
            if (chosenIdx == 0) return; // already top, nothing to do
            int deckPos = p.Deck.Count - 1 - chosenIdx;
            var pick = p.Deck[deckPos];
            p.Deck.RemoveAt(deckPos);
            p.Deck.Add(pick); // now top
            return;
        }

        // Branch B: shuffle top scryCount into deck randomly, then hand card on top + refund.
        var topThree = new System.Collections.Generic.List<RuntimeCard>();
        for (int i = 0; i < scryCount; i++)
        {
            topThree.Add(p.Deck[^1]);
            p.Deck.RemoveAt(p.Deck.Count - 1);
        }
        foreach (var c in topThree)
        {
            int idx = ctx.Rng.Next(0, p.Deck.Count + 1);
            p.Deck.Insert(idx, c);
        }
        // Now place hand card on top + refund 1 mana.
        if (ctx.PickedTargets.Count > 0)
        {
            var handCard = ctx.PickedTargets[0];
            if (handCard.Zone == Zone.Hand)
            {
                ctx.PutHandCardOnTopOfDeck(handCard);
                ctx.RefundMana(ctx.SourceSide, 1, source: "costbenefit");
            }
        }
    }
}
