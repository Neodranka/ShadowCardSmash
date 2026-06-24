using ShadowCardSmash.Cards.Resources;

namespace ShadowCardSmash.App;

/// <summary>
/// Holds the two decks (and seed) selected from <c>DeckSelection.tscn</c> so that <c>Battle.tscn</c>'s
/// BattleController can pick them up after the scene change. Static state across scene transitions —
/// Godot has no built-in DI container.
/// </summary>
public static class BattleSetup
{
    public static DeckResource? Player1Deck;
    public static DeckResource? Player2Deck;
    public static int Seed;
}
