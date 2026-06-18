using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Cards;

public abstract class SpellCard : CardScript
{
    public sealed override CardType CardType => CardType.Spell;
    public sealed override int BaseAttack => 0;
    public sealed override int BaseHealth => 0;
}
