using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Vampire;

/// <summary>
/// 卖血者 — 1费 1/1 铜
/// 开幕：对我方玩家造成2点伤害，抽1张牌。
/// </summary>
[Card(2001)]
public sealed class BloodSeller : MinionCard
{
    public override string Name => "卖血者";
    public override int Cost => 1;
    public override HeroClass HeroClass => HeroClass.Vampire;
    public override Rarity Rarity => Rarity.Bronze;
    public override IReadOnlyList<string> Tags => new[] { "人类" };

    public override int BaseAttack => 1;
    public override int BaseHealth => 1;
    public override int EvolvedAttack => 3;
    public override int EvolvedHealth => 3;

    public override void OnPlay(GameContext ctx)
    {
        ctx.SelfDamage(ctx.SourceSide, 2);
        ctx.Draw(ctx.SourceSide, 1);
    }
}
