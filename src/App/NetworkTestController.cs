using System;
using Godot;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Net;
using ShadowCardSmash.Net.Transport;

namespace ShadowCardSmash.App;

/// <summary>
/// Dev-only scene that exercises ENet handshake end-to-end without the rest of the game.
/// Two Godot instances on the same machine: one Hosts, the other Joins; pings can be exchanged
/// to verify wire format + transport. Used to validate Phase 3 plumbing before Phase 4 wires
/// the actual GameLoop through the network.
///
/// Not reachable from the production "Hot Seat" / "Deck Builder" paths.
/// </summary>
public partial class NetworkTestController : Control
{
    private EnetTransport _transport = null!;
    private RichTextLabel _logArea = null!;
    private LineEdit _addressInput = null!;
    private LineEdit _portInput = null!;
    private Label _statusLabel = null!;
    private Button _hostBtn = null!;
    private Button _joinBtn = null!;
    private Button _stopBtn = null!;
    private Button _pingBtn = null!;
    private Button _backBtn = null!;
    private int _connectedPeerId = -1;
    private Guid _sessionToken;
    private PlayerSide _assignedSide;

    public override void _Ready()
    {
        AnchorRight = 1; AnchorBottom = 1;
        BuildUi();
        _transport = new EnetTransport();
        AddChild(_transport);
        _transport.PeerConnected += OnPeerConnected;
        _transport.PeerDisconnected += OnPeerDisconnected;
        _transport.MessageReceived += OnMessageReceived;
        _transport.TransportError += err => Log($"[error] {err}");
        UpdateStatus("Idle");
    }

    private void BuildUi()
    {
        var bg = new ColorRect { Color = new Color(0.07f, 0.07f, 0.10f), AnchorRight = 1, AnchorBottom = 1 };
        AddChild(bg);

        var root = new VBoxContainer
        {
            AnchorRight = 1, AnchorBottom = 1,
            OffsetLeft = 24, OffsetTop = 16, OffsetRight = -24, OffsetBottom = -16,
        };
        root.AddThemeConstantOverride("separation", 12);
        AddChild(root);

        var title = new Label { Text = "Network Test (Dev) — ENet handshake smoke test" };
        title.AddThemeFontSizeOverride("font_size", 24);
        title.Modulate = new Color(0.9f, 0.85f, 1f);
        root.AddChild(title);

        _statusLabel = new Label { Text = "Idle" };
        _statusLabel.AddThemeFontSizeOverride("font_size", 16);
        _statusLabel.Modulate = new Color(1f, 0.95f, 0.7f);
        root.AddChild(_statusLabel);

        root.AddChild(new HSeparator());

        var addrRow = new HBoxContainer();
        addrRow.AddThemeConstantOverride("separation", 8);
        root.AddChild(addrRow);
        addrRow.AddChild(new Label { Text = "Address:" });
        _addressInput = new LineEdit { Text = "127.0.0.1", CustomMinimumSize = new Vector2(180, 32) };
        addrRow.AddChild(_addressInput);
        addrRow.AddChild(new Label { Text = "  Port:" });
        _portInput = new LineEdit { Text = NetSessionConfig.DefaultPort.ToString(), CustomMinimumSize = new Vector2(96, 32) };
        addrRow.AddChild(_portInput);

        var btnRow = new HBoxContainer();
        btnRow.AddThemeConstantOverride("separation", 8);
        root.AddChild(btnRow);

        _hostBtn = new Button { Text = "Host", CustomMinimumSize = new Vector2(120, 36) };
        _hostBtn.Pressed += OnHostPressed;
        btnRow.AddChild(_hostBtn);

        _joinBtn = new Button { Text = "Join", CustomMinimumSize = new Vector2(120, 36) };
        _joinBtn.Pressed += OnJoinPressed;
        btnRow.AddChild(_joinBtn);

        _stopBtn = new Button { Text = "Stop / Disconnect", CustomMinimumSize = new Vector2(160, 36), Disabled = true };
        _stopBtn.Pressed += OnStopPressed;
        btnRow.AddChild(_stopBtn);

        _pingBtn = new Button { Text = "Send Ping", CustomMinimumSize = new Vector2(120, 36), Disabled = true };
        _pingBtn.Pressed += OnPingPressed;
        btnRow.AddChild(_pingBtn);

        root.AddChild(new HSeparator());

        _logArea = new RichTextLabel
        {
            BbcodeEnabled = false,
            ScrollFollowing = true,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 400),
        };
        _logArea.AddThemeFontSizeOverride("normal_font_size", 13);
        root.AddChild(_logArea);

        _backBtn = new Button { Text = "← Back to Main Menu", CustomMinimumSize = new Vector2(200, 32) };
        _backBtn.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        root.AddChild(_backBtn);
    }

    private void OnHostPressed()
    {
        int port = int.TryParse(_portInput.Text, out var p) ? p : NetSessionConfig.DefaultPort;
        _transport.StartHost(port);
        UpdateStatus($"Hosting on port {port} (waiting for client)");
        Log($"[host] listening on port {port}");
        SetButtonsForRunning();
    }

    private void OnJoinPressed()
    {
        string addr = _addressInput.Text;
        int port = int.TryParse(_portInput.Text, out var p) ? p : NetSessionConfig.DefaultPort;
        _transport.StartClient(addr, port);
        UpdateStatus($"Connecting to {addr}:{port}...");
        Log($"[client] connecting to {addr}:{port}");
        SetButtonsForRunning();
    }

    private void OnStopPressed()
    {
        _transport.Stop();
        _connectedPeerId = -1;
        UpdateStatus("Idle");
        Log("[stop]");
        SetButtonsForIdle();
    }

    private void OnPingPressed()
    {
        if (_connectedPeerId < 0) return;
        var msg = new PingMessage(SenderTimeMs: (long)Time.GetTicksMsec());
        _transport.Send(_connectedPeerId, msg);
        Log($"[send → {_connectedPeerId}] Ping(t={msg.SenderTimeMs}ms)");
    }

    private void OnPeerConnected(int peerId)
    {
        _connectedPeerId = peerId;
        Log($"[event] PeerConnected id={peerId}");
        if (_transport.IsHost)
        {
            UpdateStatus($"Host — client {peerId} connected, awaiting handshake");
        }
        else
        {
            UpdateStatus($"Client — connected to server, sending handshake");
            var req = new HandshakeRequest(NetSessionConfig.ProtocolVersion, "TestClient");
            _transport.Send(peerId, req);
            Log($"[send → {peerId}] HandshakeRequest(v={req.ProtocolVersion}, name={req.ClientName})");
        }
        _pingBtn.Disabled = false;
    }

    private void OnPeerDisconnected(int peerId)
    {
        Log($"[event] PeerDisconnected id={peerId}");
        if (peerId == _connectedPeerId)
        {
            _connectedPeerId = -1;
            _pingBtn.Disabled = true;
        }
        UpdateStatus("Idle (peer disconnected)");
    }

    private void OnMessageReceived(int fromId, NetMessage msg)
    {
        switch (msg)
        {
            case HandshakeRequest req:
                Log($"[recv ← {fromId}] HandshakeRequest(v={req.ProtocolVersion}, name={req.ClientName})");
                if (req.ProtocolVersion != NetSessionConfig.ProtocolVersion)
                {
                    var reject = new HandshakeRejected(
                        $"Protocol version mismatch: client={req.ProtocolVersion}, host={NetSessionConfig.ProtocolVersion}");
                    _transport.Send(fromId, reject);
                    Log($"[send → {fromId}] HandshakeRejected ({reject.Reason})");
                    return;
                }
                var token = Guid.NewGuid();
                var assignedSide = PlayerSide.Second; // host always = First, client always = Second (V1)
                _transport.Send(fromId, new HandshakeAccepted(token, assignedSide));
                Log($"[send → {fromId}] HandshakeAccepted(token={token}, side={assignedSide})");
                UpdateStatus($"Host — handshake complete with client {fromId} (side={assignedSide})");
                break;

            case HandshakeAccepted accept:
                _sessionToken = accept.SessionToken;
                _assignedSide = accept.AssignedSide;
                Log($"[recv ← {fromId}] HandshakeAccepted(token={accept.SessionToken}, side={accept.AssignedSide})");
                UpdateStatus($"Client — handshake complete, you are {accept.AssignedSide}");
                break;

            case HandshakeRejected reject:
                Log($"[recv ← {fromId}] HandshakeRejected({reject.Reason})");
                UpdateStatus($"Rejected: {reject.Reason}");
                _transport.Stop();
                SetButtonsForIdle();
                break;

            case PingMessage ping:
                long now = (long)Time.GetTicksMsec();
                long rtt = now - ping.SenderTimeMs;
                Log($"[recv ← {fromId}] Ping(t={ping.SenderTimeMs}ms, observed rtt~{rtt}ms)");
                break;
        }
    }

    private void SetButtonsForRunning()
    {
        _hostBtn.Disabled = true;
        _joinBtn.Disabled = true;
        _stopBtn.Disabled = false;
    }

    private void SetButtonsForIdle()
    {
        _hostBtn.Disabled = false;
        _joinBtn.Disabled = false;
        _stopBtn.Disabled = true;
        _pingBtn.Disabled = true;
    }

    private void UpdateStatus(string msg) => _statusLabel.Text = msg;
    private void Log(string line) => _logArea.AppendText("\n" + line);
}
