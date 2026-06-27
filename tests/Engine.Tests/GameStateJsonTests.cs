using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine.Serialization;
using Xunit;

namespace ShadowCardSmash.Tests.Engine;

public class GameStateJsonTests
{
    [Fact]
    public void RoundTrip_EmptyState_PreservesDefaults()
    {
        var state = new GameState();
        var clone = GameStateJson.Deserialize(GameStateJson.Serialize(state));

        Assert.Equal(state.TurnNumber, clone.TurnNumber);
        Assert.Equal(state.CurrentPlayer, clone.CurrentPlayer);
        Assert.Equal(state.Phase, clone.Phase);
        Assert.Equal(state.Result, clone.Result);
        Assert.Equal(state.RandomSeed, clone.RandomSeed);
        Assert.Equal(state.RandomCounter, clone.RandomCounter);
        Assert.Equal(state.NextInstanceIdSeed, clone.NextInstanceIdSeed);
        Assert.Equal(2, clone.Players.Length);
        Assert.Equal(PlayerSide.First, clone.Players[0].Side);
        Assert.Equal(PlayerSide.Second, clone.Players[1].Side);
        Assert.Equal(PlayerState.FieldSize, clone.Players[0].Field.Length);
    }

    [Fact]
    public void RoundTrip_IsIdempotent_ForComplexState()
    {
        var state = BuildComplexState();
        var json1 = GameStateJson.Serialize(state);
        var clone = GameStateJson.Deserialize(json1);
        var json2 = GameStateJson.Serialize(clone);

        // Idempotency: serialize → deserialize → serialize again must match byte-for-byte.
        Assert.Equal(json1, json2);
    }

    [Fact]
    public void RoundTrip_PreservesRuntimeCardFields()
    {
        var state = new GameState { TurnNumber = 5, CurrentPlayer = PlayerSide.Second };
        var p = state.GetPlayer(PlayerSide.First);
        var minion = new RuntimeCard
        {
            Instance = state.AllocateInstanceId(),
            Card = new CardId(3003),
            Owner = PlayerSide.First,
            Zone = Zone.Field,
            CurrentAttack = 4,
            CurrentHealth = 2,
            MaxHealth = 3,
            IsEvolved = true,
            CanAttackThisTurn = false,
            IsSilenced = true,
            AttacksThisTurn = 1,
            Countdown = -1,
            Keywords = Keyword.Ward | Keyword.Rush,
            BarrierStacks = 1,
            SummonedThisTurn = false,
            OnDeathSuppressed = true,
        };
        minion.Buffs.Add(new BuffData { AttackDelta = 2, HealthDelta = 0, DurationTurns = 3, Source = new InstanceId(42), Tag = "buff-tag" });
        p.Field[2].Occupant = minion;
        p.Field[2].Effects.Add(new TileEffect { EffectKey = "burn", Value = 1, RemainingTurns = 2, Source = new InstanceId(7) });

        var clone = GameStateJson.Deserialize(GameStateJson.Serialize(state));
        var cloneMinion = clone.GetPlayer(PlayerSide.First).Field[2].Occupant!;

        Assert.Equal(minion.Instance, cloneMinion.Instance);
        Assert.Equal(minion.Card, cloneMinion.Card);
        Assert.Equal(minion.Owner, cloneMinion.Owner);
        Assert.Equal(minion.Zone, cloneMinion.Zone);
        Assert.Equal(minion.CurrentAttack, cloneMinion.CurrentAttack);
        Assert.Equal(minion.CurrentHealth, cloneMinion.CurrentHealth);
        Assert.Equal(minion.MaxHealth, cloneMinion.MaxHealth);
        Assert.Equal(minion.IsEvolved, cloneMinion.IsEvolved);
        Assert.Equal(minion.CanAttackThisTurn, cloneMinion.CanAttackThisTurn);
        Assert.Equal(minion.IsSilenced, cloneMinion.IsSilenced);
        Assert.Equal(minion.AttacksThisTurn, cloneMinion.AttacksThisTurn);
        Assert.Equal(minion.Countdown, cloneMinion.Countdown);
        Assert.Equal(minion.Keywords, cloneMinion.Keywords);
        Assert.Equal(minion.BarrierStacks, cloneMinion.BarrierStacks);
        Assert.Equal(minion.SummonedThisTurn, cloneMinion.SummonedThisTurn);
        Assert.Equal(minion.OnDeathSuppressed, cloneMinion.OnDeathSuppressed);

        Assert.Single(cloneMinion.Buffs);
        var buff = cloneMinion.Buffs[0];
        Assert.Equal(2, buff.AttackDelta);
        Assert.Equal(3, buff.DurationTurns);
        Assert.Equal(new InstanceId(42), buff.Source);
        Assert.Equal("buff-tag", buff.Tag);

        Assert.Single(clone.GetPlayer(PlayerSide.First).Field[2].Effects);
        var fx = clone.GetPlayer(PlayerSide.First).Field[2].Effects[0];
        Assert.Equal("burn", fx.EffectKey);
        Assert.Equal(2, fx.RemainingTurns);
    }

    [Fact]
    public void RoundTrip_CarriesInstanceIdAllocator()
    {
        var state = new GameState();
        state.AllocateInstanceId();
        state.AllocateInstanceId();
        state.AllocateInstanceId();

        var clone = GameStateJson.Deserialize(GameStateJson.Serialize(state));
        var next = clone.AllocateInstanceId();
        Assert.Equal(4, next.Value);
    }

    [Fact]
    public void RoundTrip_PreservesMulliganState()
    {
        var state = new GameState();
        state.Mulligan.Confirmed[0] = true;
        state.Mulligan.ChosenSwapIndices[1].AddRange(new[] { 0, 2, 3 });

        var clone = GameStateJson.Deserialize(GameStateJson.Serialize(state));
        Assert.True(clone.Mulligan.Confirmed[0]);
        Assert.False(clone.Mulligan.Confirmed[1]);
        Assert.Equal(new[] { 0, 2, 3 }, clone.Mulligan.ChosenSwapIndices[1]);
        Assert.Empty(clone.Mulligan.ChosenSwapIndices[0]);
    }

    [Fact]
    public void RoundTrip_DeepCopy_NotSameReference()
    {
        var state = BuildComplexState();
        var clone = GameStateJson.Deserialize(GameStateJson.Serialize(state));

        Assert.NotSame(state, clone);
        Assert.NotSame(state.Players[0], clone.Players[0]);
        Assert.NotSame(state.Mulligan, clone.Mulligan);
        if (state.Players[0].Hand.Count > 0)
            Assert.NotSame(state.Players[0].Hand[0], clone.Players[0].Hand[0]);
    }

    private static GameState BuildComplexState()
    {
        var state = new GameState
        {
            TurnNumber = 7,
            CurrentPlayer = PlayerSide.Second,
            Phase = GamePhase.Main,
            Result = GameResult.InProgress,
            RandomSeed = 12345,
            RandomCounter = 999,
        };

        var p0 = state.GetPlayer(PlayerSide.First);
        p0.HeroClass = HeroClass.Empire;
        p0.Health = 32;
        p0.MaxHealth = 40;
        p0.Mana = 5;
        p0.MaxMana = 7;
        p0.EvolutionPoints = 2;
        p0.HasEvolvedThisTurn = true;
        p0.BarrierStacks = 1;
        p0.FatigueCounter = 3;
        p0.MinionDestroyedThisTurn = true;
        p0.SelfDamageThisTurn = 4;
        p0.TotalSelfDamage = 8;
        p0.SelfDamageCount = 2;
        p0.CompensationCard = new CardId(9999);

        for (int i = 0; i < 3; i++)
        {
            p0.Hand.Add(new RuntimeCard
            {
                Instance = state.AllocateInstanceId(),
                Card = new CardId(2000 + i),
                Owner = PlayerSide.First,
                Zone = Zone.Hand,
            });
        }
        for (int i = 0; i < 8; i++)
        {
            p0.Deck.Add(new RuntimeCard
            {
                Instance = state.AllocateInstanceId(),
                Card = new CardId(1000 + i),
                Owner = PlayerSide.First,
                Zone = Zone.Deck,
            });
        }
        p0.Field[1].Occupant = new RuntimeCard
        {
            Instance = state.AllocateInstanceId(),
            Card = new CardId(3001),
            Owner = PlayerSide.First,
            Zone = Zone.Field,
            CurrentAttack = 3,
            CurrentHealth = 4,
            MaxHealth = 5,
            Keywords = Keyword.Ward,
        };
        p0.TerrainSlot.Occupant = new RuntimeCard
        {
            Instance = state.AllocateInstanceId(),
            Card = new CardId(4001),
            Owner = PlayerSide.First,
            Zone = Zone.Field,
        };
        p0.Graveyard.Add(new RuntimeCard
        {
            Instance = state.AllocateInstanceId(),
            Card = new CardId(2099),
            Owner = PlayerSide.First,
            Zone = Zone.Graveyard,
        });
        p0.Vanished.Add(new RuntimeCard
        {
            Instance = state.AllocateInstanceId(),
            Card = new CardId(2100),
            Owner = PlayerSide.First,
            Zone = Zone.Vanished,
        });

        var p1 = state.GetPlayer(PlayerSide.Second);
        p1.HeroClass = HeroClass.Forsaken;
        p1.Health = 25;
        p1.Mana = 7;
        p1.MaxMana = 7;
        for (int i = 0; i < 2; i++)
        {
            p1.Hand.Add(new RuntimeCard
            {
                Instance = state.AllocateInstanceId(),
                Card = new CardId(5000 + i),
                Owner = PlayerSide.Second,
                Zone = Zone.Hand,
            });
        }

        state.Mulligan.Confirmed[0] = true;
        state.Mulligan.Confirmed[1] = true;

        return state;
    }
}
