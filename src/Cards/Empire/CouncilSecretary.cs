using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>议会秘书 — 2 费 2/2 银。每当你把一张牌洗入牌库时 +1/+1。</summary>
[Card(3022)]
public sealed class CouncilSecretary : MinionCard
{
    public override void OnOwnerCardShuffledIntoDeck(GameContext ctx, RuntimeCard shuffledCard)
    {
        if (ctx.Source is null) return;
        ctx.Buff(ctx.Source, +1, +1, duration: -1);
    }
}
