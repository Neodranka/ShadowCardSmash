using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// Resolves a CardId to its CardScript (the C# class registered via [Card(id)]).
/// Implemented by CardRegistry in the Cards layer; Engine consumes only the interface.
/// </summary>
public interface ICardDatabase
{
    ICardScript Get(CardId id);
    bool TryGet(CardId id, out ICardScript script);
    IEnumerable<ICardScript> All();
}

/// <summary>
/// Minimal contract the Engine knows about. The Cards layer extends this with MinionCard/SpellCard/AmuletCard subclasses
/// that define richer hooks. Engine only needs to identify type + cost + base stats + run play-time hooks generically.
/// </summary>
public interface ICardScript
{
    CardId Id { get; }
    string Name { get; }
    string Description { get; }
    int Cost { get; }
    CardType CardType { get; }
    HeroClass HeroClass { get; }
    Rarity Rarity { get; }
    IReadOnlyList<string> Tags { get; }
    TargetSpec PlayTarget { get; }

    // Stat hooks (only meaningful for minions/amulets; spells return 0).
    int BaseAttack { get; }
    int BaseHealth { get; }
    int EvolvedAttack { get; }
    int EvolvedHealth { get; }
    Keyword InitialKeywords { get; }
    int InitialCountdown { get; }

    // Engine-side hook invocations. Most card scripts override only a few.
    void OnPlay(GameContext ctx);
    void OnDeath(GameContext ctx);
    void OnAttack(GameContext ctx, RuntimeCard? targetMinion, PlayerSide? targetPlayer);
    void OnDamaged(GameContext ctx, int amount, InstanceId? sourceInstance);
    void OnOwnerTurnStart(GameContext ctx);
    void OnOwnerTurnEnd(GameContext ctx);
    void OnEvolve(GameContext ctx);
    void OnActivate(GameContext ctx);
    void OnCountdownReachZero(GameContext ctx);

    /// <summary>Fired on the target card when its own barrier stack drops to 0.</summary>
    void OnSelfBarrierLost(GameContext ctx);
    /// <summary>Fired on every field card whenever any minion is placed on the field. Includes the newcomer.</summary>
    void OnFieldMinionEntered(GameContext ctx, RuntimeCard newcomer);
    /// <summary>Fired on every field card OTHER than the target when a minion loses its last barrier stack.</summary>
    void OnFieldMinionBarrierLost(GameContext ctx, RuntimeCard target);
}
