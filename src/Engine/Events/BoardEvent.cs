using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// Base type for everything that happens inside the engine.
/// Used for two purposes:
///   1. EventBus listeners react to them (card-script hooks, persistent buffs).
///   2. The same instances are appended to GameLoop.EventLog so View / Net can replay.
/// Sealed records keep the closed hierarchy explicit; adding a new event = add a record + emit it from EffectPrimitives.
/// </summary>
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
public sealed record MulliganConfirmedEvent(PlayerSide Side, IReadOnlyList<int> SwappedIndices) : BoardEvent;
