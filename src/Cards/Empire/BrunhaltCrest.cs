using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>
/// 布伦哈尔家徽 — 2 费法术。抉择：1) 抽一张"布伦哈尔"随从； 2) 选择一个友方"布伦哈尔"随从为其添加屏障。
/// V1：抉择 UI 未实装，默认走选项 1（抽布伦哈尔）。
/// </summary>
[Card(3001)]
public sealed class BrunhaltCrest : SpellCard
{
    // TODO: 当抉择 UI 接入后，让 PlayCardAction 传 ChoiceIndex 给 OnPlay；当前默认走选项 1。
    public override TargetSpec PlayTarget => TargetSpec.None;

    public override void OnPlay(GameContext ctx)
    {
        ctx.DrawSpecificFromDeck(ctx.SourceSide, s => s.CardType == CardType.Minion && s.Tags.Contains("布伦哈尔"));
    }
}
