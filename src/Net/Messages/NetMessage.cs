using System;
using System.Text.Json.Serialization;
using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Net;

/// <summary>
/// Wire-format envelope for everything sent over the network. Polymorphic via "$type" discriminator,
/// short PascalCase tags. Adding a new wire message requires (a) a new [JsonDerivedType] entry here,
/// (b) round-trip unit test in NetMessageJsonTests.
///
/// Categories (will grow over phases):
///   Phase 3: connection handshake + connectivity ping
///   Phase 4: action requests / event broadcasts
///   Phase 5: snapshot push (state sync)
///   Phase 6/7: disconnect notice / reconnect handshake
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(HandshakeRequest),  "HandshakeRequest")]
[JsonDerivedType(typeof(HandshakeAccepted), "HandshakeAccepted")]
[JsonDerivedType(typeof(HandshakeRejected), "HandshakeRejected")]
[JsonDerivedType(typeof(PingMessage),       "Ping")]
public abstract record NetMessage;

/// <summary>Client → Host: initial connection handshake. Host validates ProtocolVersion and assigns a side.</summary>
public sealed record HandshakeRequest(int ProtocolVersion, string ClientName) : NetMessage;

/// <summary>Host → Client: handshake accepted. Carries the session token used for future reconnect attempts.</summary>
public sealed record HandshakeAccepted(Guid SessionToken, PlayerSide AssignedSide) : NetMessage;

/// <summary>Host → Client: handshake rejected with a human-readable reason (version mismatch, slot full, etc.).</summary>
public sealed record HandshakeRejected(string Reason) : NetMessage;

/// <summary>Bidirectional liveness / latency probe. Carries sender's Time.GetTicksMsec() at send time.</summary>
public sealed record PingMessage(long SenderTimeMs) : NetMessage;
