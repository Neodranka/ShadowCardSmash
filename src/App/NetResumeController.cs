using System;
using Godot;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Net;
using ShadowCardSmash.Net.Transport;

namespace ShadowCardSmash.App;

/// <summary>
/// Resume a client-side session after an app restart. Loads the persisted session file (token +
/// host address + assigned side), reconnects to host, sends ReconnectRequest, and on
/// ReconnectAccepted stashes the fresh state into BattleSetup and switches to Battle.tscn.
///
/// If reconnect fails (bad token, host down, or timeout), clears the file and returns to the
/// main menu — the session is definitively over.
/// </summary>
public partial class NetResumeController : Control
{
    private const double ConnectTimeoutSeconds = 15.0;

    private EnetTransport _transport = null!;
    private PersistedNetSession _persisted = null!;
    private Label _statusLabel = null!;
    private RichTextLabel _logArea = null!;
    private Button _backBtn = null!;
    private double _elapsed;
    private bool _handedOff;
    private bool _requestSent;

    public override void _Ready()
    {
        AnchorRight = 1; AnchorBottom = 1;
        BuildUi();

        var loaded = PersistedNetSession.Load();
        if (loaded is null)
        {
            UpdateStatus("没有可恢复的对局");
            _backBtn.Visible = true;
            return;
        }
        _persisted = loaded;
        UpdateStatus($"重连中 {_persisted.HostAddress}:{_persisted.HostPort} ...");
        Log($"[resume] token={_persisted.Token.ToString().Substring(0, 8)}... side={_persisted.AssignedSide}");

        _transport = new EnetTransport();
        AddChild(_transport);
        _transport.PeerConnected += OnPeerConnected;
        _transport.PeerDisconnected += OnPeerDisconnected;
        _transport.MessageReceived += OnNetMessage;
        _transport.TransportError += err => Log($"[error] {err}");
        _transport.StartClient(_persisted.HostAddress, _persisted.HostPort);
    }

    public override void _ExitTree()
    {
        if (!_handedOff && _transport != null && GodotObject.IsInstanceValid(_transport))
        {
            _transport.Stop();
        }
    }

    public override void _Process(double delta)
    {
        if (_handedOff) return;
        _elapsed += delta;
        if (_elapsed > ConnectTimeoutSeconds && !_requestSent)
        {
            UpdateStatus("连接超时 — 对局已失效");
            Log("[resume] timeout, giving up");
            PersistedNetSession.Clear();
            _backBtn.Visible = true;
            _transport?.Stop();
        }
    }

    private void OnPeerConnected(int peerId)
    {
        Log($"[event] PeerConnected id={peerId}");
        _transport.Send(peerId, new ReconnectRequest(_persisted.Token));
        _requestSent = true;
        UpdateStatus("已连接，验证 session token...");
    }

    private void OnPeerDisconnected(int peerId)
    {
        Log($"[event] PeerDisconnected id={peerId}");
        if (!_handedOff)
        {
            UpdateStatus("对方断开");
        }
    }

    private void OnNetMessage(int fromId, NetMessage msg)
    {
        switch (msg)
        {
            case ReconnectAccepted acc:
                Log($"[recv] ReconnectAccepted (side={acc.AssignedSide})");
                UpdateStatus("重连成功，进入战斗...");
                HandOffToBattle(acc);
                break;
            case ReconnectRejected rej:
                Log($"[recv] ReconnectRejected: {rej.Reason}");
                UpdateStatus($"重连被拒：{rej.Reason}");
                PersistedNetSession.Clear();
                _backBtn.Visible = true;
                _transport?.Stop();
                break;
        }
    }

    private void HandOffToBattle(ReconnectAccepted acc)
    {
        _handedOff = true;
        _transport.PeerConnected -= OnPeerConnected;
        _transport.PeerDisconnected -= OnPeerDisconnected;
        _transport.MessageReceived -= OnNetMessage;

        RemoveChild(_transport);
        GetTree().Root.AddChild(_transport);

        BattleSetup.Mode = BattleMode.NetClient;
        BattleSetup.NetTransport = _transport;
        BattleSetup.NetLocalSide = acc.AssignedSide;
        BattleSetup.NetInitialState = acc.State;
        BattleSetup.NetSessionToken = _persisted.Token;
        BattleSetup.NetHostAddress = _persisted.HostAddress;
        BattleSetup.NetHostPort = _persisted.HostPort;

        GetTree().ChangeSceneToFile("res://scenes/Battle.tscn");
    }

    private void BuildUi()
    {
        AddChild(new ColorRect { Color = new Color(0.07f, 0.07f, 0.10f), AnchorRight = 1, AnchorBottom = 1 });
        var root = new VBoxContainer
        {
            AnchorRight = 1, AnchorBottom = 1,
            OffsetLeft = 32, OffsetTop = 24, OffsetRight = -32, OffsetBottom = -24,
        };
        root.AddThemeConstantOverride("separation", 12);
        AddChild(root);

        var title = new Label { Text = "重连上一局 (Resume)" };
        title.AddThemeFontSizeOverride("font_size", 28);
        title.Modulate = new Color(0.9f, 0.85f, 1f);
        root.AddChild(title);

        _statusLabel = new Label { Text = "" };
        _statusLabel.AddThemeFontSizeOverride("font_size", 17);
        _statusLabel.Modulate = new Color(1f, 0.95f, 0.7f);
        root.AddChild(_statusLabel);

        root.AddChild(new HSeparator());

        _logArea = new RichTextLabel
        {
            BbcodeEnabled = false,
            ScrollFollowing = true,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 320),
        };
        _logArea.AddThemeFontSizeOverride("normal_font_size", 13);
        root.AddChild(_logArea);

        _backBtn = new Button { Text = "← 返回主菜单", CustomMinimumSize = new Vector2(200, 36), Visible = false };
        _backBtn.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        root.AddChild(_backBtn);
    }

    private void UpdateStatus(string s) => _statusLabel.Text = s;
    private void Log(string line) => _logArea.AppendText("\n" + line);
}
