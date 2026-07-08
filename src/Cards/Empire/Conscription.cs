using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>征兵令 — 5 费法术。召唤 4 个征召兵，全部获得突进。</summary>
[Card(3017)]
public sealed class Conscription : SpellCard
{
    private static readonly CardId ConscriptId = new(3016);

    public override void OnPlay(GameContext ctx)
    {
        for (int i = 0; i < 4; i++)
        {
            var iid = ctx.Summon(ConscriptId, ctx.SourceSide);
            if (iid is null) continue; // no empty tile → vanished (handled by Summon)
            var c = ctx.Owner.FindOnField(iid.Value);
            if (c is not null) ctx.GainKeyword(c, Keyword.Rush);
        }
    }
}
