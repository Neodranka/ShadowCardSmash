using ShadowCardSmash.Cards;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;
using Xunit;

namespace ShadowCardSmash.Tests.Engine;

public class EffectPrimitivesTests
{
    [Card(9001)]
    private sealed class StubVanilla : MinionCard
    {
        public override string Name => "StubVanilla";
        public override int Cost => 1;
        public override HeroClass HeroClass => HeroClass.Neutral;
        public override Rarity Rarity => Rarity.Bronze;
        public override int BaseAttack => 2;
        public override int BaseHealth => 3;
    }

    [Card(9002)]
    private sealed class StubOnDeath : MinionCard
    {
        public override string Name => "StubOnDeath";
        public override int Cost => 1;
        public override HeroClass HeroClass => HeroClass.Neutral;
        public override Rarity Rarity => Rarity.Bronze;
        public override int BaseAttack => 1;
        public override int BaseHealth => 1;

        public override void OnDeath(GameContext ctx) => ctx.Damage(ctx.SourceSide.Opponent(), 3);
    }

    private static (GameLoop loop, CardRegistry reg) NewGame()
    {
        var reg = new CardRegistry();
        reg.Register(new StubVanilla());
        reg.Register(new StubOnDeath());
        var state = new GameState { RandomSeed = 1 };
        return (new GameLoop(state, reg), reg);
    }

    [Fact]
    public void Summon_PutsMinionOnField_AndPublishesEvent()
    {
        var (loop, _) = NewGame();
        var ctx = new GameContext(loop.State, loop.Rng, loop, loop.CardDatabase, PlayerSide.First);

        var id = EffectPrimitives.Summon(ctx, new CardId(9001), PlayerSide.First);

        Assert.NotNull(id);
        Assert.Equal(2, loop.State.GetPlayer(PlayerSide.First).Field[0].Occupant!.CurrentAttack);
        Assert.Contains(loop.EventLog, e => e is MinionSummonedEvent);
    }

    [Fact]
    public void Damage_LethalToMinion_TriggersDeathHook()
    {
        var (loop, _) = NewGame();
        var ctx = new GameContext(loop.State, loop.Rng, loop, loop.CardDatabase, PlayerSide.First);
        EffectPrimitives.Summon(ctx, new CardId(9002), PlayerSide.First);
        var target = loop.State.GetPlayer(PlayerSide.First).Field[0].Occupant!;

        EffectPrimitives.DamageMinion(ctx, target, 5);

        // Death hook (StubOnDeath) deals 3 damage to opposing player.
        Assert.Equal(40 - 3, loop.State.GetPlayer(PlayerSide.Second).Health);
        Assert.Contains(loop.EventLog, e => e is MinionDestroyedEvent);
        Assert.Contains(loop.EventLog, e => e is PlayerDamagedEvent);
    }

    [Fact]
    public void Draw_FromEmptyDeck_AppliesEscalatingFatigue()
    {
        var (loop, _) = NewGame();
        var ctx = new GameContext(loop.State, loop.Rng, loop, loop.CardDatabase, PlayerSide.First);

        EffectPrimitives.Draw(ctx, PlayerSide.First, 3);

        var p = loop.State.GetPlayer(PlayerSide.First);
        Assert.Equal(3, p.FatigueCounter);
        Assert.Equal(40 - (1 + 2 + 3), p.Health);
    }

    [Fact]
    public void Draw_OverHandLimit_RoutesToGraveyard()
    {
        var (loop, _) = NewGame();
        var p = loop.State.GetPlayer(PlayerSide.First);
        for (int i = 0; i < PlayerState.HandLimit; i++)
            p.Hand.Add(new RuntimeCard { Instance = loop.State.AllocateInstanceId(), Card = new CardId(9001) });
        p.Deck.Add(new RuntimeCard { Instance = loop.State.AllocateInstanceId(), Card = new CardId(9001) });

        var ctx = new GameContext(loop.State, loop.Rng, loop, loop.CardDatabase, PlayerSide.First);
        EffectPrimitives.Draw(ctx, PlayerSide.First, 1);

        Assert.Equal(PlayerState.HandLimit, p.Hand.Count);
        Assert.Single(p.Graveyard);
        Assert.Contains(loop.EventLog, e => e is CardOverdrawnEvent);
    }

    [Fact]
    public void GameLoop_LoopGuard_FiresOnExcessivePublish()
    {
        var (loop, _) = NewGame();
        loop.MaxSamePublishCount = 3;
        var ctx = new GameContext(loop.State, loop.Rng, loop, loop.CardDatabase, PlayerSide.First);

        // Manually fire the same event type many times.
        Assert.Throws<RuleViolationException>(() =>
        {
            for (int i = 0; i < 10; i++) loop.Publish(new ManaChangedEvent(PlayerSide.First, i, 10), ctx);
        });
    }

    [Fact]
    public void DeterministicRng_SameSeedSameSequence()
    {
        var a = new DeterministicRng(42, 0);
        var b = new DeterministicRng(42, 0);
        for (int i = 0; i < 100; i++) Assert.Equal(a.Next(0, 1000), b.Next(0, 1000));
    }

    [Fact]
    public void CardRegistry_RejectsDuplicateIds()
    {
        var reg = new CardRegistry();
        reg.Register(new StubVanilla());
        Assert.Throws<InvalidOperationException>(() => reg.Register(new StubVanilla()));
    }
}
