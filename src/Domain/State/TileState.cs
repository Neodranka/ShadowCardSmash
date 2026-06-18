namespace ShadowCardSmash.Domain;

public sealed class TileState
{
    public int Index;
    public RuntimeCard? Occupant;
    public List<TileEffect> Effects = new();

    public bool IsEmpty => Occupant is null;

    public TileState Clone()
    {
        var copy = new TileState
        {
            Index = Index,
            Occupant = Occupant?.Clone(),
        };
        foreach (var e in Effects) copy.Effects.Add(e.Clone());
        return copy;
    }
}
