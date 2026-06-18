using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Cards.Neutral;

/// <summary>
/// 训练假人 — 1费 0/3 中立 守护
/// 纯白板单位，用于测试和卡组填充。
/// </summary>
[Card(1001)]
public sealed class TrainingDummy : MinionCard
{
    public override string Name => "训练假人";
    public override string Description => "守护。一面 0/3 的肉墙，没有攻击力但能挡刀。";
    public override int Cost => 1;
    public override HeroClass HeroClass => HeroClass.Neutral;
    public override Rarity Rarity => Rarity.Bronze;
    public override int BaseAttack => 0;
    public override int BaseHealth => 3;
    public override int EvolvedAttack => 2;
    public override int EvolvedHealth => 5;
    public override ShadowCardSmash.Domain.Keyword InitialKeywords => ShadowCardSmash.Domain.Keyword.Ward;
}
