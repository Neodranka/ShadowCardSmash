using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>投机者 玛丽斯卡·塔尔莫维奇 — 5 费 2/3 传说。
/// 开幕：敌我双方各抽一张牌并展示。若我方费用 &gt; 敌方，随机破坏 2 个敌方随从；若 ≤，则回复 2 费。</summary>
[Card(3012)]
public sealed class SpeculatorMariska : MinionCard
{
    public override void OnPlay(GameContext ctx)
    {
        var me = ctx.Owner;
        var enemy = ctx.Enemy;
        int myHandBefore = me.Hand.Count;
        int enemyHandBefore = enemy.Hand.Count;

        ctx.Draw(ctx.SourceSide, 1);
        ctx.Draw(ctx.SourceSide.Opponent(), 1);

        RuntimeCard? myDrawn = me.Hand.Count > myHandBefore ? me.Hand[^1] : null;
        RuntimeCard? enemyDrawn = enemy.Hand.Count > enemyHandBefore ? enemy.Hand[^1] : null;
        if (myDrawn is null || enemyDrawn is null) return;

        // Reveal both (client sees enemy's; enemy sees mine; both compare).
        ctx.RevealCard(myDrawn);
        ctx.RevealCard(enemyDrawn);

        int myCost = ctx.CardDatabase.Get(myDrawn.Card).Cost;
        int enemyCost = ctx.CardDatabase.Get(enemyDrawn.Card).Cost;
        if (myCost > enemyCost)
            ctx.DestroyRandomEnemyMinions(ctx.SourceSide, 2);
        else
            ctx.RefundMana(ctx.SourceSide, 2, source: "mariska");
    }
}
