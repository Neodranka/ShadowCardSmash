using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Net.Serialization;

/// <summary>
/// JSON serializer for <see cref="NetMessage"/>. Separate from GameStateJson because the Net layer
/// owns its own wire format; engine-layer types (Action / Event / GameState) get embedded as Action/Event
/// payload messages in later phases, at which point we will share converters via a common options factory.
///
/// For now the only Domain types referenced are CardId / InstanceId (via custom converters) and PlayerSide
/// (enum, default int). UTF-8 bytes are produced for ENet transmission.
/// </summary>
public static class NetMessageJson
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var o = new JsonSerializerOptions
        {
            IncludeFields = true,
            WriteIndented = false,
        };
        o.Converters.Add(new CardIdConverter());
        o.Converters.Add(new InstanceIdConverter());
        return o;
    }

    public static string Serialize(NetMessage message) =>
        JsonSerializer.Serialize<NetMessage>(message, Options);

    public static NetMessage Deserialize(string json) =>
        JsonSerializer.Deserialize<NetMessage>(json, Options)
        ?? throw new InvalidOperationException("NetMessage deserialize returned null.");

    public static byte[] SerializeToBytes(NetMessage message) =>
        Encoding.UTF8.GetBytes(Serialize(message));

    public static NetMessage DeserializeFromBytes(byte[] bytes) =>
        Deserialize(Encoding.UTF8.GetString(bytes));

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
