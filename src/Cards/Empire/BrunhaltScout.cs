using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>
/// 布伦哈尔家族斥候 — 2 费 2/1 帝国铜。
/// 开幕：抽一张"布伦哈尔"牌；若手牌中"布伦哈尔"≥3 张，获得屏障。
/// </summary>
[Card(3002)]
public sealed class BrunhaltScout : MinionCard
{
    public override void OnPlay(GameContext ctx)
    {
        ctx.DrawSpecificFromDeck(ctx.SourceSide, s => s.Tags.Contains("布伦哈尔"));

        int countInHand = 0;
        foreach (var c in ctx.Owner.Hand)
        {
            if (ctx.CardDatabase.Get(c.Card).Tags.Contains("布伦哈尔")) countInHand++;
        }
        if (countInHand >= 3 && ctx.Source is not null) ctx.GiveBarrier(ctx.Source);
    }
}
