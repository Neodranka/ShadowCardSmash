namespace ShadowCardSmash.Domain;

public enum CardType
{
    Minion,
    Spell,
    Amulet,
    Terrain,
}

public enum Rarity
{
    Bronze,
    Silver,
    Gold,
    Legendary,
}

public enum HeroClass
{
    Neutral = 0,
    Forsaken = 1,
    Empire = 2,
    ClassC = 3,
    ClassD = 4,
    ClassE = 5,
    ClassF = 6,
    ClassG = 7,
}

public enum GamePhase
{
    NotStarted,
    Mulligan,
    TurnStart,
    Draw,
    Main,
    TurnEnd,
    GameOver,
}

[Flags]
public enum Keyword
{
    None = 0,
    Ward = 1 << 0,
    Rush = 1 << 1,
    Storm = 1 << 2,
    Barrier = 1 << 3,
    Stealth = 1 << 4,
}

public enum Zone
{
    Deck,
    Hand,
    Field,
    Graveyard,
    Vanished,
    Limbo,
}

public enum GameResult
{
    InProgress,
    FirstWins,
    SecondWins,
    Draw,
}
