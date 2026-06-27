using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;
using ShadowCardSmash.Engine.Serialization;
using Xunit;

namespace ShadowCardSmash.Tests.Engine;

public class ActionEventJsonTests
{
    // ---------- Actions ----------

    [Fact]
    public void PlayCardAction_RoundTrip()
    {
        var action = new PlayCardAction(
            Issuer: PlayerSide.Second,
            HandInstance: new InstanceId(42),
            TileIndex: 3,
            TargetMinion: new InstanceId(99),
            TargetPlayer: null,
            ExtraTargets: new[] { new InstanceId(7), new InstanceId(8) },
            ChoiceIndices: new[] { 0, 1 });

        AssertActionRoundTrip(action, "PlayCard");
        var clone = (PlayCardAction)GameStateJson.DeserializeAction(GameStateJson.SerializeAction(action));
        Assert.Equal(action.HandInstance, clone.HandInstance);
        Assert.Equal(action.TileIndex, clone.TileIndex);
        Assert.Equal(action.TargetMinion, clone.TargetMinion);
        Assert.Null(clone.TargetPlayer);
        Assert.Equal(action.ExtraTargets, clone.ExtraTargets);
        Assert.Equal(action.ChoiceIndices, clone.ChoiceIndices);
    }

    [Fact]
    public void AttackAction_RoundTrip_PlayerTarget()
    {
        var action = new AttackAction(
            Issuer: PlayerSide.First,
            Attacker: new InstanceId(11),
            TargetMinion: null,
            TargetPlayer: PlayerSide.Second);

        AssertActionRoundTrip(action, "Attack");
    }

    [Fact]
    public void AttackAction_RoundTrip_MinionTarget()
    {
        var action = new AttackAction(
            Issuer: PlayerSide.First,
            Attacker: new InstanceId(11),
            TargetMinion: new InstanceId(22),
            TargetPlayer: null);

        AssertActionRoundTrip(action, "Attack");
        var clone = (AttackAction)GameStateJson.DeserializeAction(GameStateJson.SerializeAction(action));
        Assert.Equal(new InstanceId(22), clone.TargetMinion);
        Assert.Null(clone.TargetPlayer);
    }

    [Fact]
    public void EvolveAction_RoundTrip()
    {
        var action = new EvolveAction(PlayerSide.Second, new InstanceId(55));
        AssertActionRoundTrip(action, "Evolve");
    }

    [Fact]
    public void EndTurnAction_RoundTrip()
    {
        var action = new EndTurnAction(PlayerSide.First);
        AssertActionRoundTrip(action, "EndTurn");
    }

    [Fact]
    public void MulliganAction_RoundTrip_WithSwaps()
    {
        var action = new MulliganAction(PlayerSide.Second, new[] { 0, 2, 4 });
        AssertActionRoundTrip(action, "Mulligan");
        var clone = (MulliganAction)GameStateJson.DeserializeAction(GameStateJson.SerializeAction(action));
        Assert.Equal(new[] { 0, 2, 4 }, clone.SwapIndices);
    }

    [Fact]
    public void MulliganAction_RoundTrip_EmptySwaps()
    {
        var action = new MulliganAction(PlayerSide.First, System.Array.Empty<int>());
        AssertActionRoundTrip(action, "Mulligan");
        var clone = (MulliganAction)GameStateJson.DeserializeAction(GameStateJson.SerializeAction(action));
        Assert.Empty(clone.SwapIndices);
    }

    private static void AssertActionRoundTrip(IGameAction action, string expectedDiscriminator)
    {
        var json1 = GameStateJson.SerializeAction(action);
        Assert.Contains($"\"$type\":\"{expectedDiscriminator}\"", json1);

        var clone = GameStateJson.DeserializeAction(json1);
        Assert.Equal(action.GetType(), clone.GetType());
        Assert.Equal(action.Issuer, clone.Issuer);

        // Idempotency: serialize clone, must match original JSON byte-for-byte.
        Assert.Equal(json1, GameStateJson.SerializeAction(clone));
    }

    // ---------- Events ----------

    [Fact]
    public void Event_EmptyParameterless_RoundTrip()
    {
        var evt = new GameStartedEvent { Sequence = 1, Depth = 0 };
        AssertEventRoundTrip(evt, "GameStarted");
    }

    [Fact]
    public void Event_WithPositionalEnum_RoundTrip()
    {
        var evt = new GameEndedEvent(GameResult.FirstWins) { Sequence = 99, Depth = 0 };
        AssertEventRoundTrip(evt, "GameEnded");
        var clone = (GameEndedEvent)GameStateJson.DeserializeEvent(GameStateJson.SerializeEvent(evt));
        Assert.Equal(GameResult.FirstWins, clone.Result);
        Assert.Equal(99, clone.Sequence);
    }

    [Fact]
    public void Event_WithInstanceIdAndCardId_RoundTrip()
    {
        var evt = new CardDrawnEvent(PlayerSide.First, new InstanceId(15), new CardId(1001)) { Sequence = 5, Depth = 1 };
        AssertEventRoundTrip(evt, "CardDrawn");
        var clone = (CardDrawnEvent)GameStateJson.DeserializeEvent(GameStateJson.SerializeEvent(evt));
        Assert.Equal(new InstanceId(15), clone.Instance);
        Assert.Equal(new CardId(1001), clone.Card);
        Assert.Equal(1, clone.Depth);
    }

    [Fact]
    public void Event_WithNullableInstanceId_NullValue_RoundTrip()
    {
        var evt = new MinionDamagedEvent(new InstanceId(33), 5, Source: null) { Sequence = 7 };
        AssertEventRoundTrip(evt, "MinionDamaged");
        var clone = (MinionDamagedEvent)GameStateJson.DeserializeEvent(GameStateJson.SerializeEvent(evt));
        Assert.Null(clone.Source);
    }

    [Fact]
    public void Event_WithNullableInstanceId_NonNullValue_RoundTrip()
    {
        var evt = new MinionDamagedEvent(new InstanceId(33), 5, new InstanceId(7));
        AssertEventRoundTrip(evt, "MinionDamaged");
        var clone = (MinionDamagedEvent)GameStateJson.DeserializeEvent(GameStateJson.SerializeEvent(evt));
        Assert.Equal(new InstanceId(7), clone.Source);
    }

    [Fact]
    public void Event_WithNullableInt_RoundTrip()
    {
        var evt = new MinionDestroyedEvent(new InstanceId(50), new CardId(2001), PlayerSide.Second, TileIndex: 4);
        AssertEventRoundTrip(evt, "MinionDestroyed");
        var clone = (MinionDestroyedEvent)GameStateJson.DeserializeEvent(GameStateJson.SerializeEvent(evt));
        Assert.Equal(4, clone.TileIndex);

        var evtNoTile = evt with { TileIndex = null };
        var jsonNull = GameStateJson.SerializeEvent(evtNoTile);
        var cloneNull = (MinionDestroyedEvent)GameStateJson.DeserializeEvent(jsonNull);
        Assert.Null(cloneNull.TileIndex);
    }

    [Fact]
    public void Event_WithIntArray_RoundTrip()
    {
        var evt = new MulliganConfirmedEvent(PlayerSide.Second, new[] { 1, 3 });
        AssertEventRoundTrip(evt, "MulliganConfirmed");
        var clone = (MulliganConfirmedEvent)GameStateJson.DeserializeEvent(GameStateJson.SerializeEvent(evt));
        Assert.Equal(new[] { 1, 3 }, clone.SwappedIndices);
    }

    [Fact]
    public void Event_WithStringPayload_RoundTrip()
    {
        var evt = new TileEffectAppliedEvent(PlayerSide.First, TileIndex: 2, EffectKey: "burn", Duration: 3);
        AssertEventRoundTrip(evt, "TileEffectApplied");
        var clone = (TileEffectAppliedEvent)GameStateJson.DeserializeEvent(GameStateJson.SerializeEvent(evt));
        Assert.Equal("burn", clone.EffectKey);
        Assert.Equal(3, clone.Duration);
    }

    [Fact]
    public void EventBatch_MixedTypes_RoundTrip()
    {
        BoardEvent[] events =
        {
            new GameStartedEvent { Sequence = 1 },
            new CardDrawnEvent(PlayerSide.First, new InstanceId(10), new CardId(100)) { Sequence = 2 },
            new MinionDamagedEvent(new InstanceId(33), 4, new InstanceId(10)) { Sequence = 3 },
            new MinionDestroyedEvent(new InstanceId(33), new CardId(200), PlayerSide.Second, 2) { Sequence = 4 },
            new TurnEndedEvent(PlayerSide.First, 1) { Sequence = 5 },
        };
        var json = GameStateJson.SerializeEventBatch(events);
        var clones = GameStateJson.DeserializeEventBatch(json);

        Assert.Equal(events.Length, clones.Length);
        for (int i = 0; i < events.Length; i++)
        {
            Assert.Equal(events[i].GetType(), clones[i].GetType());
            Assert.Equal(events[i].Sequence, clones[i].Sequence);
        }
        // Idempotency on batch.
        Assert.Equal(json, GameStateJson.SerializeEventBatch(clones));
    }

    [Fact]
    public void Event_DiscriminatorMismatch_DeserializesAsCorrectType()
    {
        // Even if we hand-construct a JSON for one event and pass it to DeserializeEvent (base type),
        // STJ must dispatch to the right concrete type via the discriminator.
        var played = new CardPlayedEvent(PlayerSide.Second, new InstanceId(1), new CardId(2));
        var json = GameStateJson.SerializeEvent(played);
        var back = GameStateJson.DeserializeEvent(json);
        Assert.IsType<CardPlayedEvent>(back);
    }

    private static void AssertEventRoundTrip(BoardEvent evt, string expectedDiscriminator)
    {
        var json1 = GameStateJson.SerializeEvent(evt);
        Assert.Contains($"\"$type\":\"{expectedDiscriminator}\"", json1);

        var clone = GameStateJson.DeserializeEvent(json1);
        Assert.Equal(evt.GetType(), clone.GetType());
        Assert.Equal(evt.Sequence, clone.Sequence);
        Assert.Equal(evt.Depth, clone.Depth);

        // Idempotency.
        Assert.Equal(json1, GameStateJson.SerializeEvent(clone));
    }
}
