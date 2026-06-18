namespace ShadowCardSmash.Domain;

public readonly record struct CardId(int Value)
{
    public static readonly CardId None = new(0);
    public override string ToString() => $"Card#{Value}";
}
