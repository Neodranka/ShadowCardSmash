using System;

namespace ShadowCardSmash.Net.Transport;

/// <summary>
/// Synchronous, in-process loopback transport. Used by unit tests to wire two NetSessions together
/// without spinning up a real ENet socket. Send() invokes the peer's MessageReceived callback on the
/// same thread before returning — every operation is deterministic.
///
/// NOT for production use: bypasses serialization (so wire-format bugs won't surface here; pair these
/// tests with the round-trip JSON tests for full coverage).
/// </summary>
public sealed class InMemoryNetTransport : INetTransport
{
#pragma warning disable CS0067 // PeerConnected / TransportError unused — kept for INetTransport interface parity.
    public event Action<int>? PeerConnected;
    public event Action<int>? PeerDisconnected;
    public event Action<int, NetMessage>? MessageReceived;
    public event Action<string>? TransportError;
#pragma warning restore CS0067

    public bool IsHost { get; private set; }
    public bool IsRunning { get; private set; } = true;

    private InMemoryNetTransport? _peer;
    private int _myIdFromPeerPerspective;

    public void StartHost(int port) { IsHost = true; IsRunning = true; }
    public void StartClient(string address, int port) { IsHost = false; IsRunning = true; }
    public void Stop()
    {
        IsRunning = false;
        var p = _peer;
        _peer = null;
        // Notify the other side that we dropped, mirroring ENet's behaviour.
        p?.PeerDisconnected?.Invoke(_myIdFromPeerPerspective);
    }

    public void Send(int peerId, NetMessage message)
    {
        if (_peer is null || !_peer.IsRunning) return;
        // peerId is the recipient's id from MY perspective (or 0 = broadcast to all).
        // We deliver to the peer with MY id from THEIR perspective so handlers can identify the sender.
        _peer.MessageReceived?.Invoke(_myIdFromPeerPerspective, message);
    }

    public void Broadcast(NetMessage message) => Send(0, message);

    /// <summary>
    /// Test-only: wire up two transports as a host/client pair. Host sees the client at id 999,
    /// client sees the host at id 1 (matches ENet's server-id-1 convention).
    /// PeerConnected events fire synchronously before returning so tests can subscribe after.
    /// </summary>
    public static (InMemoryNetTransport host, InMemoryNetTransport client) CreatePair()
    {
        var host = new InMemoryNetTransport { IsHost = true, IsRunning = true };
        var client = new InMemoryNetTransport { IsHost = false, IsRunning = true };
        host._peer = client;
        client._peer = host;
        host._myIdFromPeerPerspective = 1;   // host appears as id=1 to client (ENet server convention)
        client._myIdFromPeerPerspective = 999; // client appears as id=999 to host (arbitrary)
        return (host, client);
    }
}
