using ShadowCardSmash.Domain;
using Xunit;

namespace ShadowCardSmash.Tests.Engine;

public class DomainSnapshotTests
{
    [Fact]
    public void Snapshot_ProducesDeepCopy_OfPlayerHands()
    {
        var state = new GameState { TurnNumber = 3 };
        var p0 = state.GetPlayer(PlayerSide.First);
        p0.Hand.Add(new RuntimeCard
        {
            Instance = state.AllocateInstanceId(),
            Card = new CardId(1001),
            Owner = PlayerSide.First,
            Zone = Zone.Hand,
            CurrentAttack = 2,
            CurrentHealth = 3,
            MaxHealth = 3,
        });

        var snap = state.Snapshot();

        Assert.Equal(3, snap.TurnNumber);
        Assert.Single(snap.GetPlayer(PlayerSide.First).Hand);
        Assert.NotSame(p0.Hand[0], snap.GetPlayer(PlayerSide.First).Hand[0]);

        // Mutating live state should not change snapshot.
        p0.Hand[0].CurrentHealth = 0;
        Assert.Equal(3, snap.GetPlayer(PlayerSide.First).Hand[0].CurrentHealth);
    }

    [Fact]
    public void Snapshot_PreservesFieldAndBuffs()
    {
        var state = new GameState();
        var p0 = state.GetPlayer(PlayerSide.First);
        var minion = new RuntimeCard
        {
            Instance = state.AllocateInstanceId(),
            Card = new CardId(2001),
            Owner = PlayerSide.First,
            Zone = Zone.Field,
            CurrentAttack = 1,
            CurrentHealth = 1,
            MaxHealth = 1,
        };
        minion.Buffs.Add(new BuffData { AttackDelta = 1, HealthDelta = 1, DurationTurns = 2 });
        p0.Field[2].Occupant = minion;

        var snap = state.Snapshot();
        var snapMinion = snap.GetPlayer(PlayerSide.First).Field[2].Occupant!;

        Assert.NotSame(minion, snapMinion);
        Assert.NotSame(minion.Buffs[0], snapMinion.Buffs[0]);
        Assert.Equal(1, snapMinion.Buffs[0].AttackDelta);

        minion.Buffs[0].AttackDelta = 99;
        Assert.Equal(1, snapMinion.Buffs[0].AttackDelta);
    }

    [Fact]
    public void InstanceId_AllocatesMonotonically()
    {
        var state = new GameState();
        var a = state.AllocateInstanceId();
        var b = state.AllocateInstanceId();
        var c = state.AllocateInstanceId();

        Assert.Equal(1, a.Value);
        Assert.Equal(2, b.Value);
        Assert.Equal(3, c.Value);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Snapshot_CarriesInstanceIdAllocator()
    {
        var state = new GameState();
        state.AllocateInstanceId();
        state.AllocateInstanceId();

        var snap = state.Snapshot();
        var next = snap.AllocateInstanceId();
        Assert.Equal(3, next.Value);
    }

    [Fact]
    public void PlayerSide_Opponent_Flips()
    {
        Assert.Equal(PlayerSide.Second, PlayerSide.First.Opponent());
        Assert.Equal(PlayerSide.First, PlayerSide.Second.Opponent());
    }

    [Fact]
    public void Keyword_FlagsCompose()
    {
        var card = new RuntimeCard();
        card.AddKeyword(Keyword.Ward);
        card.AddKeyword(Keyword.Rush);
        Assert.True(card.HasKeyword(Keyword.Ward));
        Assert.True(card.HasKeyword(Keyword.Rush));
        Assert.False(card.HasKeyword(Keyword.Storm));

        card.RemoveKeyword(Keyword.Ward);
        Assert.False(card.HasKeyword(Keyword.Ward));
        Assert.True(card.HasKeyword(Keyword.Rush));
    }
}
