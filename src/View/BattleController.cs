using System.Reflection;
using Godot;
using ShadowCardSmash.Cards;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.View;

/// <summary>
/// Glue between GameLoop and BoardView. Owns the input state machine and re-renders after every event chain.
/// V1 runs hot seat (both players share one screen; LocalSide auto-tracks CurrentPlayer).
/// </summary>
public partial class BattleController : Node
{
    private GameLoop _loop = null!;
    private CardRegistry _registry = null!;
    private BoardView _board = null!;

    // Input state machine.
    private enum Mode { Idle, AwaitTarget }
    private Mode _mode = Mode.Idle;
    private InstanceId _selectedHandInstance;
    private CardId _selectedCardId;
    private TargetSpec _selectedSpec;

    public PlayerSide LocalSide => _loop.State.CurrentPlayer; // hot-seat: always view current player

    public override void _Ready()
    {
        _registry = CardRegistry.ScanAssembly(Assembly.GetExecutingAssembly());

        _board = new BoardView();
        AddChild(_board);

        _board.HandCardClicked += OnHandCardClicked;
        _board.TileClicked += OnTileClicked;
        _board.HeroClicked += OnHeroClicked;
        _board.EndTurnClicked += OnEndTurnClicked;

        StartHotSeatGame();
    }

    private void StartHotSeatGame()
    {
        var state = new GameState();
        var rng = new DeterministicRng(seed: (int)Time.GetTicksMsec(), counter: 0);
        _loop = new GameLoop(state, _registry, rng);

        var sample = new CardId(2001); // BloodSeller
        var deck = new List<CardId>();
        for (int i = 0; i < 40; i++) deck.Add(sample);

        var first = new GameInitializer.SeatConfig(deck, HeroClass.Vampire, null);
        var second = new GameInitializer.SeatConfig(deck, HeroClass.Vampire, null);
        GameInitializer.Begin(_loop, seed: state.RandomSeed, first, second);

        // V1: auto-confirm mulligan (no UI for mulligan yet).
        _loop.Submit(new MulliganAction(PlayerSide.First, System.Array.Empty<int>()));
        _loop.Submit(new MulliganAction(PlayerSide.Second, System.Array.Empty<int>()));

        Rebind();
    }

    private void OnHandCardClicked(int sideIndex, int instanceId)
    {
        var side = (PlayerSide)sideIndex;
        if (side != LocalSide || _loop.IsGameOver) return;

        var card = _loop.State.GetPlayer(side).Hand.FirstOrDefault(c => c.Instance.Value == instanceId);
        if (card is null) return;
        var script = _registry.Get(card.Card);

        if (_mode == Mode.AwaitTarget && _selectedHandInstance.Value == instanceId)
        {
            _mode = Mode.Idle;
            _board.SetStatus("");
            return;
        }

        _selectedHandInstance = card.Instance;
        _selectedCardId = card.Card;
        _selectedSpec = script.PlayTarget;

        if (script.CardType == CardType.Minion || script.CardType == CardType.Amulet)
        {
            _mode = Mode.AwaitTarget;
            _board.SetStatus($"Pick a friendly tile for {script.Name}");
            HighlightEmptyTiles(LocalSide);
            return;
        }

        // Spell.
        if (_selectedSpec == TargetSpec.None)
        {
            TrySubmit(new PlayCardAction(LocalSide, card.Instance, null, null, null));
            return;
        }
        _mode = Mode.AwaitTarget;
        _board.SetStatus($"Pick a target for {script.Name}");
    }

    private void OnTileClicked(int sideIndex, int tileIndex)
    {
        if (_mode != Mode.AwaitTarget || _loop.IsGameOver) return;
        var side = (PlayerSide)sideIndex;
        var script = _registry.Get(_selectedCardId);

        switch (script.CardType)
        {
            case CardType.Minion:
            case CardType.Amulet:
                if (side != LocalSide) { _board.SetStatus("That tile is on the wrong side."); return; }
                TrySubmit(new PlayCardAction(LocalSide, _selectedHandInstance, tileIndex, null, null));
                return;

            case CardType.Spell:
                // Spell aimed at a minion-on-tile.
                if (_loop.State.GetPlayer(side).Field[tileIndex].Occupant is { } occ)
                {
                    TrySubmit(new PlayCardAction(LocalSide, _selectedHandInstance, null, occ.Instance, null));
                }
                return;
        }
    }

    private void OnHeroClicked(int sideIndex)
    {
        if (_loop.IsGameOver) return;
        var side = (PlayerSide)sideIndex;

        if (_mode == Mode.AwaitTarget)
        {
            // Spell aimed at hero?
            var script = _registry.Get(_selectedCardId);
            if (script.CardType == CardType.Spell)
            {
                TrySubmit(new PlayCardAction(LocalSide, _selectedHandInstance, null, null, side));
            }
            return;
        }

        // Idle: clicking the enemy hero is a face attack. V1 simplification:
        // require the player to first click a minion to make it the attacker, but we have no minion-selection mode yet.
        // For V1 we wire face attacks via the minion-on-tile being clicked; clicking the hero alone is a no-op.
        _board.SetStatus(side == LocalSide.Opponent() ? "Click a friendly minion first to attack." : "");
    }

    private void OnEndTurnClicked()
    {
        if (_loop.IsGameOver) return;
        if (_loop.State.Phase != GamePhase.Main) return;
        TrySubmit(new EndTurnAction(LocalSide));
    }

    private void TrySubmit(IGameAction action)
    {
        try
        {
            _loop.Submit(action);
        }
        catch (InvalidActionException e)
        {
            _board.SetStatus($"Invalid: {e.Message}");
            _mode = Mode.Idle;
            Rebind();
            return;
        }
        _mode = Mode.Idle;
        Rebind();
    }

    private void Rebind() => _board.Rebind(_loop.State, _registry, LocalSide);

    private void HighlightEmptyTiles(PlayerSide side)
    {
        var tiles = side == PlayerSide.First ? _board.MyTiles : _board.EnemyTiles;
        var p = _loop.State.GetPlayer(side);
        for (int i = 0; i < tiles.Length; i++) tiles[i].Highlight(p.Field[i].IsEmpty);
    }
}
