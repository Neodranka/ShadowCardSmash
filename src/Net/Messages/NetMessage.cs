using System;
using System.Text.Json.Serialization;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Net;

/// <summary>
/// Wire-format envelope for everything sent over the network. Polymorphic via "$type" discriminator,
/// short PascalCase tags. Adding a new wire message requires (a) a new [JsonDerivedType] entry here,
/// (b) round-trip unit test in NetMessageJsonTests, (c) bump <see cref="NetSessionConfig.ProtocolVersion"/>.
///
/// Phase 3: handshake + ping
/// Phase 4: action request / applied / rejected (authoritative host loop)
/// Phase 5+: snapshot push, reconnect, disconnect notices
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(HandshakeRequest),  "HandshakeRequest")]
[JsonDerivedType(typeof(HandshakeAccepted), "HandshakeAccepted")]
[JsonDerivedType(typeof(HandshakeRejected), "HandshakeRejected")]
[JsonDerivedType(typeof(PingMessage),       "Ping")]
[JsonDerivedType(typeof(ActionRequest),     "ActionRequest")]
[JsonDerivedType(typeof(ActionApplied),     "ActionApplied")]
[JsonDerivedType(typeof(ActionRejected),    "ActionRejected")]
[JsonDerivedType(typeof(StartGame),         "StartGame")]
[JsonDerivedType(typeof(ReconnectRequest),  "ReconnectRequest")]
[JsonDerivedType(typeof(ReconnectAccepted), "ReconnectAccepted")]
[JsonDerivedType(typeof(ReconnectRejected), "ReconnectRejected")]
public abstract record NetMessage;

/// <summary>Client → Host: initial connection handshake. Host validates ProtocolVersion and assigns a side.</summary>
public sealed record HandshakeRequest(int ProtocolVersion, string ClientName) : NetMessage;

/// <summary>Host → Client: handshake accepted. Carries the session token used for future reconnect attempts.</summary>
public sealed record HandshakeAccepted(Guid SessionToken, PlayerSide AssignedSide) : NetMessage;

/// <summary>Host → Client: handshake rejected with a human-readable reason (version mismatch, slot full, etc.).</summary>
public sealed record HandshakeRejected(string Reason) : NetMessage;

/// <summary>Bidirectional liveness / latency probe. Carries sender's Time.GetTicksMsec() at send time.</summary>
public sealed record PingMessage(long SenderTimeMs) : NetMessage;

/// <summary>Client → Host: "please apply this action on my behalf". Carries a monotonic ClientRequestId
/// so the rejection (if any) can be matched back to the originating UI request.</summary>
public sealed record ActionRequest(long ClientRequestId, IGameAction Action) : NetMessage;

/// <summary>Host → ALL: "this action was applied, here are the events to animate and the post-state to sync to".
/// Carries Sequence (monotonic per host) so clients can detect gaps/duplicates and the original request id
/// (if from a client) so the originator can match it to its UI flow.</summary>
public sealed record ActionApplied(
    long Sequence,
    long? OriginatingRequestId,
    IGameAction Action,
    BoardEvent[] Events,
    GameState StateAfter
) : NetMessage;

/// <summary>Host → originating client: "your action failed validation". Empty for host's own actions
/// (host validates locally before broadcasting; it never rejects itself).</summary>
public sealed record ActionRejected(long ClientRequestId, IGameAction Action, string Reason) : NetMessage;

/// <summary>Host → Client: "battle is starting, here is the initial state, you play this side".
/// Sent after lobby handshake completes and host runs GameInitializer + mulligans locally.
/// Client uses this to seed its mirror state and transition to the Battle scene.</summary>
public sealed record StartGame(GameState InitialState, PlayerSide ClientSide) : NetMessage;

/// <summary>Client → Host during grace: "I'm the peer that dropped, here is my session token,
/// please put me back". Host validates token; on match replies with <see cref="ReconnectAccepted"/>.</summary>
public sealed record ReconnectRequest(Guid SessionToken) : NetMessage;

/// <summary>Host → reconnecting client: token OK, here is the current filtered state so you can
/// resume where you left off. Assigned side re-sent for good measure (should match original).</summary>
public sealed record ReconnectAccepted(GameState State, PlayerSide AssignedSide) : NetMessage;

/// <summary>Host → reconnecting client: token mismatch, session already expired, or some other reason.
/// Client falls back to forfeit / back-to-menu.</summary>
public sealed record ReconnectRejected(string Reason) : NetMessage;
