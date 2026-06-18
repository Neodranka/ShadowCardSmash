using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Cards;

public abstract class AmuletCard : CardScript
{
    public sealed override CardType CardType => CardType.Amulet;
    public sealed override int BaseAttack => 0;

    /// <summary>Whether the player can spend mana to trigger OnActivate. Activation rules per-card.</summary>
    public virtual bool CanActivate => false;
    public virtual int ActivateCost => 0;
}
