using System.Reflection;
using ShadowCardSmash.Cards;
using ShadowCardSmash.Cards.Vampire;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;
using Xunit;

namespace ShadowCardSmash.Tests.Cards;

public class EndToEndGameFlowTests
{
    private static readonly CardRegistry Registry = CardRegistry.ScanAssembly(Assembly.GetExecutingAssembly());

    private static (GameLoop loop, GameState state) NewGame()
    {
        var state = new GameState();
        var loop = new GameLoop(state, Registry, new DeterministicRng(seed: 7, counter: 0));
        return (loop, state);
    }

    private static IReadOnlyList<CardId> BuildDeck(CardId fill)
    {
        var d = new List<CardId>();
        for (int i = 0; i < 40; i++) d.Add(fill);
        return d;
    }

    [Fact]
    public void ScanAssembly_FindsRegisteredCards()
    {
        Assert.True(Registry.Count >= 3);
        Assert.Equal("卖血者", Registry.Get(new CardId(2001)).Name);
        Assert.Equal("撕咬", Registry.Get(new CardId(2004)).Name);
        Assert.Equal(CardType.Spell, Registry.Get(new CardId(2004)).CardType);
    }

    [Fact]
    public void GameInitializer_SeedsBothPlayersWithStartingHands()
    {
        var (loop, state) = NewGame();
        var first = new GameInitializer.SeatConfig(BuildDeck(new CardId(2001)), HeroClass.Vampire, null);
        var second = new GameInitializer.SeatConfig(BuildDeck(new CardId(1001)), HeroClass.Neutral, null);

        GameInitializer.Begin(loop, seed: 7, first, second);

        Assert.Equal(GamePhase.Mulligan, state.Phase);
        Assert.Equal(GameInitializer.FirstPlayerStartingHand, state.GetPlayer(PlayerSide.First).Hand.Count);
        Assert.Equal(GameInitializer.SecondPlayerStartingHand, state.GetPlayer(PlayerSide.Second).Hand.Count);
        Assert.Equal(40 - 4, state.GetPlayer(PlayerSide.First).Deck.Count);
    }

    [Fact]
    public void MulliganAction_ConfirmedByBoth_StartsTurn1()
    {
        var (loop, state) = NewGame();
        GameInitializer.Begin(loop,
            seed: 7,
            new GameInitializer.SeatConfig(BuildDeck(new CardId(2001)), HeroClass.Vampire, null),
            new GameInitializer.SeatConfig(BuildDeck(new CardId(1001)), HeroClass.Neutral, null));

        loop.Submit(new MulliganAction(PlayerSide.First, Array.Empty<int>()));
        loop.Submit(new MulliganAction(PlayerSide.Second, Array.Empty<int>()));

        Assert.Equal(GamePhase.Main, state.Phase);
        Assert.Equal(PlayerSide.First, state.CurrentPlayer);
        Assert.Equal(1, state.TurnNumber);
        Assert.Equal(1, state.GetPlayer(PlayerSide.First).Mana);
        Assert.Equal(1, state.GetPlayer(PlayerSide.First).MaxMana);
        Assert.Equal(GameInitializer.FirstPlayerStartingHand + 1, state.GetPlayer(PlayerSide.First).Hand.Count);
    }

    [Fact]
    public void PlayCardAction_BloodSeller_SelfDamagesAndDraws()
    {
        var (loop, state) = NewGame();
        GameInitializer.Begin(loop,
            seed: 7,
            new GameInitializer.SeatConfig(BuildDeck(new CardId(2001)), HeroClass.Vampire, null),
            new GameInitializer.SeatConfig(BuildDeck(new CardId(1001)), HeroClass.Neutral, null));
        loop.Submit(new MulliganAction(PlayerSide.First, Array.Empty<int>()));
        loop.Submit(new MulliganAction(PlayerSide.Second, Array.Empty<int>()));

        var p = state.GetPlayer(PlayerSide.First);
        int handBefore = p.Hand.Count;
        var blood = p.Hand[0];

        loop.Submit(new PlayCardAction(PlayerSide.First, blood.Instance, TileIndex: 0, null, null));

        Assert.Equal(0, p.Mana);
        Assert.Equal(40 - 2, p.Health);
        Assert.Equal(2, p.TotalSelfDamage);
        Assert.NotNull(p.Field[0].Occupant);
        Assert.Equal(1, p.Field[0].Occupant!.CurrentAttack);
        // -1 hand for played card, +1 for OnPlay draw  ⇒ net 0.
        Assert.Equal(handBefore, p.Hand.Count);
    }

    [Fact]
    public void EndTurnAction_PassesControlToOpponent_AndRaisesMana()
    {
        var (loop, state) = NewGame();
        GameInitializer.Begin(loop,
            seed: 7,
            new GameInitializer.SeatConfig(BuildDeck(new CardId(2001)), HeroClass.Vampire, null),
            new GameInitializer.SeatConfig(BuildDeck(new CardId(1001)), HeroClass.Neutral, null));
        loop.Submit(new MulliganAction(PlayerSide.First, Array.Empty<int>()));
        loop.Submit(new MulliganAction(PlayerSide.Second, Array.Empty<int>()));

        loop.Submit(new EndTurnAction(PlayerSide.First));

        Assert.Equal(PlayerSide.Second, state.CurrentPlayer);
        Assert.Equal(2, state.TurnNumber);
        Assert.Equal(1, state.GetPlayer(PlayerSide.Second).MaxMana);
    }

    [Fact]
    public void AttackAction_WardEnforced()
    {
        var (loop, state) = NewGame();
        GameInitializer.Begin(loop,
            seed: 7,
            new GameInitializer.SeatConfig(BuildDeck(new CardId(2001)), HeroClass.Vampire, null),
            new GameInitializer.SeatConfig(BuildDeck(new CardId(1001)), HeroClass.Neutral, null));
        loop.Submit(new MulliganAction(PlayerSide.First, Array.Empty<int>()));
        loop.Submit(new MulliganAction(PlayerSide.Second, Array.Empty<int>()));

        // Set up an attacker for first player and a ward minion for second player by direct field manipulation.
        var p0 = state.GetPlayer(PlayerSide.First);
        var attacker = new RuntimeCard
        {
            Instance = state.AllocateInstanceId(),
            Card = new CardId(2001), Owner = PlayerSide.First, Zone = Zone.Field,
            CurrentAttack = 3, CurrentHealth = 3, MaxHealth = 3,
            CanAttackThisTurn = true,
        };
        p0.Field[0].Occupant = attacker;

        var p1 = state.GetPlayer(PlayerSide.Second);
        var ward = new RuntimeCard
        {
            Instance = state.AllocateInstanceId(),
            Card = new CardId(1001), Owner = PlayerSide.Second, Zone = Zone.Field,
            CurrentAttack = 0, CurrentHealth = 3, MaxHealth = 3,
            Keywords = Keyword.Ward,
        };
        p1.Field[2].Occupant = ward;

        var attackFace = new AttackAction(PlayerSide.First, attacker.Instance, null, PlayerSide.Second);
        Assert.False(attackFace.Validate(state).IsOk);

        var attackWard = new AttackAction(PlayerSide.First, attacker.Instance, ward.Instance, null);
        Assert.True(attackWard.Validate(state).IsOk);
        loop.Submit(attackWard);

        // Ward was 0/3, attacker 3/3 → ward destroyed, attacker took 0.
        Assert.Null(p1.Field[2].Occupant);
        Assert.Equal(3, p0.Field[0].Occupant!.CurrentHealth);
    }

    [Fact]
    public void DeckValidator_RejectsWrongClassAndOversize()
    {
        var deck = BuildDeck(new CardId(2001)); // Vampire cards in Neutral deck
        var bad = DeckValidator.Validate(deck, HeroClass.ClassB, Registry);
        Assert.False(bad.IsValid);

        var listOfThree = new List<CardId> { new(2001), new(2001), new(2001) };
        var tooSmall = DeckValidator.Validate(listOfThree, HeroClass.Vampire, Registry);
        Assert.False(tooSmall.IsValid);

        var ok = DeckValidator.Validate(BuildDeck(new CardId(2001)), HeroClass.Vampire, Registry);
        Assert.True(ok.IsValid);
    }
}
