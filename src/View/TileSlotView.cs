using Godot;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.View;

/// <summary>
/// One of 6 battlefield slots per side. Holds at most one CardView. Emits clicks (empty or occupied).
/// </summary>
public partial class TileSlotView : PanelContainer
{
    public const int Width = CardView.CardWidth + 16;
    public const int Height = CardView.CardHeight + 16;

    [Signal] public delegate void TileClickedEventHandler(int tileIndex);

    public int TileIndex { get; set; }
    public PlayerSide Side { get; set; }

    public CardView? Occupant { get; private set; }
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
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.14f, 0.5f),
            BorderColor = new Color(0.4f, 0.4f, 0.45f),
            BorderWidthTop = 1, BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
        };
        AddThemeStyleboxOverride("panel", sb);
    }

    public void SetOccupant(CardView? cv)
    {
        if (Occupant is not null)
        {
            RemoveChild(Occupant);
            Occupant.QueueFree();
        }
        Occupant = cv;
        if (cv is not null) AddChild(cv);
    }

    public void Highlight(bool on)
    {
        Modulate = on ? new Color(1.5f, 1.5f, 0.7f) : new Color(1, 1, 1);
    }

    private void OnGuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            EmitSignal(SignalName.TileClicked, TileIndex);
    }
}
