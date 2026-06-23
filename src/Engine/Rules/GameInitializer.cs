using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// One-shot setup: shuffle decks, draw starting hands (4 / 5), enter Mulligan phase.
/// Second player keeps the compensation card slot; it is materialized at end-of-mulligan.
/// </summary>
public static class GameInitializer
{
    public const int FirstPlayerStartingHand = 4;
    public const int SecondPlayerStartingHand = 5;

    public readonly record struct SeatConfig(IReadOnlyList<CardId> DeckList, HeroClass HeroClass, CardId? CompensationCard);

    public static void Begin(GameLoop loop, int seed, SeatConfig first, SeatConfig second)
    {
        var state = loop.State;
        state.RandomSeed = seed;
        state.RandomCounter = 0;
        loop.GetType(); // keep loop reachable; Rng was bound in constructor.

        SeedPlayer(loop, PlayerSide.First, first);
        SeedPlayer(loop, PlayerSide.Second, second);

        // Shuffle decks deterministically via the loop's RNG.
        loop.Rng.Shuffle(state.GetPlayer(PlayerSide.First).Deck);
        loop.Rng.Shuffle(state.GetPlayer(PlayerSide.Second).Deck);

        // Initial draws as raw moves (no triggers — pre-game).
        DealInitial(state, PlayerSide.First, FirstPlayerStartingHand);
        DealInitial(state, PlayerSide.Second, SecondPlayerStartingHand);

        // EP starts locked (0) for both players — both gain 3 at the start of P2's 4th turn (see TurnManager).

        state.Phase = GamePhase.Mulligan;
        state.CurrentPlayer = PlayerSide.First;

        var ctx = new GameContext(state, loop.Rng, loop, loop.CardDatabase, PlayerSide.First);
        loop.Publish(new GameStartedEvent(), ctx);
        loop.Publish(new PhaseChangedEvent(GamePhase.Mulligan, PlayerSide.First), ctx);
    }

    private static void SeedPlayer(GameLoop loop, PlayerSide side, SeatConfig cfg)
    {
        var p = loop.State.GetPlayer(side);
        p.HeroClass = cfg.HeroClass;
        p.CompensationCard = cfg.CompensationCard ?? CardId.None;
        foreach (var id in cfg.DeckList)
        {
            p.Deck.Add(new RuntimeCard
            {
                Instance = loop.State.AllocateInstanceId(),
                Card = id,
                Owner = side,
                Zone = Zone.Deck,
            });
        }
    }

    private static void DealInitial(GameState state, PlayerSide side, int count)
    {
        var p = state.GetPlayer(side);
        for (int i = 0; i < count && p.Deck.Count > 0; i++)
        {
            var card = p.Deck[^1];
            p.Deck.RemoveAt(p.Deck.Count - 1);
            card.Zone = Zone.Hand;
            p.Hand.Add(card);
        }
    }
}
