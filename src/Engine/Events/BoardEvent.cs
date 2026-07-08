using System.Text.Json.Serialization;
using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// Base type for everything that happens inside the engine.
/// Used for two purposes:
///   1. EventBus listeners react to them (card-script hooks, persistent buffs).
///   2. The same instances are appended to GameLoop.EventLog so View / Net can replay.
/// Sealed records keep the closed hierarchy explicit; adding a new event = add a record + emit it from EffectPrimitives.
///
/// JSON: polymorphic via "$type" discriminator. Adding a new BoardEvent requires (a) a new
/// [JsonDerivedType(...)] entry here, (b) a round-trip unit test in ActionEventJsonTests.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(GameStartedEvent),             "GameStarted")]
[JsonDerivedType(typeof(GameEndedEvent),               "GameEnded")]
[JsonDerivedType(typeof(PhaseChangedEvent),            "PhaseChanged")]
[JsonDerivedType(typeof(TurnStartedEvent),             "TurnStarted")]
[JsonDerivedType(typeof(TurnEndedEvent),               "TurnEnded")]
[JsonDerivedType(typeof(ManaChangedEvent),             "ManaChanged")]
[JsonDerivedType(typeof(CardDrawnEvent),               "CardDrawn")]
[JsonDerivedType(typeof(CardOverdrawnEvent),           "CardOverdrawn")]
[JsonDerivedType(typeof(FatigueEvent),                 "Fatigue")]
[JsonDerivedType(typeof(CardPlayedEvent),              "CardPlayed")]
[JsonDerivedType(typeof(MinionSummonedEvent),          "MinionSummoned")]
[JsonDerivedType(typeof(AmuletPlacedEvent),            "AmuletPlaced")]
[JsonDerivedType(typeof(MinionAttacksEvent),           "MinionAttacks")]
[JsonDerivedType(typeof(MinionDamagedEvent),           "MinionDamaged")]
[JsonDerivedType(typeof(PlayerDamagedEvent),           "PlayerDamaged")]
[JsonDerivedType(typeof(PlayerHealedEvent),            "PlayerHealed")]
[JsonDerivedType(typeof(MinionHealedEvent),            "MinionHealed")]
[JsonDerivedType(typeof(MinionDestroyedEvent),         "MinionDestroyed")]
[JsonDerivedType(typeof(AmuletDestroyedEvent),         "AmuletDestroyed")]
[JsonDerivedType(typeof(MinionVanishedEvent),          "MinionVanished")]
[JsonDerivedType(typeof(MinionEvolvedEvent),           "MinionEvolved")]
[JsonDerivedType(typeof(EvolutionPointsGrantedEvent),  "EvolutionPointsGranted")]
[JsonDerivedType(typeof(SilenceAppliedEvent),          "SilenceApplied")]
[JsonDerivedType(typeof(BuffAppliedEvent),             "BuffApplied")]
[JsonDerivedType(typeof(KeywordGainedEvent),           "KeywordGained")]
[JsonDerivedType(typeof(BarrierGainedEvent),           "BarrierGained")]
[JsonDerivedType(typeof(BarrierLostEvent),             "BarrierLost")]
[JsonDerivedType(typeof(PlayerBarrierGainedEvent),     "PlayerBarrierGained")]
[JsonDerivedType(typeof(PlayerBarrierLostEvent),       "PlayerBarrierLost")]
[JsonDerivedType(typeof(CountdownTickedEvent),         "CountdownTicked")]
[JsonDerivedType(typeof(AmuletActivatedEvent),         "AmuletActivated")]
[JsonDerivedType(typeof(CardZoneChangedEvent),         "CardZoneChanged")]
[JsonDerivedType(typeof(TileEffectAppliedEvent),       "TileEffectApplied")]
[JsonDerivedType(typeof(PlayerPickRequestEvent),       "PlayerPickRequest")]
[JsonDerivedType(typeof(MulliganConfirmedEvent),       "MulliganConfirmed")]
[JsonDerivedType(typeof(CardRevealedEvent),            "CardRevealed")]
[JsonDerivedType(typeof(ManaGainedEvent),              "ManaGained")]
[JsonDerivedType(typeof(PlayerCounterChangedEvent),    "PlayerCounterChanged")]
[JsonDerivedType(typeof(CardShuffledIntoDeckEvent),    "CardShuffledIntoDeck")]
[JsonDerivedType(typeof(DeckMilledEvent),              "DeckMilled")]
public abstract record BoardEvent
{
    public int Sequence { get; init; }
    public int Depth { get; init; }
}

public sealed record GameStartedEvent : BoardEvent;
public sealed record GameEndedEvent(GameResult Result) : BoardEvent;
public sealed record PhaseChangedEvent(GamePhase Phase, PlayerSide Side) : BoardEvent;

public sealed record TurnStartedEvent(PlayerSide Side, int TurnNumber) : BoardEvent;
public sealed record TurnEndedEvent(PlayerSide Side, int TurnNumber) : BoardEvent;

public sealed record ManaChangedEvent(PlayerSide Side, int Mana, int MaxMana) : BoardEvent;

public sealed record CardDrawnEvent(PlayerSide Side, InstanceId Instance, CardId Card) : BoardEvent;
public sealed record CardOverdrawnEvent(PlayerSide Side, InstanceId Instance, CardId Card) : BoardEvent;
public sealed record FatigueEvent(PlayerSide Side, int Damage) : BoardEvent;

public sealed record CardPlayedEvent(PlayerSide Side, InstanceId Instance, CardId Card) : BoardEvent;
public sealed record MinionSummonedEvent(PlayerSide Side, InstanceId Instance, CardId Card, int TileIndex) : BoardEvent;
public sealed record AmuletPlacedEvent(PlayerSide Side, InstanceId Instance, CardId Card, int TileIndex) : BoardEvent;

public sealed record MinionAttacksEvent(InstanceId Attacker, InstanceId? TargetMinion, PlayerSide? TargetPlayer) : BoardEvent;
public sealed record MinionDamagedEvent(InstanceId Target, int Amount, InstanceId? Source) : BoardEvent;
public sealed record PlayerDamagedEvent(PlayerSide Target, int Amount, InstanceId? Source) : BoardEvent;
public sealed record PlayerHealedEvent(PlayerSide Target, int Amount, InstanceId? Source) : BoardEvent;
public sealed record MinionHealedEvent(InstanceId Target, int Amount, InstanceId? Source) : BoardEvent;

public sealed record MinionDestroyedEvent(InstanceId Instance, CardId Card, PlayerSide Owner, int? TileIndex) : BoardEvent;
public sealed record AmuletDestroyedEvent(InstanceId Instance, CardId Card, PlayerSide Owner, int TileIndex) : BoardEvent;
public sealed record MinionVanishedEvent(InstanceId Instance, CardId Card, PlayerSide Owner) : BoardEvent;

public sealed record MinionEvolvedEvent(InstanceId Instance) : BoardEvent;
public sealed record EvolutionPointsGrantedEvent(PlayerSide Side, int Amount) : BoardEvent;
public sealed record SilenceAppliedEvent(InstanceId Instance) : BoardEvent;
public sealed record BuffAppliedEvent(InstanceId Instance, int AttackDelta, int HealthDelta) : BoardEvent;
public sealed record KeywordGainedEvent(InstanceId Instance, Keyword Keyword) : BoardEvent;
public sealed record BarrierGainedEvent(InstanceId Instance, int Stacks) : BoardEvent;
public sealed record BarrierLostEvent(InstanceId Instance) : BoardEvent;
public sealed record PlayerBarrierGainedEvent(PlayerSide Side, int Stacks) : BoardEvent;
public sealed record PlayerBarrierLostEvent(PlayerSide Side) : BoardEvent;

public sealed record CountdownTickedEvent(InstanceId Instance, int Remaining) : BoardEvent;
public sealed record AmuletActivatedEvent(InstanceId Instance) : BoardEvent;

public sealed record CardZoneChangedEvent(InstanceId Instance, Zone From, Zone To) : BoardEvent;
public sealed record TileEffectAppliedEvent(PlayerSide Side, int TileIndex, string EffectKey, int Duration) : BoardEvent;

public sealed record PlayerPickRequestEvent(PlayerSide Side, string Reason) : BoardEvent;
public sealed record MulliganConfirmedEvent(PlayerSide Side, int[] SwappedIndices) : BoardEvent;

/// <summary>Fired when a card in hand is briefly flipped face-up to both players (玛丽斯卡's compare).</summary>
public sealed record CardRevealedEvent(InstanceId Instance, CardId Card, PlayerSide Owner) : BoardEvent;

/// <summary>Fired by Refund/Grant mana primitives. Turn-start refill uses ManaChangedEvent only —
/// so listeners like 塔尔莫维奇财务官 fire on 3/6/回合外获取 but not on 每回合起始的正常涨费.</summary>
public sealed record ManaGainedEvent(PlayerSide Side, int Amount, string Source) : BoardEvent;

/// <summary>Fired when PlayerState.Counters[Key] changes (e.g., "regency" for 议案 layers).</summary>
public sealed record PlayerCounterChangedEvent(PlayerSide Side, string Key, int NewValue) : BoardEvent;

/// <summary>Fired when a hand card is placed back into deck (通用洗入). Triggers 议会秘书.</summary>
public sealed record CardShuffledIntoDeckEvent(InstanceId Instance, CardId Card, PlayerSide Side) : BoardEvent;

/// <summary>Fired when N cards are moved from deck to graveyard in bulk (紧急议案). Not a draw.</summary>
public sealed record DeckMilledEvent(PlayerSide Side, int Count) : BoardEvent;
