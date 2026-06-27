using System;
using System.Text;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;
using ShadowCardSmash.Net;
using ShadowCardSmash.Net.Serialization;
using Xunit;

namespace ShadowCardSmash.Tests.Engine;

public class NetMessageJsonTests
{
    [Fact]
    public void HandshakeRequest_RoundTrip()
    {
        var msg = new HandshakeRequest(NetSessionConfig.ProtocolVersion, "Alice");
        AssertRoundTrip(msg, "HandshakeRequest");
        var clone = (HandshakeRequest)NetMessageJson.Deserialize(NetMessageJson.Serialize(msg));
        Assert.Equal(NetSessionConfig.ProtocolVersion, clone.ProtocolVersion);
        Assert.Equal("Alice", clone.ClientName);
    }

    [Fact]
    public void HandshakeAccepted_RoundTrip()
    {
        var token = Guid.NewGuid();
        var msg = new HandshakeAccepted(token, PlayerSide.Second);
        AssertRoundTrip(msg, "HandshakeAccepted");
        var clone = (HandshakeAccepted)NetMessageJson.Deserialize(NetMessageJson.Serialize(msg));
        Assert.Equal(token, clone.SessionToken);
        Assert.Equal(PlayerSide.Second, clone.AssignedSide);
    }

    [Fact]
    public void HandshakeRejected_RoundTrip()
    {
        var msg = new HandshakeRejected("Protocol mismatch (got 2, expected 1)");
        AssertRoundTrip(msg, "HandshakeRejected");
        var clone = (HandshakeRejected)NetMessageJson.Deserialize(NetMessageJson.Serialize(msg));
        Assert.Equal(msg.Reason, clone.Reason);
    }

    [Fact]
    public void Ping_RoundTrip()
    {
        var msg = new PingMessage(SenderTimeMs: 1234567890L);
        AssertRoundTrip(msg, "Ping");
        var clone = (PingMessage)NetMessageJson.Deserialize(NetMessageJson.Serialize(msg));
        Assert.Equal(1234567890L, clone.SenderTimeMs);
    }

    [Fact]
    public void BytesRoundTrip_UTF8()
    {
        var msg = new HandshakeRequest(NetSessionConfig.ProtocolVersion, "测试中文名");
        var bytes = NetMessageJson.SerializeToBytes(msg);
        var clone = (HandshakeRequest)NetMessageJson.DeserializeFromBytes(bytes);
        Assert.Equal("测试中文名", clone.ClientName);
    }

    [Fact]
    public void Discriminator_DispatchesCorrectType()
    {
        var accept = new HandshakeAccepted(Guid.Empty, PlayerSide.First);
        var json = NetMessageJson.Serialize(accept);
        Assert.Contains("\"$type\":\"HandshakeAccepted\"", json);
        var back = NetMessageJson.Deserialize(json);
        Assert.IsType<HandshakeAccepted>(back);
    }

    [Fact]
    public void ActionRequest_RoundTrip_WithEmbeddedPlayCard()
    {
        var inner = new PlayCardAction(
            PlayerSide.First, new InstanceId(7), TileIndex: 2,
            TargetMinion: null, TargetPlayer: null);
        var msg = new ActionRequest(ClientRequestId: 42, Action: inner);
        AssertRoundTrip(msg, "ActionRequest");

        var clone = (ActionRequest)NetMessageJson.Deserialize(NetMessageJson.Serialize(msg));
        Assert.Equal(42, clone.ClientRequestId);
        Assert.IsType<PlayCardAction>(clone.Action);
        var inner2 = (PlayCardAction)clone.Action;
        Assert.Equal(PlayerSide.First, inner2.Issuer);
        Assert.Equal(new InstanceId(7), inner2.HandInstance);
        Assert.Equal(2, inner2.TileIndex);
    }

    [Fact]
    public void ActionRequest_RoundTrip_WithEmbeddedEndTurn()
    {
        var msg = new ActionRequest(99, new EndTurnAction(PlayerSide.Second));
        AssertRoundTrip(msg, "ActionRequest");
        var clone = (ActionRequest)NetMessageJson.Deserialize(NetMessageJson.Serialize(msg));
        Assert.IsType<EndTurnAction>(clone.Action);
    }

    [Fact]
    public void ActionApplied_RoundTrip_PreservesEventsAndState()
    {
        var state = BuildSmallState();
        BoardEvent[] events =
        {
            new TurnEndedEvent(PlayerSide.First, 3) { Sequence = 10 },
            new TurnStartedEvent(PlayerSide.Second, 4) { Sequence = 11 },
            new CardDrawnEvent(PlayerSide.Second, new InstanceId(50), new CardId(2001)) { Sequence = 12 },
        };
        var msg = new ActionApplied(
            Sequence: 100,
            OriginatingRequestId: 7,
            Action: new EndTurnAction(PlayerSide.First),
            Events: events,
            StateAfter: state);

        AssertRoundTrip(msg, "ActionApplied");

        var clone = (ActionApplied)NetMessageJson.Deserialize(NetMessageJson.Serialize(msg));
        Assert.Equal(100, clone.Sequence);
        Assert.Equal(7, clone.OriginatingRequestId);
        Assert.IsType<EndTurnAction>(clone.Action);
        Assert.Equal(3, clone.Events.Length);
        Assert.IsType<TurnEndedEvent>(clone.Events[0]);
        Assert.IsType<TurnStartedEvent>(clone.Events[1]);
        Assert.IsType<CardDrawnEvent>(clone.Events[2]);
        Assert.Equal(10, clone.Events[0].Sequence);
        Assert.Equal(state.TurnNumber, clone.StateAfter.TurnNumber);
        Assert.Equal(state.CurrentPlayer, clone.StateAfter.CurrentPlayer);
        Assert.Equal(state.GetPlayer(PlayerSide.First).Health, clone.StateAfter.GetPlayer(PlayerSide.First).Health);
    }

    [Fact]
    public void ActionApplied_RoundTrip_NullOriginatingRequestId()
    {
        // Host-originated action has no client request id.
        var msg = new ActionApplied(
            Sequence: 1,
            OriginatingRequestId: null,
            Action: new EndTurnAction(PlayerSide.First),
            Events: System.Array.Empty<BoardEvent>(),
            StateAfter: new GameState());
        AssertRoundTrip(msg, "ActionApplied");

        var clone = (ActionApplied)NetMessageJson.Deserialize(NetMessageJson.Serialize(msg));
        Assert.Null(clone.OriginatingRequestId);
        Assert.Empty(clone.Events);
    }

    [Fact]
    public void ActionRejected_RoundTrip()
    {
        var msg = new ActionRejected(
            ClientRequestId: 42,
            Action: new EndTurnAction(PlayerSide.First),
            Reason: "Not your turn.");
        AssertRoundTrip(msg, "ActionRejected");

        var clone = (ActionRejected)NetMessageJson.Deserialize(NetMessageJson.Serialize(msg));
        Assert.Equal(42, clone.ClientRequestId);
        Assert.Equal("Not your turn.", clone.Reason);
        Assert.IsType<EndTurnAction>(clone.Action);
    }

    private static GameState BuildSmallState()
    {
        var s = new GameState
        {
            TurnNumber = 4,
            CurrentPlayer = PlayerSide.Second,
            Phase = GamePhase.Main,
        };
        s.GetPlayer(PlayerSide.First).Health = 33;
        s.GetPlayer(PlayerSide.Second).Mana = 4;
        s.GetPlayer(PlayerSide.Second).Hand.Add(new RuntimeCard
        {
            Instance = s.AllocateInstanceId(),
            Card = new CardId(1001),
            Owner = PlayerSide.Second,
            Zone = Zone.Hand,
        });
        return s;
    }

    private static void AssertRoundTrip(NetMessage msg, string expectedDiscriminator)
    {
        var json1 = NetMessageJson.Serialize(msg);
        Assert.Contains($"\"$type\":\"{expectedDiscriminator}\"", json1);
        var clone = NetMessageJson.Deserialize(json1);
        Assert.Equal(msg.GetType(), clone.GetType());
        Assert.Equal(json1, NetMessageJson.Serialize(clone));
    }
}
