using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Cards;

public abstract class AmuletCard : CardScript
{
    public sealed override CardType CardType => CardType.Amulet;
    public sealed override int BaseAttack => 0;
    // CanActivate / ActivateCost live on CardScript now (minion landmarks 财务官 also use them).
}
