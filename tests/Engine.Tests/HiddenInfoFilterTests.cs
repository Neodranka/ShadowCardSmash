using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;
using Xunit;

namespace ShadowCardSmash.Tests.Engine;

public class HiddenInfoFilterTests
{
    [Fact]
    public void FilterFor_MasksOpponentHandCardIds_PreservesInstanceAndCount()
    {
        var state = BuildState();
        var p2Original = state.GetPlayer(PlayerSide.Second);
        Assert.Equal(2, p2Original.Hand.Count);
        var origInstanceA = p2Original.Hand[0].Instance;
        var origInstanceB = p2Original.Hand[1].Instance;

        // Filter for First (so Second's hand is the "opponent's hand" from First's view).
        var filtered = state.FilterFor(PlayerSide.First);
        var p2Filtered = filtered.GetPlayer(PlayerSide.Second);

        Assert.Equal(2, p2Filtered.Hand.Count);
        Assert.Equal(CardId.Hidden, p2Filtered.Hand[0].Card);
        Assert.Equal(CardId.Hidden, p2Filtered.Hand[1].Card);
        // InstanceIds preserved so UI can still track per-card animations.
        Assert.Equal(origInstanceA, p2Filtered.Hand[0].Instance);
        Assert.Equal(origInstanceB, p2Filtered.Hand[1].Instance);
        Assert.Equal(Zone.Hand, p2Filtered.Hand[0].Zone);
    }

    [Fact]
    public void FilterFor_LeavesOwnHandUntouched()
    {
        var state = BuildState();
        var origCardId = state.GetPlayer(PlayerSide.First).Hand[0].Card;

        var filtered = state.FilterFor(PlayerSide.First);
        var p1Filtered = filtered.GetPlayer(PlayerSide.First);

        Assert.Equal(origCardId, p1Filtered.Hand[0].Card);
        Assert.NotEqual(CardId.Hidden, p1Filtered.Hand[0].Card);
    }

    [Fact]
    public void FilterFor_MasksOpponentDeck_NotOwn()
    {
        var state = BuildState();
        var filtered = state.FilterFor(PlayerSide.First);

        Assert.All(filtered.GetPlayer(PlayerSide.Second).Deck, c => Assert.Equal(CardId.Hidden, c.Card));
        Assert.All(filtered.GetPlayer(PlayerSide.First).Deck, c => Assert.NotEqual(CardId.Hidden, c.Card));
    }

    [Fact]
    public void FilterFor_LeavesPublicZonesUntouched()
    {
        var state = BuildState();
        // Put a card in Second's graveyard and field.
        var p2 = state.GetPlayer(PlayerSide.Second);
        var graveCard = new RuntimeCard { Instance = state.AllocateInstanceId(), Card = new CardId(7777),
            Owner = PlayerSide.Second, Zone = Zone.Graveyard };
        p2.Graveyard.Add(graveCard);
        p2.Field[3].Occupant = new RuntimeCard { Instance = state.AllocateInstanceId(), Card = new CardId(8888),
            Owner = PlayerSide.Second, Zone = Zone.Field };

        var filtered = state.FilterFor(PlayerSide.First);
        Assert.Equal(new CardId(7777), filtered.GetPlayer(PlayerSide.Second).Graveyard[0].Card);
        Assert.Equal(new CardId(8888), filtered.GetPlayer(PlayerSide.Second).Field[3].Occupant!.Card);
    }

    [Fact]
    public void FilterFor_IsDeepCopy_OriginalUnchanged()
    {
        var state = BuildState();
        var origCardId = state.GetPlayer(PlayerSide.Second).Hand[0].Card;

        var filtered = state.FilterFor(PlayerSide.First);
        // Mutate filtered, original must not move.
        filtered.GetPlayer(PlayerSide.Second).Hand[0] = new RuntimeCard
            { Instance = filtered.GetPlayer(PlayerSide.Second).Hand[0].Instance, Card = new CardId(99999), Owner = PlayerSide.Second, Zone = Zone.Hand };

        Assert.Equal(origCardId, state.GetPlayer(PlayerSide.Second).Hand[0].Card);
    }

    [Fact]
    public void EventFilter_MasksCardDrawnEvent_ForOpponent()
    {
        var evt = new CardDrawnEvent(PlayerSide.Second, new InstanceId(42), new CardId(3001)) { Sequence = 5 };

        var filteredForFirst = EventFilter.FilterFor(evt, PlayerSide.First);
        var filteredCast = Assert.IsType<CardDrawnEvent>(filteredForFirst);
        Assert.Equal(CardId.Hidden, filteredCast.Card);
        Assert.Equal(new InstanceId(42), filteredCast.Instance);
        Assert.Equal(5, filteredCast.Sequence);
    }

    [Fact]
    public void EventFilter_LeavesCardDrawnEvent_ForOwnSide()
    {
        var evt = new CardDrawnEvent(PlayerSide.Second, new InstanceId(42), new CardId(3001));

        var filteredForSecond = EventFilter.FilterFor(evt, PlayerSide.Second);
        Assert.Equal(new CardId(3001), ((CardDrawnEvent)filteredForSecond).Card);
    }

    [Fact]
    public void EventFilter_PassesThroughPublicEvents()
    {
        var played = new CardPlayedEvent(PlayerSide.Second, new InstanceId(42), new CardId(3001));
        var summoned = new MinionSummonedEvent(PlayerSide.Second, new InstanceId(42), new CardId(3001), TileIndex: 2);
        var damaged = new MinionDamagedEvent(new InstanceId(42), 3, null);

        Assert.Same(played, EventFilter.FilterFor(played, PlayerSide.First));
        Assert.Same(summoned, EventFilter.FilterFor(summoned, PlayerSide.First));
        Assert.Same(damaged, EventFilter.FilterFor(damaged, PlayerSide.First));
    }

    [Fact]
    public void EventFilter_FilterAll_BatchPreservesOrder()
    {
        BoardEvent[] events =
        {
            new CardDrawnEvent(PlayerSide.Second, new InstanceId(1), new CardId(100)) { Sequence = 1 },
            new TurnEndedEvent(PlayerSide.First, 3) { Sequence = 2 },
            new CardDrawnEvent(PlayerSide.First, new InstanceId(2), new CardId(200)) { Sequence = 3 },
        };
        var filtered = EventFilter.FilterAll(events, PlayerSide.First);
        Assert.Equal(3, filtered.Length);
        Assert.Equal(CardId.Hidden, ((CardDrawnEvent)filtered[0]).Card); // opponent's draw masked
        Assert.IsType<TurnEndedEvent>(filtered[1]);
        Assert.Equal(new CardId(200), ((CardDrawnEvent)filtered[2]).Card); // own draw kept
    }

    private static GameState BuildState()
    {
        var s = new GameState { TurnNumber = 1, CurrentPlayer = PlayerSide.First, Phase = GamePhase.Main };
        var p1 = s.GetPlayer(PlayerSide.First);
        var p2 = s.GetPlayer(PlayerSide.Second);
        for (int i = 0; i < 2; i++)
        {
            p1.Hand.Add(new RuntimeCard { Instance = s.AllocateInstanceId(), Card = new CardId(1000 + i),
                Owner = PlayerSide.First, Zone = Zone.Hand });
            p2.Hand.Add(new RuntimeCard { Instance = s.AllocateInstanceId(), Card = new CardId(2000 + i),
                Owner = PlayerSide.Second, Zone = Zone.Hand });
        }
        for (int i = 0; i < 5; i++)
        {
            p1.Deck.Add(new RuntimeCard { Instance = s.AllocateInstanceId(), Card = new CardId(3000 + i),
                Owner = PlayerSide.First, Zone = Zone.Deck });
            p2.Deck.Add(new RuntimeCard { Instance = s.AllocateInstanceId(), Card = new CardId(4000 + i),
                Owner = PlayerSide.Second, Zone = Zone.Deck });
        }
        return s;
    }
}
