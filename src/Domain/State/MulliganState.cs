namespace ShadowCardSmash.Domain;

public sealed class MulliganState
{
    public bool[] Confirmed = new bool[2];
    public List<int>[] ChosenSwapIndices =
    {
        new(),
        new(),
    };

    public MulliganState Clone()
    {
        var copy = new MulliganState
        {
            Confirmed = (bool[])Confirmed.Clone(),
            ChosenSwapIndices = new List<int>[]
            {
                new(ChosenSwapIndices[0]),
                new(ChosenSwapIndices[1]),
            },
        };
        return copy;
    }
}
