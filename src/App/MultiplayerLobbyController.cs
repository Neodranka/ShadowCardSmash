using System;
using Godot;
using ShadowCardSmash.Cards;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;
using ShadowCardSmash.Net;
using ShadowCardSmash.Net.Transport;
using System.Reflection;
using ShadowCardSmash.Cards.Resources;

namespace ShadowCardSmash.App;

/// <summary>
/// Phase 4d: production multiplayer lobby (vs Phase 3's dev-only NetworkTest scene).
///
/// Flow:
///   Host:  Host button → ENet server up → wait for client → wait for handshake →
///          "Start Game" button enabled → click → init GameLoop locally → send StartGame to client →
///          reparent transport to /root (survives scene change) → switch to Battle scene
///   Client: Join button → connect → send HandshakeRequest → receive HandshakeAccepted →
///          status "Waiting for host..." → receive StartGame → reparent transport → switch to Battle scene
///
/// V1 simplifications (deferred to a Phase 4d-extension):
///   - No deck-pick UI in the lobby. Both sides use BattleController's default-deck fallback.
///   - No reconnect mid-lobby (that's Phase 7).
///   - No version-mismatch dialog beyond status text.
/// </summary>
public partial class MultiplayerLobbyController : Control
{
    private EnetTransport _transport = null!;
    private Label _statusLabel = null!;
    private RichTextLabel _logArea = null!;
    private Button _hostBtn = null!, _joinBtn = null!, _stopBtn = null!, _startBtn = null!, _backBtn = null!;
    private LineEdit _addressInput = null!, _portInput = null!;
    private Container _modePanel = null!;

    private int _connectedPeerId = -1;
    private bool _handshakeComplete;
    private PlayerSide _assignedSide;
    private bool _transitioning;

    public override void _Ready()
    {
        AnchorRight = 1; AnchorBottom = 1;
        BuildUi();
        _transport = new EnetTransport();
        AddChild(_transport);
        _transport.PeerConnected += OnPeerConnected;
        _transport.PeerDisconnected += OnPeerDisconnected;
        _transport.MessageReceived += OnNetMessage;
        _transport.TransportError += OnTransportError;
        UpdateStatus("选择 Host（开主机）或 Join（加入）");
    }

    private void OnTransportError(string err)
    {
        // Guarded: after TransitionToBattle this lobby instance is freed but the transport lives on
        // under /root and could still surface errors. Drop them silently — Battle scene wires its own.
        if (!IsInstanceValid(this) || !IsInstanceValid(_logArea)) return;
        Log($"[error] {err}");
    }

    public override void _ExitTree()
    {
        // If we're handing off to Battle, transport is already reparented and ownership transferred.
        // Otherwise (back-to-menu), clean it up.
        if (!_transitioning && _transport != null && GodotObject.IsInstanceValid(_transport))
        {
            _transport.Stop();
        }
    }

    private void BuildUi()
    {
        AddChild(new ColorRect { Color = new Color(0.07f, 0.07f, 0.10f), AnchorRight = 1, AnchorBottom = 1 });

        var root = new VBoxContainer
        {
            AnchorRight = 1, AnchorBottom = 1,
            OffsetLeft = 32, OffsetTop = 24, OffsetRight = -32, OffsetBottom = -24,
        };
        root.AddThemeConstantOverride("separation", 14);
        AddChild(root);

        var title = new Label { Text = "联机大厅 (Multiplayer Lobby)" };
        title.AddThemeFontSizeOverride("font_size", 28);
        title.Modulate = new Color(0.9f, 0.85f, 1f);
        root.AddChild(title);

        _statusLabel = new Label { Text = "" };
        _statusLabel.AddThemeFontSizeOverride("font_size", 17);
        _statusLabel.Modulate = new Color(1f, 0.95f, 0.7f);
        root.AddChild(_statusLabel);

        root.AddChild(new HSeparator());

        // Address / port row (used for both Host=listen-port and Join=server-addr).
        var addrRow = new HBoxContainer();
        addrRow.AddThemeConstantOverride("separation", 8);
        root.AddChild(addrRow);
        addrRow.AddChild(new Label { Text = "对方地址：" });
        _addressInput = new LineEdit { Text = "127.0.0.1", CustomMinimumSize = new Vector2(220, 32) };
        addrRow.AddChild(_addressInput);
        addrRow.AddChild(new Label { Text = "  端口：" });
        _portInput = new LineEdit { Text = NetSessionConfig.DefaultPort.ToString(), CustomMinimumSize = new Vector2(96, 32) };
        addrRow.AddChild(_portInput);

        // Mode buttons (Host / Join / Stop / Start / Back).
        _modePanel = new HBoxContainer();
        _modePanel.AddThemeConstantOverride("separation", 8);
        root.AddChild(_modePanel);

        _hostBtn = MakeBtn("Host (主机)", OnHostPressed);
        _modePanel.AddChild(_hostBtn);
        _joinBtn = MakeBtn("Join (加入)", OnJoinPressed);
        _modePanel.AddChild(_joinBtn);
        _stopBtn = MakeBtn("Stop / Disconnect", OnStopPressed, disabled: true);
        _modePanel.AddChild(_stopBtn);
        _startBtn = MakeBtn("Start Game (开始)", OnStartPressed, disabled: true);
        _startBtn.Modulate = new Color(0.7f, 1f, 0.7f);
        _modePanel.AddChild(_startBtn);

        root.AddChild(new HSeparator());

        _logArea = new RichTextLabel
        {
            BbcodeEnabled = false,
            ScrollFollowing = true,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 360),
        };
        _logArea.AddThemeFontSizeOverride("normal_font_size", 13);
        root.AddChild(_logArea);

        _backBtn = MakeBtn("← 返回主菜单", OnBackPressed);
        root.AddChild(_backBtn);
    }

    private static Button MakeBtn(string text, Action onPressed, bool disabled = false)
    {
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(160, 36), Disabled = disabled };
        b.Pressed += onPressed;
        return b;
    }

    // ---- User actions ----

    private void OnHostPressed()
    {
        int port = int.TryParse(_portInput.Text, out var p) ? p : NetSessionConfig.DefaultPort;
        _transport.StartHost(port);
        UpdateStatus($"主机模式 — 监听端口 {port}，等待对手加入...");
        Log($"[host] listening on {port}");
        _hostBtn.Disabled = true;
        _joinBtn.Disabled = true;
        _stopBtn.Disabled = false;
    }

    private void OnJoinPressed()
    {
        string addr = _addressInput.Text;
        int port = int.TryParse(_portInput.Text, out var p) ? p : NetSessionConfig.DefaultPort;
        // Persist for Phase 7 reconnect attempts inside Battle scene.
        BattleSetup.NetHostAddress = addr;
        BattleSetup.NetHostPort = port;
        _transport.StartClient(addr, port);
        UpdateStatus($"连接 {addr}:{port}...");
        Log($"[client] connecting to {addr}:{port}");
        _hostBtn.Disabled = true;
        _joinBtn.Disabled = true;
        _stopBtn.Disabled = false;
    }

    private void OnStopPressed()
    {
        _transport.Stop();
        _connectedPeerId = -1;
        _handshakeComplete = false;
        UpdateStatus("已断开");
        Log("[stop]");
        _hostBtn.Disabled = false;
        _joinBtn.Disabled = false;
        _stopBtn.Disabled = true;
        _startBtn.Disabled = true;
    }

    private void OnStartPressed()
    {
        if (!_transport.IsHost || !_handshakeComplete || _connectedPeerId < 0)
        {
            Log("[start] cannot start — handshake not complete");
            return;
        }
        Log("[host] initializing game state and sending StartGame to client...");

        // Build initial state with BOTH decks (Phase 4d V1: use default decks, no deck picker yet).
        var registry = CardRegistry.ScanAssembly(Assembly.GetExecutingAssembly());
        CardResourceLoader.AttachAll(registry);

        var state = new GameState();
        int seed = (int)Time.GetTicksMsec();
        var rng = new DeterministicRng(seed, counter: 0);
        var loop = new GameLoop(state, registry, rng);

        var (p1Cards, p1Class) = DefaultDeck();
        var (p2Cards, p2Class) = DefaultDeck();
        GameInitializer.Begin(loop, seed,
            new GameInitializer.SeatConfig(p1Cards, p1Class, null),
            new GameInitializer.SeatConfig(p2Cards, p2Class, null));
        loop.Submit(new MulliganAction(PlayerSide.First, Array.Empty<int>()));
        loop.Submit(new MulliganAction(PlayerSide.Second, Array.Empty<int>()));

        // Push to client — filter for client's view (mask host's hand/deck CardIds to Hidden).
        var clientSnapshot = state.FilterFor(PlayerSide.Second);
        _transport.Send(_connectedPeerId, new StartGame(clientSnapshot, ClientSide: PlayerSide.Second));
        Log($"[send → {_connectedPeerId}] StartGame (sides assigned: host=First, client=Second)");

        // Hand off the live loop + transport to BattleController via BattleSetup.
        TransitionToBattle(BattleMode.NetHost, hostLoop: loop, initialState: null, localSide: PlayerSide.First);
    }

    private void OnBackPressed()
    {
        if (_transport != null) _transport.Stop();
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }

    // ---- Network event handlers ----

    private void OnPeerConnected(int peerId)
    {
        _connectedPeerId = peerId;
        Log($"[event] PeerConnected id={peerId}");
        if (_transport.IsHost)
        {
            UpdateStatus($"对手已连接（id={peerId}），等待握手...");
        }
        else
        {
            UpdateStatus("已连接服务器，发送握手中...");
            _transport.Send(peerId, new HandshakeRequest(NetSessionConfig.ProtocolVersion, "Player"));
            Log("[send] HandshakeRequest");
        }
    }

    private void OnPeerDisconnected(int peerId)
    {
        Log($"[event] PeerDisconnected id={peerId}");
        if (peerId == _connectedPeerId)
        {
            _connectedPeerId = -1;
            _handshakeComplete = false;
        }
        UpdateStatus("对方断开");
        _startBtn.Disabled = true;
    }

    private void OnNetMessage(int fromId, NetMessage msg)
    {
        switch (msg)
        {
            case HandshakeRequest req:
                Log($"[recv ← {fromId}] HandshakeRequest(v={req.ProtocolVersion}, name={req.ClientName})");
                if (req.ProtocolVersion != NetSessionConfig.ProtocolVersion)
                {
                    var reject = new HandshakeRejected(
                        $"协议版本不匹配：client={req.ProtocolVersion} host={NetSessionConfig.ProtocolVersion}");
                    _transport.Send(fromId, reject);
                    Log($"[send] HandshakeRejected");
                    return;
                }
                _handshakeComplete = true;
                var token = Guid.NewGuid();
                BattleSetup.NetSessionToken = token; // host stores own copy for Phase 7 reconnect
                _transport.Send(fromId, new HandshakeAccepted(token, PlayerSide.Second));
                Log($"[send → {fromId}] HandshakeAccepted (client assigned Second)");
                UpdateStatus("握手完成 — 点击 Start Game 开始对局");
                _startBtn.Disabled = false;
                break;

            case HandshakeAccepted accept:
                Log($"[recv ← {fromId}] HandshakeAccepted(side={accept.AssignedSide})");
                _handshakeComplete = true;
                _assignedSide = accept.AssignedSide;
                BattleSetup.NetSessionToken = accept.SessionToken; // client persists for Phase 7 reconnect
                UpdateStatus($"握手完成 — 你的位置：{accept.AssignedSide}。等待主机开始游戏...");
                break;

            case HandshakeRejected reject:
                Log($"[recv ← {fromId}] HandshakeRejected: {reject.Reason}");
                UpdateStatus($"被拒绝：{reject.Reason}");
                _transport.Stop();
                _stopBtn.Disabled = true;
                _hostBtn.Disabled = false;
                _joinBtn.Disabled = false;
                break;

            case StartGame sg:
                Log($"[recv ← {fromId}] StartGame (you play {sg.ClientSide})");
                UpdateStatus("收到游戏开始 — 进入战斗...");
                TransitionToBattle(BattleMode.NetClient, hostLoop: null,
                    initialState: sg.InitialState, localSide: sg.ClientSide);
                break;
        }
    }

    // ---- Scene transition / ownership handoff ----

    private void TransitionToBattle(BattleMode mode, GameLoop? hostLoop, GameState? initialState, PlayerSide localSide)
    {
        _transitioning = true;
        // Unwire our handlers so the freed lobby doesn't get callbacks on the soon-to-be-orphaned transport.
        _transport.PeerConnected -= OnPeerConnected;
        _transport.PeerDisconnected -= OnPeerDisconnected;
        _transport.MessageReceived -= OnNetMessage;
        _transport.TransportError -= OnTransportError;

        // Reparent transport from this scene to /root so it survives scene change.
        RemoveChild(_transport);
        GetTree().Root.AddChild(_transport);

        BattleSetup.Mode = mode;
        BattleSetup.NetTransport = _transport;
        BattleSetup.NetLocalSide = localSide;
        BattleSetup.NetInitialState = initialState;

        // Host hands the live loop directly (no extra wire to itself). Stash by static for the same reason.
        if (mode == BattleMode.NetHost && hostLoop != null)
        {
            BattleSetup.PendingHostLoop = hostLoop;
        }

        GetTree().ChangeSceneToFile("res://scenes/Battle.tscn");
    }

    private void UpdateStatus(string s) => _statusLabel.Text = s;
    private void Log(string line) => _logArea.AppendText("\n" + line);

    private static (System.Collections.Generic.List<CardId> cards, HeroClass cls) DefaultDeck()
    {
        var list = new System.Collections.Generic.List<CardId>();
        for (int i = 0; i < 20; i++) list.Add(new CardId(2001));
        for (int i = 0; i < 20; i++) list.Add(new CardId(1001));
        return (list, HeroClass.Forsaken);
    }
}
