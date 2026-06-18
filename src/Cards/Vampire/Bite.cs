using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Vampire;

/// <summary>
/// 撕咬 — 1费法术 铜
/// 对一个敌方随从造成2点伤害，恢复2点我方生命值。
/// </summary>
[Card(2004)]
public sealed class Bite : SpellCard
{
    public override string Name => "撕咬";
    public override int Cost => 1;
    public override HeroClass HeroClass => HeroClass.Vampire;
    public override Rarity Rarity => Rarity.Bronze;
    public override IReadOnlyList<string> Tags => new[] { "魔法" };
    public override TargetSpec PlayTarget => TargetSpec.SingleEnemyMinion;

    public override void OnPlay(GameContext ctx)
    {
        if (ctx.PickedTarget is not null) ctx.Damage(ctx.PickedTarget, 2);
        ctx.Heal(ctx.SourceSide, 2);
    }
}
