using Godot;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.View;

/// <summary>
/// Modal "choose N from M" popup. Each choice renders as a panel with title + description.
/// User clicks options to toggle them; when exactly N are selected the Confirm button enables.
/// Emits <see cref="ChoiceConfirmedEventHandler"/> with the picked indices (in click order).
///
/// Designed for N=1, M=2 today (布伦哈尔家徽) but works for any (N, M) configuration.
/// </summary>
public partial class ChoicePopup : Control
{
    [Signal] public delegate void ChoiceConfirmedEventHandler(int[] picked);
    [Signal] public delegate void CancelledEventHandler();

    private readonly List<int> _picked = new();
    private int _pickCount = 1;
    private IReadOnlyList<CardChoice> _options = System.Array.Empty<CardChoice>();
    private VBoxContainer _optionsBox = null!;
    private Label _title = null!;
    private Label _counter = null!;
    private Button _confirmBtn = null!;
    private readonly List<Button> _optionButtons = new();
    private bool _builtUi;

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

        // Dim background that absorbs clicks.
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
            OffsetLeft = -260, OffsetTop = -260, OffsetRight = 260, OffsetBottom = 260,
        };
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.08f, 0.16f, 0.98f),
            BorderColor = new Color(0.9f, 0.75f, 0.3f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 16, ContentMarginBottom = 16,
        };
        panel.AddThemeStyleboxOverride("panel", sb);
        AddChild(panel);

        var vb = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        vb.AddThemeConstantOverride("separation", 10);
        panel.AddChild(vb);

        _title = new Label { HorizontalAlignment = HorizontalAlignment.Center, Text = "" };
        _title.AddThemeFontSizeOverride("font_size", 22);
        _title.Modulate = new Color(1f, 0.95f, 0.7f);
        vb.AddChild(_title);

        _counter = new Label { HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(0.75f, 0.75f, 0.85f) };
        _counter.AddThemeFontSizeOverride("font_size", 13);
        vb.AddChild(_counter);

        vb.AddChild(new HSeparator());

        _optionsBox = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _optionsBox.AddThemeConstantOverride("separation", 8);
        vb.AddChild(_optionsBox);

        vb.AddChild(new HSeparator());

        var btnRow = new HBoxContainer();
        btnRow.AddThemeConstantOverride("separation", 12);
        vb.AddChild(btnRow);

        var cancelBtn = new Button { Text = "取消", CustomMinimumSize = new Vector2(120, 36) };
        cancelBtn.Pressed += Cancel;
        btnRow.AddChild(cancelBtn);

        btnRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        _confirmBtn = new Button { Text = "确认", CustomMinimumSize = new Vector2(120, 36) };
        _confirmBtn.Pressed += Confirm;
        btnRow.AddChild(_confirmBtn);
    }

    public void Setup(string title, IReadOnlyList<CardChoice> options, int pickCount)
    {
        BuildUi();
        _pickCount = Math.Max(1, pickCount);
        _options = options;
        _picked.Clear();
        _title.Text = title;

        for (int i = _optionsBox.GetChildCount() - 1; i >= 0; i--)
        {
            var c = _optionsBox.GetChild(i);
            _optionsBox.RemoveChild(c);
            c.QueueFree();
        }
        _optionButtons.Clear();

        for (int i = 0; i < options.Count; i++)
        {
            int captured = i;
            var opt = options[i];
            var btn = new Button
            {
                Text = string.IsNullOrEmpty(opt.Description) ? opt.Title : $"{opt.Title}\n{opt.Description}",
                CustomMinimumSize = new Vector2(0, 64),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ToggleMode = true,
                ClipText = false,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            btn.Toggled += (bool pressed) => OnOptionToggled(captured, pressed);
            _optionsBox.AddChild(btn);
            _optionButtons.Add(btn);
        }

        UpdateState();
    }

    private void OnOptionToggled(int index, bool pressed)
    {
        if (pressed)
        {
            if (_picked.Contains(index)) return;
            // Auto-deselect the oldest if pickCount=1 (radio style).
            if (_pickCount == 1 && _picked.Count >= 1)
            {
                int old = _picked[0];
                _picked.Clear();
                if (old != index && old < _optionButtons.Count) _optionButtons[old].ButtonPressed = false;
            }
            // Cap at pickCount (push out the earliest).
            while (_picked.Count >= _pickCount)
            {
                int evicted = _picked[0];
                _picked.RemoveAt(0);
                if (evicted < _optionButtons.Count) _optionButtons[evicted].ButtonPressed = false;
            }
            _picked.Add(index);
        }
        else
        {
            _picked.Remove(index);
        }
        UpdateState();
    }

    private void UpdateState()
    {
        _counter.Text = $"已选 {_picked.Count}/{_pickCount}";
        _confirmBtn.Disabled = _picked.Count != _pickCount;
    }

    private void Confirm()
    {
        if (_picked.Count != _pickCount) return;
        EmitSignal(SignalName.ChoiceConfirmed, _picked.ToArray());
        QueueFree();
    }

    private void Cancel()
    {
        EmitSignal(SignalName.Cancelled);
        QueueFree();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right }
            || @event is InputEventKey { Pressed: true, Keycode: Key.Escape })
        {
            Cancel();
            GetViewport().SetInputAsHandled();
        }
    }
}
