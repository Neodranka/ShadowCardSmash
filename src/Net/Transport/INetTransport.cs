using System;

namespace ShadowCardSmash.Net.Transport;

/// <summary>
/// Abstraction over the actual network transport (ENet for V1, can be swapped for in-memory loopback
/// for integration tests or WebSocket for browser play later).
///
/// Lifecycle:
///   • <see cref="StartHost"/> / <see cref="StartClient"/> open a peer.
///   • Connection events arrive via <see cref="PeerConnected"/> / <see cref="PeerDisconnected"/>.
///   • Incoming wire messages arrive via <see cref="MessageReceived"/>.
///   • <see cref="Send"/> targets a specific peer; <see cref="Broadcast"/> sends to all.
///   • <see cref="Stop"/> closes the peer and clears state.
///
/// Concrete implementations are responsible for draining the underlying peer each frame and dispatching
/// signals; the consumer (NetSession / scene controller) just listens.
/// </summary>
public interface INetTransport
{
    event Action<int>? PeerConnected;
    event Action<int>? PeerDisconnected;
    event Action<int, NetMessage>? MessageReceived;
    event Action<string>? TransportError;

    bool IsHost { get; }
    bool IsRunning { get; }

    void StartHost(int port);
    void StartClient(string address, int port);
    void Stop();

    void Send(int peerId, NetMessage message);
    void Broadcast(NetMessage message);
}
