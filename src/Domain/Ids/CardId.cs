namespace ShadowCardSmash.Domain;

public readonly record struct CardId(int Value)
{
    public static readonly CardId None = new(0);
    /// <summary>Sentinel for hidden information: opponent's hand/deck cards seen from client's view.
    /// Card scripts must never receive this; renderers should fall back to face-down.</summary>
    public static readonly CardId Hidden = new(-1);
    public override string ToString() => Value == -1 ? "Card#Hidden" : $"Card#{Value}";
}
