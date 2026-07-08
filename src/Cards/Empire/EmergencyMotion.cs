using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>紧急议案 — 3 费法术。从牌库抽到手上限，其余牌库牌全部进入墓地（不算抽牌）。</summary>
[Card(3013)]
public sealed class EmergencyMotion : SpellCard
{
    public override void OnPlay(GameContext ctx)
    {
        ctx.DrawUntilHandFull(ctx.SourceSide);
        ctx.MillRestOfDeck(ctx.SourceSide);
    }
}
