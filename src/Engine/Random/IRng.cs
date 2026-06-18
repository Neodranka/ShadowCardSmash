namespace ShadowCardSmash.Engine;

public interface IRng
{
    int Next(int minInclusive, int maxExclusive);
    void Shuffle<T>(IList<T> list);
}
