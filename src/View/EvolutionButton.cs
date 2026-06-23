using Godot;
using ShadowCardSmash.Domain;

namespace ShadowCardSmash.View;

/// <summary>
/// Tile-sized button used to spend an Evolution Point. Visualizes EP as a horizontal segmented bar:
/// remaining segments lit yellow, spent ones dark. A label overlays the bar with status text.
/// Sits to the left of the 6 field tiles, mirroring the terrain slot on the right.
/// </summary>
public partial class EvolutionButton : PanelContainer
{
    public const int Width = TileSlotView.Width;
    public const int Height = TileSlotView.Height;

    [Signal] public delegate void EvolveButtonClickedEventHandler(int sideIndex);

    public PlayerSide Side { get; set; }
    public int CurrentEP { get; private set; }
    public int MaxEP { get; private set; }
    public bool IsLocked { get; private set; } = true;
    public bool UsedThisTurn { get; private set; }
    public bool IsActive { get; private set; } // true when controller is in AwaitEvolveTarget mode

    public void SetActive(bool active)
    {
        IsActive = active;
        if (_builtUi) UpdateAppearance();
    }

    private Control _stack = null!;
    private VBoxContainer _bar = null!;
    private Label _label = null!;
    private StyleBoxFlat _sb = null!;
    private bool _builtUi;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(Width, Height);
        BuildUi();
        GuiInput += OnGuiInput;
    }

    private void BuildUi()
    {
        if (_builtUi) return;
        _builtUi = true;

        _sb = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.10f, 0.18f),
            BorderColor = new Color(0.45f, 0.4f, 0.55f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 4, ContentMarginBottom = 4,
        };
        AddThemeStyleboxOverride("panel", _sb);

        _stack = new Control { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_stack);

        _bar = new VBoxContainer
        {
            AnchorRight = 1, AnchorBottom = 1,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _bar.AddThemeConstantOverride("separation", 2);
        _stack.AddChild(_bar);

        _label = new Label
        {
            AnchorRight = 1, AnchorBottom = 1,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Text = "进化",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _label.AddThemeFontSizeOverride("font_size", 20);
        _label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0));
        _label.AddThemeConstantOverride("outline_size", 4);
        _stack.AddChild(_label);
    }

    public void Bind(int currentEP, bool everUnlocked, bool usedThisTurn)
    {
        BuildUi();
        // We don't track MaxEP per-player; once unlocked, the bar shows 3 segments.
        MaxEP = everUnlocked ? Engine.EvolutionSystem.EvolutionPointsAtUnlock : 0;
        CurrentEP = currentEP;
        IsLocked = !everUnlocked;
        UsedThisTurn = usedThisTurn;
        RebuildBar();
        UpdateAppearance();
    }

    private void RebuildBar()
    {
        for (int i = _bar.GetChildCount() - 1; i >= 0; i--)
        {
            var c = _bar.GetChild(i);
            _bar.RemoveChild(c);
            c.QueueFree();
        }

        if (MaxEP <= 0)
        {
            // Locked — single dim segment.
            var empty = new ColorRect
            {
                Color = new Color(0.08f, 0.08f, 0.10f),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _bar.AddChild(empty);
            return;
        }

        for (int i = 0; i < MaxEP; i++)
        {
            var seg = new ColorRect
            {
                Color = i < CurrentEP ? new Color(1f, 0.85f, 0.2f) : new Color(0.18f, 0.15f, 0.20f),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _bar.AddChild(seg);
        }
    }

    private void UpdateAppearance()
    {
        if (IsLocked) _label.Text = "EP\n锁定";
        else if (UsedThisTurn) _label.Text = $"已用\n{CurrentEP}/{MaxEP}";
        else _label.Text = $"进化\n{CurrentEP}/{MaxEP}";

        _sb.BorderColor = IsActive
            ? new Color(1f, 0.85f, 0.2f)
            : IsLocked || CurrentEP <= 0 || UsedThisTurn
                ? new Color(0.45f, 0.4f, 0.55f)
                : new Color(0.95f, 0.7f, 0.3f);
        _sb.BorderWidthTop = IsActive ? 4 : 2;
        _sb.BorderWidthBottom = IsActive ? 4 : 2;
        _sb.BorderWidthLeft = IsActive ? 4 : 2;
        _sb.BorderWidthRight = IsActive ? 4 : 2;

        // Dim when there's nothing to spend.
        Modulate = (IsLocked || CurrentEP <= 0 || UsedThisTurn) ? new Color(0.7f, 0.7f, 0.7f) : new Color(1, 1, 1);
    }

    private void OnGuiInput(InputEvent e)
    {
        if (IsLocked || CurrentEP <= 0 || UsedThisTurn) return;
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            EmitSignal(SignalName.EvolveButtonClicked, (int)Side);
    }
}
