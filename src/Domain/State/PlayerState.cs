namespace ShadowCardSmash.Domain;

public sealed class PlayerState
{
    public const int FieldSize = 6;
    public const int HandLimit = 10;
    public const int StartingHealth = 40;

    public PlayerSide Side;
    public HeroClass HeroClass;

    public int Health = StartingHealth;
    public int MaxHealth = StartingHealth;

    public int Mana;
    public int MaxMana;

    public int EvolutionPoints;
    public bool HasEvolvedThisTurn;

    public int FatigueCounter;

    public List<RuntimeCard> Deck = new();
    public List<RuntimeCard> Hand = new();
    public TileState[] Field = CreateEmptyField();
    public List<RuntimeCard> Graveyard = new();
    public List<RuntimeCard> Vanished = new();

    public CardId CompensationCard = CardId.None;

    public bool MinionDestroyedThisTurn;
    public int SelfDamageThisTurn;
    public int TotalSelfDamage;
    public int SelfDamageCount;

    private static TileState[] CreateEmptyField()
    {
        var f = new TileState[FieldSize];
        for (int i = 0; i < FieldSize; i++) f[i] = new TileState { Index = i };
        return f;
    }

    public int EmptyTileCount()
    {
        int n = 0;
        for (int i = 0; i < Field.Length; i++) if (Field[i].IsEmpty) n++;
        return n;
    }

    public bool TryGetEmptyTileIndex(out int index)
    {
        for (int i = 0; i < Field.Length; i++)
        {
            if (Field[i].IsEmpty) { index = i; return true; }
        }
        index = -1;
        return false;
    }

    public RuntimeCard? FindOnField(InstanceId id)
    {
        for (int i = 0; i < Field.Length; i++)
        {
            var occ = Field[i].Occupant;
            if (occ is not null && occ.Instance == id) return occ;
        }
        return null;
    }

    public PlayerState Clone()
    {
        var copy = new PlayerState
        {
            Side = Side,
            HeroClass = HeroClass,
            Health = Health,
            MaxHealth = MaxHealth,
            Mana = Mana,
            MaxMana = MaxMana,
            EvolutionPoints = EvolutionPoints,
            HasEvolvedThisTurn = HasEvolvedThisTurn,
            FatigueCounter = FatigueCounter,
            CompensationCard = CompensationCard,
            MinionDestroyedThisTurn = MinionDestroyedThisTurn,
            SelfDamageThisTurn = SelfDamageThisTurn,
            TotalSelfDamage = TotalSelfDamage,
            SelfDamageCount = SelfDamageCount,
        };
        foreach (var c in Deck) copy.Deck.Add(c.Clone());
        foreach (var c in Hand) copy.Hand.Add(c.Clone());
        for (int i = 0; i < Field.Length; i++) copy.Field[i] = Field[i].Clone();
        foreach (var c in Graveyard) copy.Graveyard.Add(c.Clone());
        foreach (var c in Vanished) copy.Vanished.Add(c.Clone());
        return copy;
    }
}
