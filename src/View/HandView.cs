using Godot;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.View;

/// <summary>
/// Horizontal fan of the player's hand. Each CardView re-emits Clicked upward as CardSelected.
/// </summary>
public partial class HandView : HBoxContainer
{
    [Signal] public delegate void CardSelectedEventHandler(int instanceId);
    [Signal] public delegate void CardHoveredEventHandler(int instanceId);
    [Signal] public delegate void CardHoverExitedEventHandler(int instanceId);

    public PlayerSide Side { get; set; }
    public bool ShowFaces { get; set; } = true;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
    }

    public void Rebind(IReadOnlyList<RuntimeCard> hand, ICardDatabase db, int viewerMana = 0)
    {
        for (int i = GetChildCount() - 1; i >= 0; i--)
        {
            var child = GetChild(i);
            RemoveChild(child);
            child.QueueFree();
        }
        foreach (var card in hand)
        {
            var cv = new CardView();
            AddChild(cv);
            if (ShowFaces)
            {
                cv.Bind(card, db.Get(card.Card), onField: false, viewerMana: viewerMana);
                cv.Clicked += OnCardClicked;
                cv.HoverEntered += iid => EmitSignal(SignalName.CardHovered, iid);
                cv.HoverExited += iid => EmitSignal(SignalName.CardHoverExited, iid);
            }
            else
            {
                cv.BindFaceDown();
            }
        }
    }

    private void OnCardClicked(int instanceId) => EmitSignal(SignalName.CardSelected, instanceId);
}
