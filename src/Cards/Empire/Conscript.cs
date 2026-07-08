using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>征召兵 — 1 费 1/1 铜。攻击时获得 +2/+0（本次攻击）。</summary>
[Card(3016)]
public sealed class Conscript : MinionCard
{
    public override void OnAttack(GameContext ctx, RuntimeCard? targetMinion, PlayerSide? targetPlayer)
    {
        if (ctx.Source is null) return;
        // Temporary attack boost for this attack only — durations tick at TurnEnd. Duration=1 so it
        // expires this turn even if no attack happens next turn (defensive).
        ctx.Buff(ctx.Source, +2, 0, duration: 1);
    }
}
