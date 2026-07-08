using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>利害权衡 — 1 费法术。抉择：
///   1) 查看牌库顶 3 张，选一张置顶；
///   2) 选一张手牌置于牌库顶，并回复 1 费。
/// V1 简化：选项 1 直接把随机 3 张中最优（费用最低）一张置顶 —— 因为查看牌库顶 UI 会占篇幅，
/// 暂用启发式代理。真实"看 3 选 1"UI 在 Wave 后期补。</summary>
[Card(3015)]
public sealed class CostBenefit : SpellCard
{
    public override IReadOnlyList<CardChoice> Choices { get; } = new[]
    {
        new CardChoice(
            Title: "看牌库顶 3 张，选一张置顶",
            Description: "查看你牌库顶的 3 张牌，选择 1 张移动到牌库顶。",
            TargetForChoice: TargetSpec.None),
        new CardChoice(
            Title: "手牌置顶 + 回费",
            Description: "选择一张手牌置于牌库顶，回复 1 费。",
            TargetForChoice: TargetSpec.MultipleFromHand, TargetsForChoice: 1),
    };

    public override void OnPlay(GameContext ctx)
    {
        int chosen = ctx.PickedChoices.Count > 0 ? ctx.PickedChoices[0] : 0;
        var p = ctx.Owner;

        if (chosen == 0)
        {
            // Choice A: scry-3, put lowest-cost on top. Full "look at 3, pick 1" UI is TODO;
            // heuristic pick keeps the effect functional.
            if (p.Deck.Count == 0) return;
            int scryCount = Math.Min(3, p.Deck.Count);
            int startIdx = p.Deck.Count - scryCount; // top 3 = last 3 in list
            int bestIdx = startIdx;
            int bestCost = ctx.CardDatabase.Get(p.Deck[startIdx].Card).Cost;
            for (int i = startIdx + 1; i < p.Deck.Count; i++)
            {
                int c = ctx.CardDatabase.Get(p.Deck[i].Card).Cost;
                if (c < bestCost) { bestCost = c; bestIdx = i; }
            }
            if (bestIdx != p.Deck.Count - 1)
            {
                var pick = p.Deck[bestIdx];
                p.Deck.RemoveAt(bestIdx);
                p.Deck.Add(pick); // top
            }
        }
        else
        {
            // Choice B: put a chosen hand card on top of deck + refund 1 mana.
            foreach (var c in ctx.PickedTargets)
            {
                if (c.Zone != Zone.Hand) continue;
                ctx.PutHandCardOnTopOfDeck(c);
                break;
            }
            ctx.RefundMana(ctx.SourceSide, 1, source: "costbenefit");
        }
    }
}
