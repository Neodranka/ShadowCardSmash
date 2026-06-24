using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>
/// 布伦哈尔家徽 — 2 费法术。抉择：1) 抽一张"布伦哈尔"随从； 2) 选择一个友方"布伦哈尔"随从，为其添加屏障。
/// </summary>
[Card(3001)]
public sealed class BrunhaltCrest : SpellCard
{
    public override IReadOnlyList<CardChoice> Choices { get; } = new[]
    {
        new CardChoice(
            Title: "抽一张「布伦哈尔」随从",
            Description: "从牌库找一张带「布伦哈尔」标签的随从加入手牌。",
            TargetForChoice: TargetSpec.None),
        new CardChoice(
            Title: "强化「布伦哈尔」",
            Description: "选择一个友方「布伦哈尔」随从，为其添加屏障。",
            TargetForChoice: TargetSpec.SingleAllyMinion),
    };

    public override void OnPlay(GameContext ctx)
    {
        int chosen = ctx.PickedChoices.Count > 0 ? ctx.PickedChoices[0] : 0;
        if (chosen == 0)
        {
            ctx.DrawSpecificFromDeck(ctx.SourceSide,
                s => s.CardType == CardType.Minion && s.Tags.Contains("布伦哈尔"));
        }
        else
        {
            if (ctx.PickedTarget is not { } target) return;
            var script = ctx.CardDatabase.Get(target.Card);
            if (!script.Tags.Contains("布伦哈尔")) return;
            ctx.GiveBarrier(target);
        }
    }
}
