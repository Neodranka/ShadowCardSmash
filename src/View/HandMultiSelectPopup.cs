using Godot;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.View;

/// <summary>
/// Modal popup that shows a hand card grid where each card can be toggled selected. Used by cards
/// with TargetSpec.MultipleFromHand (塔尔莫维奇商队 / 摄政议会 / 拖延议程 / 利害权衡 branch B).
/// 0 selections is legal; the confirm button is always enabled.
/// </summary>
public partial class HandMultiSelectPopup : Control
{
    [Signal] public delegate void ConfirmedEventHandler(int[] selectedInstanceIds);
    [Signal] public delegate void CancelledEventHandler();

    private readonly System.Collections.Generic.HashSet<int> _selected = new();
    private GridContainer _grid = null!;
    private Label _title = null!;
    private Label _countLabel = null!;
    private System.Collections.Generic.Dictionary<int, StyleBoxFlat> _cardBoxes = new();
    private System.Collections.Generic.Dictionary<int, PanelContainer> _cardWraps = new();
    private bool _builtUi;
    private int _minPick;
    private int _maxPick = int.MaxValue;

    public override void _Ready()
    {
        AnchorRight = 1; AnchorBottom = 1;
        BuildUi();
        ProcessMode = ProcessModeEnum.Always;
    }

    private void BuildUi()
    {
        if (_builtUi) return;
        _builtUi = true;

        var dim = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.6f),
            AnchorRight = 1, AnchorBottom = 1,
            MouseFilter = MouseFilterEnum.Stop,
        };
        AddChild(dim);

        var panel = new PanelContainer
        {
            AnchorLeft = 0.5f, AnchorTop = 0.5f, AnchorRight = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft = -540, OffsetTop = -370, OffsetRight = 540, OffsetBottom = 370,
        };
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.08f, 0.16f, 0.97f),
            BorderColor = new Color(0.7f, 0.6f, 0.9f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 14, ContentMarginBottom = 14,
        };
        panel.AddThemeStyleboxOverride("panel", sb);
        AddChild(panel);

        var vb = new VBoxContainer();
        vb.AddThemeConstantOverride("separation", 10);
        panel.AddChild(vb);

        var header = new HBoxContainer();
        vb.AddChild(header);
        _title = new Label { Text = "选择手牌", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _title.AddThemeFontSizeOverride("font_size", 22);
        _title.Modulate = new Color(0.95f, 0.9f, 1f);
        header.AddChild(_title);

        _countLabel = new Label { Text = "" };
        _countLabel.AddThemeFontSizeOverride("font_size", 16);
        _countLabel.Modulate = new Color(1f, 0.95f, 0.7f);
        header.AddChild(_countLabel);

        vb.AddChild(new HSeparator());

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        vb.AddChild(scroll);
        _grid = new GridContainer { Columns = 5 };
        _grid.AddThemeConstantOverride("h_separation", 8);
        _grid.AddThemeConstantOverride("v_separation", 8);
        scroll.AddChild(_grid);

        var btnRow = new HBoxContainer();
        btnRow.AddThemeConstantOverride("separation", 12);
        btnRow.Alignment = BoxContainer.AlignmentMode.End;
        vb.AddChild(btnRow);

        var cancelBtn = new Button { Text = "取消", CustomMinimumSize = new Vector2(120, 40) };
        cancelBtn.Pressed += () => { EmitSignal(SignalName.Cancelled); QueueFree(); };
        btnRow.AddChild(cancelBtn);

        var confirmBtn = new Button { Text = "确认", CustomMinimumSize = new Vector2(120, 40) };
        confirmBtn.Modulate = new Color(0.7f, 1f, 0.7f);
        confirmBtn.Pressed += OnConfirmPressed;
        btnRow.AddChild(confirmBtn);
    }

    public void Populate(string title, System.Collections.Generic.IReadOnlyList<RuntimeCard> handCards,
        ICardDatabase db, int minPick = 0, int maxPick = int.MaxValue, InstanceId? excludeInstance = null)
    {
        BuildUi();
        _title.Text = title;
        _minPick = minPick;
        _maxPick = maxPick;
        RefreshCount();

        for (int i = _grid.GetChildCount() - 1; i >= 0; i--)
        {
            var c = _grid.GetChild(i);
            _grid.RemoveChild(c);
            c.QueueFree();
        }
        _cardBoxes.Clear();
        _cardWraps.Clear();

        foreach (var card in handCards)
        {
            if (excludeInstance.HasValue && card.Instance == excludeInstance.Value) continue;
            int iid = card.Instance.Value;

            var wrap = new PanelContainer();
            var box = new StyleBoxFlat
            {
                BgColor = new Color(0, 0, 0, 0),
                BorderColor = new Color(0.6f, 0.5f, 0.8f, 0.5f),
                BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
                CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
                CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            };
            wrap.AddThemeStyleboxOverride("panel", box);
            _grid.AddChild(wrap);

            var cv = new CardView();
            wrap.AddChild(cv);
            cv.Bind(card, db.Get(card.Card), onField: false);
            cv.Clicked += _ => ToggleSelect(iid);
            _cardBoxes[iid] = box;
            _cardWraps[iid] = wrap;
        }
    }

    private void ToggleSelect(int iid)
    {
        if (_selected.Contains(iid))
        {
            _selected.Remove(iid);
        }
        else
        {
            if (_selected.Count >= _maxPick) return;
            _selected.Add(iid);
        }
        if (_cardBoxes.TryGetValue(iid, out var sb))
        {
            bool on = _selected.Contains(iid);
            sb.BorderColor = on ? new Color(0.5f, 1f, 0.5f) : new Color(0.6f, 0.5f, 0.8f, 0.5f);
            sb.BorderWidthTop = on ? 4 : 2;
            sb.BorderWidthBottom = on ? 4 : 2;
            sb.BorderWidthLeft = on ? 4 : 2;
            sb.BorderWidthRight = on ? 4 : 2;
        }
        RefreshCount();
    }

    private void RefreshCount()
    {
        _countLabel.Text = $"已选：{_selected.Count}"
            + (_maxPick < int.MaxValue ? $" / {_maxPick}" : "")
            + (_minPick > 0 ? $"（至少 {_minPick}）" : "");
    }

    private void OnConfirmPressed()
    {
        if (_selected.Count < _minPick) return;
        var arr = new int[_selected.Count];
        int i = 0;
        foreach (var v in _selected) arr[i++] = v;
        EmitSignal(SignalName.Confirmed, arr);
        QueueFree();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if ((@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right })
            || (@event is InputEventKey { Pressed: true, Keycode: Key.Escape }))
        {
            EmitSignal(SignalName.Cancelled);
            QueueFree();
            GetViewport().SetInputAsHandled();
        }
    }
}
