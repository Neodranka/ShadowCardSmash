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

    public PlayerSide Side { get; set; }
    public bool ShowFaces { get; set; } = true;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
    }

    public void Rebind(IReadOnlyList<RuntimeCard> hand, ICardDatabase db)
    {
        // Remove from tree immediately (QueueFree alone defers to end of frame, leaving stale slots).
        foreach (var child in GetChildren().ToArray())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        foreach (var card in hand)
        {
            var cv = new CardView();
            AddChild(cv);
            cv.Bind(card, db.Get(card.Card), onField: false);
            if (!ShowFaces) cv.Modulate = new Color(0.3f, 0.3f, 0.4f);
            cv.Clicked += OnCardClicked;
        }
    }

    private void OnCardClicked(int instanceId) => EmitSignal(SignalName.CardSelected, instanceId);
}
