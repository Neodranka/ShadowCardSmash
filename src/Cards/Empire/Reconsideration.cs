using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>复议 — 5 费法术。从墓地随机抽到手上限，其余墓地牌全部返回牌库。</summary>
[Card(3014)]
public sealed class Reconsideration : SpellCard
{
    public override void OnPlay(GameContext ctx)
    {
        ctx.DrawFromGraveyardUntilHandFull(ctx.SourceSide);
        ctx.ReturnGraveyardToDeck(ctx.SourceSide);
    }
}
