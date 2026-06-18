namespace ShadowCardSmash.Domain;

public sealed class TileEffect
{
    public string EffectKey = string.Empty;
    public int Value;
    public int RemainingTurns;
    public InstanceId Source;

    public TileEffect Clone() => new()
    {
        EffectKey = EffectKey,
        Value = Value,
        RemainingTurns = RemainingTurns,
        Source = Source,
    };
}
