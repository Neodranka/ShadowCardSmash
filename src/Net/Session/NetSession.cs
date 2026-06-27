using System;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;
using ShadowCardSmash.Net.Transport;

namespace ShadowCardSmash.Net.Session;

/// <summary>
/// Authoritative-host gameplay session. One per game on each peer; orchestrates the GameLoop (on host)
/// and dispatches <see cref="ActionApplied"/> / <see cref="ActionRejected"/> events to whatever UI
/// listens (BattleController).
///
/// Three operating modes:
///   • <see cref="Mode.HotseatLoopback"/> — single process, no transport, both players use the same
///     screen. Used by current hotseat scene; preserves the "host-authoritative" data flow so the
///     same BattleController code drives all modes.
///   • <see cref="Mode.NetHost"/> — owns the real GameLoop, accepts ActionRequest from one remote
///     client, broadcasts ActionApplied to all peers (and fires the same event locally).
///   • <see cref="Mode.NetClient"/> — no GameLoop; mirror state is replaced wholesale on each
///     ActionApplied received from host. SubmitLocalAction sends ActionRequest to host instead.
///
/// Design note: all paths go through <see cref="HandleActionRequest"/> on the host. Local host actions
/// take the same code path as remote ones (fromPeerId = SelfPeer). This is the foundation for
/// "swap in a dedicated server later without rewriting BattleController" (option (i) in the design
/// discussion).
/// </summary>
public sealed class NetSession
{
    public enum Mode { HotseatLoopback, NetHost, NetClient }

    private const int SelfPeer = -1;
    private const int HostPeer = 1; // ENet server peer id by convention

    public Mode SessionMode { get; }
    /// <summary>The side this peer's UI represents. Null in hotseat (both sides local).</summary>
    public PlayerSide? LocalSide { get; }

    /// <summary>Current truth: host's loop state for HotseatLoopback/NetHost; mirror for NetClient.</summary>
    public GameState State => SessionMode == Mode.NetClient ? _clientMirror : _hostLoop!.State;

    /// <summary>Fires whenever a fresh ActionApplied is realized locally (after host applies, or after
    /// client receives broadcast). UI animates events and rebinds in response.</summary>
    public event Action<ActionApplied>? ActionApplied;

    /// <summary>Fires when an action this peer submitted was rejected by the host (validation failure).</summary>
    public event Action<ActionRejected>? ActionRejected;

    /// <summary>Forwarded from the underlying transport. In V1 1v1 this means the game-pair peer dropped
    /// — host's only client, or client's server. UI uses this to start the disconnect-grace timer.
    /// Never fires in HotseatLoopback (no transport).</summary>
    public event Action? PeerDisconnected;

    private readonly GameLoop? _hostLoop;
    private GameState _clientMirror = new();
    private readonly INetTransport? _transport;
    private long _hostSequence;
    private long _clientNextRequestId;

    private NetSession(Mode mode, GameLoop? hostLoop, INetTransport? transport, PlayerSide? localSide)
    {
        SessionMode = mode;
        _hostLoop = hostLoop;
        _transport = transport;
        LocalSide = localSide;
        if (transport != null)
        {
            transport.MessageReceived += OnTransportMessage;
            transport.PeerDisconnected += _ => PeerDisconnected?.Invoke();
        }
    }

    public static NetSession CreateHotseatLoopback(GameLoop loop) =>
        new(Mode.HotseatLoopback, loop, transport: null, localSide: null);

    public static NetSession CreateNetHost(GameLoop loop, INetTransport transport, PlayerSide localSide) =>
        new(Mode.NetHost, loop, transport, localSide);

    public static NetSession CreateNetClient(INetTransport transport, PlayerSide localSide, GameState initialState)
    {
        var s = new NetSession(Mode.NetClient, hostLoop: null, transport, localSide);
        s._clientMirror = initialState;
        return s;
    }

    public bool IsHost => SessionMode != Mode.NetClient;

    /// <summary>
    /// Submit an action originating from local UI. On host (incl. hotseat) goes through validation +
    /// loop apply + broadcast immediately. On client, serializes an <see cref="Messages.ActionRequest"/>
    /// over the wire to host and awaits the ActionApplied / ActionRejected callback.
    /// Returns the request id used (clients can match against ActionRejected.ClientRequestId).
    /// </summary>
    public long SubmitLocalAction(IGameAction action)
    {
        long requestId = ++_clientNextRequestId;
        if (IsHost)
        {
            HandleActionRequest(action, requestId, SelfPeer);
        }
        else
        {
            _transport!.Send(HostPeer, new ActionRequest(requestId, action));
        }
        return requestId;
    }

    private void HandleActionRequest(IGameAction action, long requestId, int fromPeerId)
    {
        // Validate first using the action's own contract.
        var validate = action.Validate(_hostLoop!.State);
        if (!validate.IsOk)
        {
            DispatchRejected(new ActionRejected(requestId, action, validate.Reason ?? "(no reason)"), fromPeerId);
            return;
        }

        int eventsBefore = _hostLoop.EventLog.Count;
        try
        {
            _hostLoop.Submit(action);
        }
        catch (InvalidActionException e)
        {
            DispatchRejected(new ActionRejected(requestId, action, e.Message), fromPeerId);
            return;
        }
        catch (Exception e)
        {
            DispatchRejected(new ActionRejected(requestId, action, $"Engine error: {e.Message}"), fromPeerId);
            return;
        }

        int newCount = _hostLoop.EventLog.Count - eventsBefore;
        var newEvents = new BoardEvent[newCount];
        for (int i = 0; i < newCount; i++)
            newEvents[i] = _hostLoop.EventLog[eventsBefore + i];

        long seq = ++_hostSequence;
        long? originatingId = fromPeerId == SelfPeer ? null : requestId;

        // Host-local: unfiltered (host sees everything).
        var hostApplied = new ActionApplied(
            Sequence: seq,
            OriginatingRequestId: originatingId,
            Action: action,
            Events: newEvents,
            StateAfter: _hostLoop.State.Snapshot());
        ActionApplied?.Invoke(hostApplied);

        // Remote clients: hide opponent's private zones + redact card-draw events.
        // V1 1v1: there's exactly one remote viewer = host's opposite side.
        if (_transport != null && LocalSide.HasValue)
        {
            var clientSide = LocalSide.Value.Opponent();
            var clientApplied = new ActionApplied(
                Sequence: seq,
                OriginatingRequestId: originatingId,
                Action: action,
                Events: EventFilter.FilterAll(newEvents, clientSide),
                StateAfter: hostApplied.StateAfter.FilterFor(clientSide));
            _transport.Broadcast(clientApplied);
        }
    }

    private void DispatchRejected(ActionRejected reject, int targetPeerId)
    {
        if (targetPeerId == SelfPeer)
            ActionRejected?.Invoke(reject);
        else
            _transport?.Send(targetPeerId, reject);
    }

    private void OnTransportMessage(int fromPeerId, NetMessage msg)
    {
        switch (msg)
        {
            case ActionRequest req when IsHost:
                HandleActionRequest(req.Action, req.ClientRequestId, fromPeerId);
                break;
            case ActionApplied applied when !IsHost:
                _clientMirror = applied.StateAfter;
                ActionApplied?.Invoke(applied);
                break;
            case ActionRejected rej when !IsHost:
                ActionRejected?.Invoke(rej);
                break;
            // Handshake / Ping are lobby-layer concerns, not consumed by NetSession.
        }
    }
}
