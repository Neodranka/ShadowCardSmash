namespace ShadowCardSmash.Engine;

/// <summary>
/// XorShift64* RNG. Pure function of (seed, counter). Same (seed, counter) → same outputs on every machine.
/// The GameLoop persists the counter inside GameState so that snapshots round-trip the RNG cursor.
/// </summary>
public sealed class DeterministicRng : IRng
{
    private ulong _state;

    public DeterministicRng(int seed, ulong counter)
    {
        _state = Mix((ulong)seed) ^ Mix(counter ^ 0x9E3779B97F4A7C15UL);
        if (_state == 0) _state = 0x9E3779B97F4A7C15UL;
    }

    public ulong Counter { get; private set; }

    public int Next(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive) return minInclusive;
        var span = (uint)(maxExclusive - minInclusive);
        return minInclusive + (int)(NextUInt() % span);
    }

    public void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Next(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private uint NextUInt()
    {
        Counter++;
        _state ^= _state >> 12;
        _state ^= _state << 25;
        _state ^= _state >> 27;
        var v = _state * 0x2545F4914F6CDD1DUL;
        return (uint)(v >> 32);
    }

    private static ulong Mix(ulong z)
    {
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
