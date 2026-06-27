using System;
using Godot;
using ShadowCardSmash.Net.Serialization;

namespace ShadowCardSmash.Net.Transport;

/// <summary>
/// ENet implementation of <see cref="INetTransport"/> using Godot 4's <see cref="ENetMultiplayerPeer"/>
/// directly (no MultiplayerApi integration — we own packet pump and dispatch).
///
/// Threading: Godot's _Process pumps the peer once per frame on the main thread, so all events are
/// surfaced synchronously to the UI/controller layer; no locking needed in consumers.
///
/// Wire format: UTF-8 JSON of <see cref="NetMessage"/>, channel/reliability defaults
/// (reliable + ordered, the safe pick for a turn-based card game where every byte matters semantically).
/// </summary>
public partial class EnetTransport : Node, INetTransport
{
	public event Action<int>? PeerConnected;
	public event Action<int>? PeerDisconnected;
	public event Action<int, NetMessage>? MessageReceived;
	public event Action<string>? TransportError;

	public bool IsHost { get; private set; }
	public bool IsRunning => _peer is not null;

	private ENetMultiplayerPeer? _peer;
	// Tracks the previous frame's connection status so we can detect Connecting→Disconnected as a
	// failed connection attempt (Godot's `connection_failed` lives on MultiplayerApi, not the peer).
	private MultiplayerPeer.ConnectionStatus _lastStatus = MultiplayerPeer.ConnectionStatus.Disconnected;

	public void StartHost(int port)
	{
		Stop();
		var peer = new ENetMultiplayerPeer();
		var err = peer.CreateServer(port, NetSessionConfig.MaxPlayers);
		if (err != Error.Ok)
		{
			TransportError?.Invoke($"CreateServer({port}) failed: {err}");
			return;
		}
		AttachPeer(peer, isHost: true);
	}

	public void StartClient(string address, int port)
	{
		Stop();
		var peer = new ENetMultiplayerPeer();
		var err = peer.CreateClient(address, port);
		if (err != Error.Ok)
		{
			TransportError?.Invoke($"CreateClient({address}:{port}) failed: {err}");
			return;
		}
		AttachPeer(peer, isHost: false);
	}

	public void Stop()
	{
		if (_peer is null) return;
		_peer.PeerConnected -= OnPeerConnected;
		_peer.PeerDisconnected -= OnPeerDisconnected;
		_peer.Close();
		_peer = null;
		IsHost = false;
		_lastStatus = MultiplayerPeer.ConnectionStatus.Disconnected;
	}

	public void Send(int peerId, NetMessage message)
	{
		if (_peer is null) { TransportError?.Invoke("Send called before Start."); return; }
		var bytes = NetMessageJson.SerializeToBytes(message);
		_peer.SetTargetPeer(peerId);
		var err = _peer.PutPacket(bytes);
		GD.Print($"[Net.Send] -> peer={peerId} type={message.GetType().Name} bytes={bytes.Length} err={err}");
		if (err != Error.Ok) TransportError?.Invoke($"PutPacket -> {peerId} failed: {err}");
	}

	/// <summary>Broadcast to all connected peers (ENet peer id 0 = "all").</summary>
	public void Broadcast(NetMessage message) => Send(0, message);

	public override void _Process(double delta)
	{
		if (_peer is null) return;
		_peer.Poll();
		if (_peer is null) return; // Poll() can synchronously fire signals that call Stop().

		// Synthetic "connection failed" detection: transitioned out of Connecting without ever reaching Connected.
		var status = _peer.GetConnectionStatus();
		if (_lastStatus == MultiplayerPeer.ConnectionStatus.Connecting
			&& status == MultiplayerPeer.ConnectionStatus.Disconnected)
		{
			TransportError?.Invoke("Connection failed (server unreachable or refused).");
		}
		if (_peer is null) return;
		_lastStatus = status;

		// Re-check _peer each iteration: a MessageReceived handler may call Stop() (e.g., on
		// HandshakeRejected) which nulls _peer; without the guard the next GetAvailablePacketCount NREs.
		while (_peer is not null && _peer.GetAvailablePacketCount() > 0)
		{
			int fromId = _peer.GetPacketPeer();
			byte[] bytes = _peer.GetPacket();
			try
			{
				var msg = NetMessageJson.DeserializeFromBytes(bytes);
				int subs = MessageReceived?.GetInvocationList().Length ?? 0;
				GD.Print($"[Net.Recv] <- peer={fromId} type={msg.GetType().Name} bytes={bytes.Length} subs={subs}");
				MessageReceived?.Invoke(fromId, msg);
			}
			catch (Exception e)
			{
				GD.PrintErr($"[Net.Recv] decode FAILED from peer {fromId}: {e}");
				TransportError?.Invoke($"Decode error from peer {fromId}: {e.Message}");
			}
		}
	}

	// IMPORTANT: do NOT auto-Stop in _ExitTree. Godot's Reparent() (and RemoveChild+AddChild) fires
	// _ExitTree during the parent change — auto-stopping there would tear down the ENet peer the moment
	// we hand off this Node from lobby to /root, dropping in-flight packets (e.g., StartGame) and
	// leaving Battle scene with a dead transport. NotificationPredelete only fires when the node is
	// actually being freed, which is the right time to release the peer.
	public override void _Notification(int what)
	{
		base._Notification(what);
		if (what == NotificationPredelete) Stop();
	}

	private void AttachPeer(ENetMultiplayerPeer peer, bool isHost)
	{
		_peer = peer;
		IsHost = isHost;
		peer.PeerConnected += OnPeerConnected;
		peer.PeerDisconnected += OnPeerDisconnected;
		_lastStatus = peer.GetConnectionStatus();
	}

	private void OnPeerConnected(long id) => PeerConnected?.Invoke((int)id);
	private void OnPeerDisconnected(long id) => PeerDisconnected?.Invoke((int)id);
}
