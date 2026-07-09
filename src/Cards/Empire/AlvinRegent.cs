using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>首席摄政 阿尔文大公 — 3 费 1/1 传说。
/// 开幕：从卡组抽一张"摄政议会"；若我方场上已存在摄政议会，则改为洗任意张手牌回牌库并抽等量。
/// 进化时：从墓地选一张摄政牌返回手牌。</summary>
[Card(3010)]
public sealed class AlvinRegent : MinionCard
{
    private static readonly CardId RegencyCouncilId = new(3011);
    public override TargetSpec PlayTarget => TargetSpec.MultipleFromHand;
    public override int TargetsToPick => int.MaxValue; // "任意张手牌"

    /// <summary>Dynamic override: no council on field → PlayTarget=None (auto-tutor, no popup).
    /// Council on field → MultipleFromHand (open hand-multi-select for the shuffle path).</summary>
    public override TargetSpec ResolvePlayTarget(GameState state, PlayerSide side)
    {
        var p = state.GetPlayer(side);
        if (p.TerrainSlot.Occupant is { } t && t.Card == RegencyCouncilId) return TargetSpec.MultipleFromHand;
        foreach (var tile in p.Field)
            if (tile.Occupant is { } m && m.Card == RegencyCouncilId) return TargetSpec.MultipleFromHand;
        return TargetSpec.None;
    }

    public override void OnPlay(GameContext ctx)
    {
        var p = ctx.Owner;
        bool councilOnField =
            (p.TerrainSlot.Occupant is { } t && t.Card == RegencyCouncilId);
        // Also count normal-tile occupants just in case future variants are placed there.
        foreach (var tile in p.Field)
        {
            if (tile.Occupant is { } m && m.Card == RegencyCouncilId) { councilOnField = true; break; }
        }

        if (!councilOnField)
        {
            // Try to draw a 摄政议会 from deck. If none → no effect (per user Q10).
            ctx.DrawSpecificFromDeck(ctx.SourceSide, s => s.Id == RegencyCouncilId);
            return;
        }

        // Shuffle-and-redraw path: any number of hand cards → deck, then draw same count.
        int shuffled = 0;
        foreach (var c in ctx.PickedTargets)
        {
            if (c.Zone != Zone.Hand) continue;
            if (c.Instance == ctx.Source?.Instance) continue;
            ctx.ShuffleHandToDeck(c);
            shuffled++;
        }
        if (shuffled > 0) ctx.Draw(ctx.SourceSide, shuffled);
    }

    public override void OnEvolve(GameContext ctx)
    {
        // Return a Regency-tagged card from graveyard to hand.
        // ctx.PickedTarget is set by the evolve target-picking UI when we add graveyard target support.
        // For now, fall back to auto-pick: highest-cost 摄政 card from graveyard.
        if (ctx.PickedTarget is { } target && target.Zone == Zone.Graveyard)
        {
            var script = ctx.CardDatabase.Get(target.Card);
            if (script.Tags.Contains("摄政")) ctx.ReturnGraveyardCardToHand(target);
            return;
        }
        // Auto-pick fallback: first 摄政 card found in graveyard.
        var p = ctx.Owner;
        foreach (var c in p.Graveyard)
        {
            var s = ctx.CardDatabase.Get(c.Card);
            if (s.Tags.Contains("摄政"))
            {
                ctx.ReturnGraveyardCardToHand(c);
                return;
            }
        }
    }
}
