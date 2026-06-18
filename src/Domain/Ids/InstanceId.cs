namespace ShadowCardSmash.Domain;

public readonly record struct InstanceId(int Value)
{
    public static readonly InstanceId None = new(0);
    public override string ToString() => $"Inst#{Value}";
}
