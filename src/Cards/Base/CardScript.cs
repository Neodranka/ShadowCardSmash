using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards;

/// <summary>
/// Base class every concrete card inherits from (transitively, via MinionCard/SpellCard/AmuletCard).
/// Default hooks are no-ops; cards override only what they need.
///
/// Authoring contract:
///   • Class is sealed.
///   • Marked [Card(numeric_id)].
///   • Constructor is parameterless.
///   • All overrides are pure with respect to ctx: read state, call ctx.X primitives, no I/O.
/// </summary>
public abstract class CardScript : ICardScript
{
    public CardId Id { get; }

    public abstract string Name { get; }
    public virtual string Description => "";
    public abstract int Cost { get; }
    public abstract CardType CardType { get; }
    public abstract HeroClass HeroClass { get; }
    public abstract Rarity Rarity { get; }
    public virtual IReadOnlyList<string> Tags => Array.Empty<string>();
    public virtual TargetSpec PlayTarget => TargetSpec.None;

    public virtual int BaseAttack => 0;
    public virtual int BaseHealth => 0;
    public virtual int EvolvedAttack => 0;
    public virtual int EvolvedHealth => 0;
    public virtual Keyword InitialKeywords => Keyword.None;
    public virtual int InitialCountdown => -1;

    protected CardScript()
    {
        var attr = (CardAttribute?)Attribute.GetCustomAttribute(GetType(), typeof(CardAttribute))
                   ?? throw new InvalidOperationException($"{GetType().Name} is missing [Card(id)] attribute.");
        Id = attr.Id;
    }

    // Engine-side hook signatures. Default no-op; override in concrete cards.
    public virtual void OnPlay(GameContext ctx) { }
    public virtual void OnDeath(GameContext ctx) { }
    public virtual void OnAttack(GameContext ctx, RuntimeCard? targetMinion, PlayerSide? targetPlayer) { }
    public virtual void OnDamaged(GameContext ctx, int amount, InstanceId? sourceInstance) { }
    public virtual void OnOwnerTurnStart(GameContext ctx) { }
    public virtual void OnOwnerTurnEnd(GameContext ctx) { }
    public virtual void OnEvolve(GameContext ctx) { }
    public virtual void OnActivate(GameContext ctx) { }
    public virtual void OnCountdownReachZero(GameContext ctx) { }
}
