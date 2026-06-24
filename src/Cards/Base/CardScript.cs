using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards;

/// <summary>
/// Base class every concrete card inherits from (transitively, via MinionCard/SpellCard/AmuletCard).
/// Default hooks are no-ops; cards override only what they need.
///
/// Data sourcing: static properties (Name/Cost/Attack/Health/...) read from <see cref="Data"/> when
/// a CardDataResource is attached via <see cref="AttachData"/>; otherwise from C# overrides on the card class.
/// Tests bypass the resource pipeline entirely by overriding the properties directly.
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
    public ICardData? Data { get; private set; }

    /// <summary>Bind a data resource to this card. Called by CardResourceLoader at startup.</summary>
    public void AttachData(ICardData data) => Data = data;

    public virtual string Name => Data?.Name ?? "[未配置]";
    public virtual string Description => Data?.Description ?? "";
    public virtual int Cost => Data?.Cost ?? 0;
    public virtual CardType CardType => Data?.CardType ?? CardType.Minion;
    public virtual HeroClass HeroClass => Data?.HeroClass ?? HeroClass.Neutral;
    public virtual Rarity Rarity => Data?.Rarity ?? Rarity.Bronze;
    public virtual IReadOnlyList<string> Tags => Data?.Tags ?? Array.Empty<string>();
    public virtual TargetSpec PlayTarget => TargetSpec.None;

    public virtual int BaseAttack => Data?.BaseAttack ?? 0;
    public virtual int BaseHealth => Data?.BaseHealth ?? 0;
    public virtual int EvolvedAttack => Data?.EvolvedAttack ?? 0;
    public virtual int EvolvedHealth => Data?.EvolvedHealth ?? 0;
    public virtual Keyword InitialKeywords => Data?.InitialKeywords ?? Keyword.None;
    public virtual int InitialCountdown => Data?.InitialCountdown ?? -1;

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

    public virtual void OnSelfBarrierLost(GameContext ctx) { }
    public virtual void OnFieldMinionEntered(GameContext ctx, RuntimeCard newcomer) { }
    public virtual void OnFieldMinionBarrierLost(GameContext ctx, RuntimeCard target) { }
}
