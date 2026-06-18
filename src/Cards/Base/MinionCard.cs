using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Cards;

public abstract class MinionCard : CardScript
{
    public sealed override CardType CardType => CardType.Minion;
}
