using System.Text.Json.Serialization;
using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// Player-issued command. Validate → Apply. Adding a new command = a new class implementing this; no framework change.
/// Persisted/transported by the Net layer; same instance executes on both peers under deterministic engine.
///
/// JSON: serialized polymorphically via the "$type" discriminator. Adding a new IGameAction implementation
/// requires (a) a new [JsonDerivedType(...)] entry here, (b) a round-trip unit test in ActionEventJsonTests.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(PlayCardAction), "PlayCard")]
[JsonDerivedType(typeof(AttackAction),   "Attack")]
[JsonDerivedType(typeof(EvolveAction),   "Evolve")]
[JsonDerivedType(typeof(EndTurnAction),  "EndTurn")]
[JsonDerivedType(typeof(MulliganAction), "Mulligan")]
[JsonDerivedType(typeof(ActivateAction), "Activate")]
public interface IGameAction
{
    /// <summary>The side issuing this action.</summary>
    PlayerSide Issuer { get; }

    /// <summary>Read-only check: is this action legal in the given state? Returns reason on failure.</summary>
    ActionResult Validate(GameState state);

    /// <summary>
    /// Apply state mutations and publish events via ctx. Called only after Validate succeeds.
    /// The GameLoop has already snapshotted state before this is called; any throw rolls back.
    /// </summary>
    void Apply(GameContext ctx);
}

public readonly struct ActionResult
{
    public bool IsOk { get; }
    public string? Reason { get; }
    private ActionResult(bool ok, string? reason) { IsOk = ok; Reason = reason; }
    public static ActionResult Ok() => new(true, null);
    public static ActionResult Fail(string reason) => new(false, reason);
    public static implicit operator bool(ActionResult r) => r.IsOk;
}
