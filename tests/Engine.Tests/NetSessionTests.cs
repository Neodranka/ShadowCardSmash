using System.Collections.Generic;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;
using ShadowCardSmash.Net;
using ShadowCardSmash.Net.Session;
using ShadowCardSmash.Net.Transport;
using Xunit;

namespace ShadowCardSmash.Tests.Engine;

public class NetSessionTests
{
    [Fact]
    public void Hotseat_SubmitEndTurn_FiresActionAppliedLocally()
    {
        var (loop, _) = NewLoopInMainPhase();
        var session = NetSession.CreateHotseatLoopback(loop);

        var captured = new List<ActionApplied>();
        session.ActionApplied += captured.Add;

        session.SubmitLocalAction(new EndTurnAction(PlayerSide.First));

        Assert.Single(captured);
        var applied = captured[0];
        Assert.IsType<EndTurnAction>(applied.Action);
        Assert.Equal(1, applied.Sequence);
        Assert.Null(applied.OriginatingRequestId); // host-originated
        Assert.Contains(applied.Events, e => e is TurnEndedEvent);
        Assert.Contains(applied.Events, e => e is TurnStartedEvent);
        Assert.Equal(PlayerSide.Second, applied.StateAfter.CurrentPlayer);
    }

    [Fact]
    public void Hotseat_SubmitInvalidAction_FiresActionRejected()
    {
        var (loop, _) = NewLoopInMainPhase();
        var session = NetSession.CreateHotseatLoopback(loop);

        var captured = new List<ActionRejected>();
        var appliedCaptured = new List<ActionApplied>();
        session.ActionRejected += captured.Add;
        session.ActionApplied += appliedCaptured.Add;

        // Second player tries to end turn while First's turn — should reject.
        session.SubmitLocalAction(new EndTurnAction(PlayerSide.Second));

        Assert.Single(captured);
        Assert.Empty(appliedCaptured);
        Assert.NotNull(captured[0].Reason);
    }

    [Fact]
    public void NetHostClient_ClientSubmit_ReachesHostAndBroadcastsBack()
    {
        var (hostTransport, clientTransport) = InMemoryNetTransport.CreatePair();
        var (loop, state) = NewLoopInMainPhase();
        var hostSession = NetSession.CreateNetHost(loop, hostTransport, PlayerSide.First);
        var clientSession = NetSession.CreateNetClient(clientTransport, PlayerSide.Second, state.Snapshot());

        var hostApplied = new List<ActionApplied>();
        var clientApplied = new List<ActionApplied>();
        hostSession.ActionApplied += hostApplied.Add;
        clientSession.ActionApplied += clientApplied.Add;

        // Client (sitting on PlayerSide.Second) submits an EndTurn for the current player (First).
        // Note: validation still rejects if not your turn — but the wire flow is what we're testing.
        // Submit on behalf of First (the active player) by passing First Issuer; client just forwards.
        long reqId = clientSession.SubmitLocalAction(new EndTurnAction(PlayerSide.First));

        Assert.Equal(1, reqId);
        // Host applied locally.
        Assert.Single(hostApplied);
        Assert.Equal(reqId, hostApplied[0].OriginatingRequestId);
        // Client received the broadcast and updated mirror.
        Assert.Single(clientApplied);
        Assert.Equal(reqId, clientApplied[0].OriginatingRequestId);
        Assert.Equal(PlayerSide.Second, clientSession.State.CurrentPlayer);
        Assert.Equal(PlayerSide.Second, hostSession.State.CurrentPlayer);
    }

    [Fact]
    public void NetHostClient_HostSubmit_BroadcastsToClient()
    {
        var (hostTransport, clientTransport) = InMemoryNetTransport.CreatePair();
        var (loop, state) = NewLoopInMainPhase();
        var hostSession = NetSession.CreateNetHost(loop, hostTransport, PlayerSide.First);
        var clientSession = NetSession.CreateNetClient(clientTransport, PlayerSide.Second, state.Snapshot());

        var hostApplied = new List<ActionApplied>();
        var clientApplied = new List<ActionApplied>();
        hostSession.ActionApplied += hostApplied.Add;
        clientSession.ActionApplied += clientApplied.Add;

        hostSession.SubmitLocalAction(new EndTurnAction(PlayerSide.First));

        Assert.Single(hostApplied);
        Assert.Null(hostApplied[0].OriginatingRequestId); // host-originated, no client req id
        Assert.Single(clientApplied);
        Assert.Equal(hostApplied[0].Sequence, clientApplied[0].Sequence);
        Assert.Equal(PlayerSide.Second, clientSession.State.CurrentPlayer);
    }

    [Fact]
    public void NetHostClient_ClientSubmitInvalid_RejectedOverWire()
    {
        var (hostTransport, clientTransport) = InMemoryNetTransport.CreatePair();
        var (loop, state) = NewLoopInMainPhase();
        var hostSession = NetSession.CreateNetHost(loop, hostTransport, PlayerSide.First);
        var clientSession = NetSession.CreateNetClient(clientTransport, PlayerSide.Second, state.Snapshot());

        var clientRejected = new List<ActionRejected>();
        clientSession.ActionRejected += clientRejected.Add;

        // Issuer=Second while it's First's turn → rejected.
        long reqId = clientSession.SubmitLocalAction(new EndTurnAction(PlayerSide.Second));

        Assert.Single(clientRejected);
        Assert.Equal(reqId, clientRejected[0].ClientRequestId);
    }

    [Fact]
    public void NetClient_MirrorStateReflectsLatestActionApplied()
    {
        var (hostTransport, clientTransport) = InMemoryNetTransport.CreatePair();
        var (loop, state) = NewLoopInMainPhase();
        var hostSession = NetSession.CreateNetHost(loop, hostTransport, PlayerSide.First);
        var clientSession = NetSession.CreateNetClient(clientTransport, PlayerSide.Second, state.Snapshot());

        Assert.Equal(PlayerSide.First, clientSession.State.CurrentPlayer);

        hostSession.SubmitLocalAction(new EndTurnAction(PlayerSide.First));
        Assert.Equal(PlayerSide.Second, clientSession.State.CurrentPlayer);

        hostSession.SubmitLocalAction(new EndTurnAction(PlayerSide.Second));
        Assert.Equal(PlayerSide.First, clientSession.State.CurrentPlayer);
        Assert.Equal(3, clientSession.State.TurnNumber);
    }

    /// <summary>
    /// Build a GameLoop already in Main phase with empty fields, one card in each deck (so first turn
    /// draw doesn't fatigue), and a stub card-db (never queried because field hooks have no occupants).
    /// </summary>
    private static (GameLoop loop, GameState state) NewLoopInMainPhase()
    {
        var state = new GameState
        {
            TurnNumber = 1,
            CurrentPlayer = PlayerSide.First,
            Phase = GamePhase.Main,
        };
        // Buffer deck so end-turn draw doesn't crash with fatigue.
        for (int i = 0; i < 5; i++)
        {
            state.GetPlayer(PlayerSide.First).Deck.Add(new RuntimeCard
                { Instance = state.AllocateInstanceId(), Card = new CardId(1), Owner = PlayerSide.First, Zone = Zone.Deck });
            state.GetPlayer(PlayerSide.Second).Deck.Add(new RuntimeCard
                { Instance = state.AllocateInstanceId(), Card = new CardId(1), Owner = PlayerSide.Second, Zone = Zone.Deck });
        }
        var loop = new GameLoop(state, new StubCardDb(), new DeterministicRng(seed: 1, counter: 0));
        return (loop, state);
    }

    private sealed class StubCardDb : ICardDatabase
    {
        public ICardScript Get(CardId id) => throw new System.InvalidOperationException(
            $"StubCardDb does not implement Get({id}) — tests must avoid actions that resolve card scripts.");
        public bool TryGet(CardId id, out ICardScript script) { script = null!; return false; }
        public IEnumerable<ICardScript> All() => System.Array.Empty<ICardScript>();
    }
}
