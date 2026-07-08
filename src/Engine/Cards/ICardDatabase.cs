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
    /// <summary>How many minion targets the main play action wants when PlayTarget is Single*. Default 1.</summary>
    int TargetsToPick { get; }
    /// <summary>Optional choice menu shown before play resolves. Empty = no choice.</summary>
    IReadOnlyList<CardChoice> Choices { get; }
    /// <summary>How many of <see cref="Choices"/> the player must select. Default 1.</summary>
    int ChoicesToPick { get; }
    /// <summary>
    /// 强化 X：玩家剩余费用 ≥ X 时，本卡费用被强制改为 X，并触发 ctx.IsEnhanced 额外效果。
    /// 0 表示无强化。
    /// </summary>
    int EnhanceCost { get; }

    // Stat hooks (only meaningful for minions/amulets; spells return 0).
    int BaseAttack { get; }
    int BaseHealth { get; }
    int EvolvedAttack { get; }
    int EvolvedHealth { get; }
    Keyword InitialKeywords { get; }
    int InitialCountdown { get; }
    string ArtPath { get; }
    /// <summary>Landmark = special Amulet whose target slot is TerrainSlot; playing overwrites any
    /// existing occupant of that slot (existing terrain/landmark gets destroyed to graveyard first).</summary>
    bool IsLandmark { get; }
    /// <summary>True = card has a player-triggered activate ability (spends ActivateCost mana).</summary>
    bool CanActivate { get; }
    int ActivateCost { get; }

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

    /// <summary>Fired on every field card belonging to <paramref name="side"/> after a draw operation
    /// completes. count = number of cards actually drawn (not milled). Fires once per draw call
    /// regardless of count (会议记录员's rule).</summary>
    void OnOwnerCardsDrawnBatch(GameContext ctx, int count);

    /// <summary>Fired on every field card belonging to the shuffled card's owner when a hand card is
    /// moved back into the deck. Triggers 议会秘书.</summary>
    void OnOwnerCardShuffledIntoDeck(GameContext ctx, RuntimeCard shuffledCard);

    /// <summary>Fired on every field card belonging to <paramref name="side"/> when mana is gained
    /// via Refund/Grant (NOT turn-start refill). Triggers 塔尔莫维奇财务官.</summary>
    void OnOwnerManaGained(GameContext ctx, int amount, string source);
}
