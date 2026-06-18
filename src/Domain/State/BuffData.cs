namespace ShadowCardSmash.Domain;

public sealed class BuffData
{
    public int AttackDelta;
    public int HealthDelta;
    public int DurationTurns;
    public InstanceId Source;
    public string? Tag;

    public BuffData Clone() => new()
    {
        AttackDelta = AttackDelta,
        HealthDelta = HealthDelta,
        DurationTurns = DurationTurns,
        Source = Source,
        Tag = Tag,
    };
}
