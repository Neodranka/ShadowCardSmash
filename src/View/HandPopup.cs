using Godot;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.View;

/// <summary>
/// Full-screen overlay showing the player's hand in <see cref="HandView.DisplayMode.Full"/> mode.
/// Opened when the player clicks the peek strip; closed by ESC, right-click, clicking the dim background,
/// or by selecting a card (the play flow takes over).
/// </summary>
public partial class HandPopup : Control
{
    [Signal] public delegate void CardSelectedEventHandler(int instanceId);
    [Signal] public delegate void CardHoveredEventHandler(int instanceId);
    [Signal] public delegate void CardHoverExitedEventHandler(int instanceId);
    [Signal] public delegate void CancelledEventHandler();

    private HandView _hand = null!;
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

        // Dim background absorbs background clicks and acts as the "click anywhere to close" target.
        var dim = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.55f),
            AnchorRight = 1, AnchorBottom = 1,
            MouseFilter = MouseFilterEnum.Stop,
        };
        dim.GuiInput += OnDimInput;
        AddChild(dim);

        // Centered container near the bottom of the screen — cards visually slide up from the peek strip.
        var center = new Control
        {
            AnchorLeft = 0, AnchorTop = 1, AnchorRight = 1, AnchorBottom = 1,
            OffsetLeft = 0, OffsetTop = -(CardView.CardHeight + 40), OffsetRight = 0, OffsetBottom = -20,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(center);

        _hand = new HandView
        {
            ShowFaces = true,
            Mode = HandView.DisplayMode.Full,
            AnchorRight = 1, AnchorBottom = 1,
        };
        _hand.CardSelected += iid => EmitSignal(SignalName.CardSelected, iid);
        _hand.CardHovered += iid => EmitSignal(SignalName.CardHovered, iid);
        _hand.CardHoverExited += iid => EmitSignal(SignalName.CardHoverExited, iid);
        center.AddChild(_hand);
    }

    public void Populate(IReadOnlyList<RuntimeCard> cards, ICardDatabase db, int viewerMana, PlayerSide side)
    {
        BuildUi();
        _hand.Side = side;
        _hand.Rebind(cards, db, viewerMana);
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

    private void OnDimInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left or MouseButton.Right })
            Cancel();
    }

    private void Cancel()
    {
        EmitSignal(SignalName.Cancelled);
        QueueFree();
    }
}
