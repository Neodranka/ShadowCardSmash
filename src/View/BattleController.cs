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
    private enum Mode { Idle, AwaitPlayTarget, AwaitAttackTarget }
    private Mode _mode = Mode.Idle;
    private InstanceId _selectedHandInstance;
    private CardId _selectedCardId;
    private TargetSpec _selectedSpec;
    private InstanceId _selectedAttacker;

    public PlayerSide LocalSide => _loop.State.CurrentPlayer; // hot-seat: always view current player

    public override void _Ready()
    {
        _registry = CardRegistry.ScanAssembly(Assembly.GetExecutingAssembly());

        _board = new BoardView();
        AddChild(_board);

        _board.HandCardClicked += OnHandCardClicked;
        _board.TileClicked += OnTileClicked;
        _board.MinionClicked += OnMinionClicked;
        _board.HeroClicked += OnHeroClicked;
        _board.EndTurnClicked += OnEndTurnClicked;

        StartHotSeatGame();
    }

    private void StartHotSeatGame()
    {
        var state = new GameState();
        var rng = new DeterministicRng(seed: (int)Time.GetTicksMsec(), counter: 0);
        _loop = new GameLoop(state, _registry, rng);

        // V1 demo deck: half BloodSeller (1/1 OnPlay self-damage + draw), half TrainingDummy (0/3 Ward,
        // no OnPlay) so the player can see the hand count actually shrink when they drop a Dummy.
        var deck = new List<CardId>();
        for (int i = 0; i < 20; i++) deck.Add(new CardId(2001));
        for (int i = 0; i < 20; i++) deck.Add(new CardId(1001));

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

        // Toggle off if user clicks the same card again.
        if (_mode == Mode.AwaitPlayTarget && _selectedHandInstance.Value == instanceId)
        {
            ResetMode();
            return;
        }

        // Switching from another mode resets first.
        ResetMode();

        _selectedHandInstance = card.Instance;
        _selectedCardId = card.Card;
        _selectedSpec = script.PlayTarget;

        if (script.CardType == CardType.Minion || script.CardType == CardType.Amulet)
        {
            _mode = Mode.AwaitPlayTarget;
            _board.SetStatus($"选择一个空格子放置 {script.Name}");
            HighlightFriendlyEmptyTiles();
            return;
        }

        // Spell.
        if (_selectedSpec == TargetSpec.None)
        {
            TrySubmit(new PlayCardAction(LocalSide, card.Instance, null, null, null));
            return;
        }
        _mode = Mode.AwaitPlayTarget;
        _board.SetStatus($"为 {script.Name} 选择一个目标");
    }

    private void OnTileClicked(int sideIndex, int tileIndex)
    {
        if (_loop.IsGameOver) return;
        var side = (PlayerSide)sideIndex;

        if (_mode == Mode.AwaitPlayTarget)
        {
            var script = _registry.Get(_selectedCardId);
            switch (script.CardType)
            {
                case CardType.Minion:
                case CardType.Amulet:
                    if (side != LocalSide) { _board.SetStatus("必须放在己方场地"); return; }
                    TrySubmit(new PlayCardAction(LocalSide, _selectedHandInstance, tileIndex, null, null));
                    return;
                case CardType.Spell:
                    if (_loop.State.GetPlayer(side).Field[tileIndex].Occupant is { } occ)
                        TrySubmit(new PlayCardAction(LocalSide, _selectedHandInstance, null, occ.Instance, null));
                    return;
            }
        }
        // Clicking empty space in Idle or Attack mode just cancels selection.
        ResetMode();
    }

    private void OnMinionClicked(int sideIndex, int instanceId)
    {
        if (_loop.IsGameOver) return;
        var side = (PlayerSide)sideIndex;
        var instance = new InstanceId(instanceId);

        // Spell aimed at a minion takes priority.
        if (_mode == Mode.AwaitPlayTarget)
        {
            var script = _registry.Get(_selectedCardId);
            if (script.CardType == CardType.Spell)
            {
                TrySubmit(new PlayCardAction(LocalSide, _selectedHandInstance, null, instance, null));
            }
            return;
        }

        // Attack target.
        if (_mode == Mode.AwaitAttackTarget)
        {
            if (side == LocalSide.Opponent())
                TrySubmit(new AttackAction(LocalSide, _selectedAttacker, instance, null));
            else
                ResetMode(); // reclicking own field cancels
            return;
        }

        // Idle: clicking own minion → enter attack mode if it can attack.
        if (side == LocalSide)
        {
            var attacker = _loop.State.GetPlayer(side).FindOnField(instance);
            if (attacker is null) return;

            string? blocker = DiagnoseAttackBlocker(attacker);
            if (blocker is not null) { _board.SetStatus(blocker); return; }

            _selectedAttacker = instance;
            _mode = Mode.AwaitAttackTarget;
            _board.SetStatus(CombatResolver.EnemyHasWard(_loop.State, LocalSide)
                ? "对方有守护，必须先攻击守护随从"
                : "选择攻击目标（敌方随从或英雄）");
        }
    }

    private void OnHeroClicked(int sideIndex)
    {
        if (_loop.IsGameOver) return;
        var side = (PlayerSide)sideIndex;

        if (_mode == Mode.AwaitAttackTarget && side == LocalSide.Opponent())
        {
            TrySubmit(new AttackAction(LocalSide, _selectedAttacker, null, side));
            return;
        }

        if (_mode == Mode.AwaitPlayTarget)
        {
            var script = _registry.Get(_selectedCardId);
            if (script.CardType == CardType.Spell)
                TrySubmit(new PlayCardAction(LocalSide, _selectedHandInstance, null, null, side));
        }
    }

    private void OnEndTurnClicked()
    {
        if (_loop.IsGameOver) return;
        if (_loop.State.Phase != GamePhase.Main) return;
        ResetMode();
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
            _board.SetStatus($"非法操作: {e.Message}");
            ResetMode();
            Rebind();
            return;
        }
        ResetMode();
        Rebind();
    }

    private void Rebind() => _board.Rebind(_loop.State, _registry, LocalSide);

    private void ResetMode()
    {
        _mode = Mode.Idle;
        _selectedHandInstance = InstanceId.None;
        _selectedAttacker = InstanceId.None;
        _board.SetStatus("");
        _board.ClearHighlights();
    }

    private void HighlightFriendlyEmptyTiles()
    {
        var tiles = _board.MyTiles; // MyTiles now carry LocalSide after Rebind
        var p = _loop.State.GetPlayer(LocalSide);
        for (int i = 0; i < tiles.Length; i++) tiles[i].Highlight(p.Field[i].IsEmpty);
    }

    private static string? DiagnoseAttackBlocker(RuntimeCard m)
    {
        if (m.CurrentAttack <= 0) return "攻击力为 0，无法攻击";
        if (m.AttacksThisTurn > 0) return "本回合已经攻击过";
        if (m.SummonedThisTurn && !m.HasKeyword(Keyword.Rush) && !m.HasKeyword(Keyword.Storm))
            return "召唤晕眩中，下回合才能攻击";
        if (!m.CanAttackThisTurn) return "此随从本回合不能攻击";
        return null;
    }
}
