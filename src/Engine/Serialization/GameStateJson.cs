using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine.Serialization;

/// <summary>
/// Round-trippable JSON for <see cref="GameState"/>. Used by:
///   • Network protocol (snapshot send/receive between host and client).
///   • Reconnect: host sends a filtered snapshot to a returning client.
///   • Diagnostics / save-state dumps.
///
/// Format guarantees:
///   • `Deserialize(Serialize(s))` produces a state semantically equal to `s` (deep copy via JSON).
///   • `Serialize(Deserialize(Serialize(s))) == Serialize(s)` (idempotent round-trip).
///   • All public fields on Domain types are included; private state must be exposed via public members.
///
/// Notes:
///   • <see cref="CardId"/> / <see cref="InstanceId"/> serialize as plain ints (custom converters).
///   • Enums serialize as their int values (System.Text.Json default).
///   • Does NOT include card script behaviour — recipient must have the same CardRegistry registered.
/// </summary>
public static class GameStateJson
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var o = new JsonSerializerOptions
        {
            IncludeFields = true,         // Domain types use public fields, not auto-properties.
            WriteIndented = false,
        };
        o.Converters.Add(new CardIdConverter());
        o.Converters.Add(new InstanceIdConverter());
        return o;
    }

    public static string Serialize(GameState state) => JsonSerializer.Serialize(state, Options);

    public static GameState Deserialize(string json) =>
        JsonSerializer.Deserialize<GameState>(json, Options)
        ?? throw new InvalidOperationException("Deserialize returned null.");

    /// <summary>Polymorphic action serialize — writes "$type" discriminator for the concrete IGameAction.</summary>
    public static string SerializeAction(IGameAction action) =>
        JsonSerializer.Serialize<IGameAction>(action, Options);

    public static IGameAction DeserializeAction(string json) =>
        JsonSerializer.Deserialize<IGameAction>(json, Options)
        ?? throw new InvalidOperationException("Action deserialize returned null.");

    /// <summary>Polymorphic event serialize — writes "$type" discriminator for the concrete BoardEvent.</summary>
    public static string SerializeEvent(BoardEvent evt) =>
        JsonSerializer.Serialize<BoardEvent>(evt, Options);

    public static BoardEvent DeserializeEvent(string json) =>
        JsonSerializer.Deserialize<BoardEvent>(json, Options)
        ?? throw new InvalidOperationException("Event deserialize returned null.");

    /// <summary>Batch serialize a list of events as a single JSON array of polymorphic BoardEvent.</summary>
    public static string SerializeEventBatch(IReadOnlyList<BoardEvent> events) =>
        JsonSerializer.Serialize<IReadOnlyList<BoardEvent>>(events, Options);

    public static BoardEvent[] DeserializeEventBatch(string json) =>
        JsonSerializer.Deserialize<BoardEvent[]>(json, Options)
        ?? throw new InvalidOperationException("Event batch deserialize returned null.");

    private sealed class CardIdConverter : JsonConverter<CardId>
    {
        public override CardId Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) => new(r.GetInt32());
        public override void Write(Utf8JsonWriter w, CardId v, JsonSerializerOptions o) => w.WriteNumberValue(v.Value);
    }

    private sealed class InstanceIdConverter : JsonConverter<InstanceId>
    {
        public override InstanceId Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) => new(r.GetInt32());
        public override void Write(Utf8JsonWriter w, InstanceId v, JsonSerializerOptions o) => w.WriteNumberValue(v.Value);
    }
}
