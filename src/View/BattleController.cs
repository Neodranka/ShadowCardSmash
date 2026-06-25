using System.Reflection;
using System.Threading.Tasks;
using Godot;
using ShadowCardSmash.App;
using ShadowCardSmash.Cards;
using ShadowCardSmash.Cards.Resources;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.View;

/// <summary>
/// Glue between GameLoop and BoardView. Owns the input state machine and re-renders after every event chain.
/// V1 runs hot seat (both players share one screen; LocalSide auto-tracks CurrentPlayer).
///
/// Event playback: after each Submit, the controller walks the slice of new BoardEvents and dispatches each to
/// an animation routine, awaiting Godot tweens between events. Final Rebind happens after the chain completes.
/// Input is locked while <see cref="_isAnimating"/> is true.
/// </summary>
public partial class BattleController : Node
{
    private GameLoop _loop = null!;
    private CardRegistry _registry = null!;
    private BoardView _board = null!;

    private enum Mode { Idle, AwaitPlayTarget, AwaitAttackTarget, AwaitEvolveTarget, AwaitChoice }
    private Mode _mode = Mode.Idle;
    private InstanceId _selectedHandInstance;
    private CardId _selectedCardId;
    private TargetSpec _selectedSpec;
    private InstanceId _selectedAttacker;

    // Multi-target / choice flow state.
    private readonly List<InstanceId> _pickedTargets = new();
    private int _targetsNeeded = 1;
    private int[]? _pickedChoices;
    /// <summary>Tile chosen for a minion/amulet that still needs to pick targets before submission.</summary>
    private int? _pendingTileIndex;

    private bool _isAnimating;
    private int _consumedEventCount;

    public PlayerSide LocalSide => _loop.State.CurrentPlayer; // hot-seat: always view current player

    public override void _Ready()
    {
        _registry = CardRegistry.ScanAssembly(Assembly.GetExecutingAssembly());
        var loaderResult = CardResourceLoader.AttachAll(_registry);
        GD.Print($"[Cards] Registered {_registry.Count} scripts, attached {loaderResult.Attached} .tres resources, {loaderResult.Missing} missing.");

        _board = new BoardView();
        AddChild(_board);

        _board.HandCardClicked += OnHandCardClicked;
        _board.TileClicked += OnTileClicked;
        _board.MinionClicked += OnMinionClicked;
        _board.HeroClicked += OnHeroClicked;
        _board.EndTurnClicked += OnEndTurnClicked;
        _board.PileClicked += OnPileClicked;
        _board.EvolveButtonClicked += OnEvolveButtonClicked;
        _board.HandStripClicked += OnHandStripClicked;

        StartHotSeatGame();
    }

    private (IReadOnlyList<CardId> Cards, HeroClass HeroClass) ResolveSeat(DeckResource? deck, HeroClass fallbackClass)
    {
        if (deck is not null && deck.CardIds.Length > 0)
        {
            var list = new List<CardId>(deck.CardIds.Length);
            foreach (var id in deck.CardIds) list.Add(new CardId(id));
            return (list, deck.HeroClassEnum);
        }
        // Fallback (no deck selected — should not happen via DeckSelection flow, but covers F5-on-Battle dev case).
        var demo = new List<CardId>();
        for (int i = 0; i < 20; i++) demo.Add(new CardId(2001));
        for (int i = 0; i < 20; i++) demo.Add(new CardId(1001));
        return (demo, fallbackClass);
    }

    private void StartHotSeatGame()
    {
        var state = new GameState();
        int seed = BattleSetup.Seed != 0 ? BattleSetup.Seed : (int)Time.GetTicksMsec();
        var rng = new DeterministicRng(seed, counter: 0);
        _loop = new GameLoop(state, _registry, rng);

        var (p1List, p1Class) = ResolveSeat(BattleSetup.Player1Deck, fallbackClass: HeroClass.Forsaken);
        var (p2List, p2Class) = ResolveSeat(BattleSetup.Player2Deck, fallbackClass: HeroClass.Forsaken);

        var first = new GameInitializer.SeatConfig(p1List, p1Class, null);
        var second = new GameInitializer.SeatConfig(p2List, p2Class, null);
        GameInitializer.Begin(_loop, seed: seed, first, second);

        _loop.Submit(new MulliganAction(PlayerSide.First, System.Array.Empty<int>()));
        _loop.Submit(new MulliganAction(PlayerSide.Second, System.Array.Empty<int>()));

        // Mark initial events as consumed so we don't animate game-setup retroactively.
        _consumedEventCount = _loop.EventLog.Count;
        Rebind();
    }

    private void OnHandCardClicked(int sideIndex, int instanceId)
    {
        if (_isAnimating) return;
        var side = (PlayerSide)sideIndex;
        if (side != LocalSide || _loop.IsGameOver) return;

        var card = _loop.State.GetPlayer(side).Hand.FirstOrDefault(c => c.Instance.Value == instanceId);
        if (card is null) return;
        var script = _registry.Get(card.Card);

        if (_mode == Mode.AwaitPlayTarget && _selectedHandInstance.Value == instanceId)
        {
            ResetMode();
            return;
        }

        ResetMode();
        _selectedHandInstance = card.Instance;
        _selectedCardId = card.Card;
        _selectedSpec = script.PlayTarget;

        if (script.CardType == CardType.Minion || script.CardType == CardType.Amulet)
        {
            _mode = Mode.AwaitPlayTarget;
            _board.SetStatus($"选择一个空格子放置 {script.Name}（右键/Esc 取消）");
            _board.PinDetail(card.Instance);
            HighlightFriendlyEmptyTiles();
            return;
        }

        if (script.CardType == CardType.Terrain)
        {
            // Auto-target: terrain has exactly one slot. Skip the "pick a tile" step.
            TrySubmit(new PlayCardAction(LocalSide, card.Instance, PlayerState.TerrainSlotIndex, null, null));
            return;
        }

        // Spell — three sub-cases: choice first, then target.
        if (script.Choices.Count > 0)
        {
            OpenChoicePopup(script);
            return;
        }

        if (_selectedSpec == TargetSpec.None)
        {
            SubmitPlayCard();
            return;
        }

        BeginSpellTargetPick(script.PlayTarget, script.TargetsToPick, script.Name, card.Instance);
    }

    private void OpenChoicePopup(ICardScript script)
    {
        _mode = Mode.AwaitChoice;
        var popup = new ChoicePopup();
        AddChild(popup);
        popup.Setup($"{script.Name} — 抉择 ({script.ChoicesToPick}/{script.Choices.Count})",
            script.Choices, script.ChoicesToPick);
        popup.ChoiceConfirmed += idx => OnChoiceConfirmed(script, idx);
        popup.Cancelled += () => ResetMode();
    }

    private void OnChoiceConfirmed(ICardScript script, int[] indices)
    {
        _pickedChoices = indices;
        // For ChoicesToPick=1 use the chosen option's target spec to drive the next step.
        // Future M-choose-N: combine target specs across selected choices.
        if (indices.Length == 0) { ResetMode(); return; }
        var chosen = script.Choices[indices[0]];
        if (chosen.TargetForChoice == TargetSpec.None)
        {
            SubmitPlayCard();
        }
        else
        {
            BeginSpellTargetPick(chosen.TargetForChoice, chosen.TargetsForChoice, script.Name, _selectedHandInstance);
        }
    }

    private void BeginSpellTargetPick(TargetSpec spec, int count, string cardName, InstanceId handInstance)
    {
        _selectedSpec = spec;
        _targetsNeeded = Math.Max(1, count);
        _pickedTargets.Clear();
        _mode = Mode.AwaitPlayTarget;
        _board.PinDetail(handInstance);
        RefreshTargetStatus(cardName);
    }

    private void RefreshTargetStatus(string cardName)
    {
        _board.SetStatus($"为 {cardName} 选择目标 ({_pickedTargets.Count}/{_targetsNeeded})（右键/Esc 取消）");
    }

    private void SubmitPlayCard()
    {
        InstanceId? primary = _pickedTargets.Count > 0 ? _pickedTargets[0] : null;
        InstanceId[]? extras = _pickedTargets.Count > 1 ? _pickedTargets.Skip(1).ToArray() : null;
        TrySubmit(new PlayCardAction(LocalSide, _selectedHandInstance,
            TileIndex: _pendingTileIndex,    // null for spells, set for minion/amulet that needed targeting
            TargetMinion: primary,
            TargetPlayer: null,
            ExtraTargets: extras,
            ChoiceIndices: _pickedChoices));
    }

    private void OnTileClicked(int sideIndex, int tileIndex)
    {
        if (_isAnimating || _loop.IsGameOver) return;
        var side = (PlayerSide)sideIndex;

        if (_mode == Mode.AwaitPlayTarget)
        {
            var script = _registry.Get(_selectedCardId);
            switch (script.CardType)
            {
                case CardType.Minion:
                case CardType.Amulet:
                    if (side != LocalSide) { _board.SetStatus("必须放在己方场地"); return; }
                    if (tileIndex == PlayerState.TerrainSlotIndex)
                    {
                        _board.SetStatus($"{script.Name} 不能放在场地槽位");
                        return;
                    }
                    if (script.PlayTarget != TargetSpec.None && _pendingTileIndex is null)
                    {
                        // 还需要选 target：先把 tile 存起来，进入"等待目标"子阶段。
                        _pendingTileIndex = tileIndex;
                        _targetsNeeded = Math.Max(1, script.TargetsToPick);
                        _pickedTargets.Clear();
                        _board.SetStatus($"为 {script.Name} 选择目标 (0/{_targetsNeeded})（右键/Esc 取消）");
                        return;
                    }
                    TrySubmit(new PlayCardAction(LocalSide, _selectedHandInstance, tileIndex, null, null));
                    return;
                case CardType.Spell:
                    // V1: spells don't target the terrain slot (terrain isn't a regular minion).
                    if (tileIndex == PlayerState.TerrainSlotIndex) return;
                    if (_loop.State.GetPlayer(side).Field[tileIndex].Occupant is { } occ)
                        TrySubmit(new PlayCardAction(LocalSide, _selectedHandInstance, null, occ.Instance, null));
                    return;
            }
        }
        ResetMode();
    }

    private void OnMinionClicked(int sideIndex, int instanceId)
    {
        if (_isAnimating || _loop.IsGameOver) return;
        var side = (PlayerSide)sideIndex;
        var instance = new InstanceId(instanceId);

        if (_mode == Mode.AwaitPlayTarget)
        {
            var script = _registry.Get(_selectedCardId);
            bool isMinionAwaitingTarget = (script.CardType == CardType.Minion || script.CardType == CardType.Amulet)
                                           && _pendingTileIndex.HasValue;
            if (script.CardType == CardType.Spell || isMinionAwaitingTarget)
            {
                if (_pickedTargets.Contains(instance)) return; // ignore duplicate
                _pickedTargets.Add(instance);
                if (_pickedTargets.Count >= _targetsNeeded)
                    SubmitPlayCard();
                else
                    RefreshTargetStatus(script.Name);
            }
            return;
        }

        if (_mode == Mode.AwaitAttackTarget)
        {
            if (side == LocalSide.Opponent())
                TrySubmit(new AttackAction(LocalSide, _selectedAttacker, instance, null));
            else
                ResetMode();
            return;
        }

        // Evolve target — must be a friendly minion.
        if (_mode == Mode.AwaitEvolveTarget)
        {
            if (side != LocalSide) { _board.SetStatus("只能进化己方随从"); return; }
            var target = _loop.State.GetPlayer(side).FindOnField(instance);
            if (target is null || target.IsEvolved) { _board.SetStatus("无法进化此随从"); return; }
            TrySubmit(new EvolveAction(LocalSide, instance));
            return;
        }

        if (side == LocalSide)
        {
            var attacker = _loop.State.GetPlayer(side).FindOnField(instance);
            if (attacker is null) return;

            string? blocker = DiagnoseAttackBlocker(attacker);
            if (blocker is not null) { _board.SetStatus(blocker); return; }

            _selectedAttacker = instance;
            _mode = Mode.AwaitAttackTarget;
            _board.SetStatus((CombatResolver.EnemyHasWard(_loop.State, LocalSide)
                ? "对方有守护，必须先攻击守护随从"
                : "选择攻击目标（敌方随从或英雄）") + "  右键/Esc 取消");
            _board.PinDetail(instance);
        }
    }

    private void OnHeroClicked(int sideIndex)
    {
        if (_isAnimating || _loop.IsGameOver) return;
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
        if (_isAnimating || _loop.IsGameOver) return;
        if (_loop.State.Phase != GamePhase.Main) return;
        ResetMode();
        TrySubmit(new EndTurnAction(LocalSide));
    }

    private void OnHandStripClicked(int sideIndex)
    {
        if (_isAnimating || _loop.IsGameOver) return;
        var side = (PlayerSide)sideIndex;
        if (side != LocalSide) return;
        // If we're already in some selection mode, ignore (player should finish or cancel first).
        if (_mode != Mode.Idle) return;
        if (IsHandPopupOpen()) return;

        var popup = new HandPopup();
        AddChild(popup);
        var me = _loop.State.GetPlayer(LocalSide);
        popup.Populate(me.Hand, _registry, me.Mana, LocalSide);
        _board.MyHand.Visible = false;
        popup.CardSelected += iid =>
        {
            // Route through the existing play-card pipeline, then close the popup.
            popup.QueueFree();
            _board.MyHand.Visible = true;
            OnHandCardClicked((int)LocalSide, iid);
        };
        popup.CardHovered += iid => _board.HoverDetailForId(iid);
        popup.CardHoverExited += _ => _board.DetailPanel.HoverHide();
        popup.Cancelled += () =>
        {
            _board.MyHand.Visible = true;
        };
    }

    private bool IsHandPopupOpen()
    {
        foreach (var c in GetChildren())
            if (c is HandPopup) return true;
        return false;
    }

    private void OnEvolveButtonClicked(int sideIndex)
    {
        if (_isAnimating || _loop.IsGameOver) return;
        var side = (PlayerSide)sideIndex;
        if (side != LocalSide) return;
        if (!EvolutionSystem.CanManuallyEvolve(_loop.State, LocalSide))
        {
            _board.SetStatus("EP 不足或本回合已进化过");
            return;
        }
        // Toggle off when already in evolve mode.
        if (_mode == Mode.AwaitEvolveTarget) { ResetMode(); return; }

        ResetMode();
        _mode = Mode.AwaitEvolveTarget;
        _board.MyEvolveButton.SetActive(true);
        _board.SetStatus("选择一个我方随从进化（右键/Esc 取消）");
    }

    private void OnPileClicked(int sideIndex, int kindIndex)
    {
        if (_isAnimating) return;
        var side = (PlayerSide)sideIndex;
        var kind = (PileView.Kind)kindIndex;
        var p = _loop.State.GetPlayer(side);

        IReadOnlyList<RuntimeCard> cards;
        string title;
        switch (kind)
        {
            case PileView.Kind.Deck:
                cards = p.Deck
                    .OrderBy(c => _registry.Get(c.Card).Cost)
                    .ThenBy(c => c.Card.Value)
                    .ToArray();
                title = side == LocalSide ? $"我方牌库（{p.Deck.Count}）" : $"对方牌库（{p.Deck.Count}）";
                break;
            case PileView.Kind.Banish:
                cards = p.Vanished.ToArray();
                title = side == LocalSide ? $"我方放逐区（{p.Vanished.Count}）" : $"对方放逐区（{p.Vanished.Count}）";
                break;
            default: // Graveyard
                cards = p.Graveyard.ToArray();
                title = side == LocalSide ? $"我方墓地（{p.Graveyard.Count}）" : $"对方墓地（{p.Graveyard.Count}）";
                break;
        }

        var popup = new PilePopup();
        AddChild(popup);
        popup.Populate(title, cards, _registry);
        popup.CardHovered += iid => _board.HoverDetailForId(iid);
        popup.CardHoverExited += () => _board.DetailPanel.HoverHide();
        popup.Closed += () => _board.DetailPanel.HoverHide();
    }

    /// <summary>
    /// Fire-and-forget async dispatch: submit the action, animate the new events, then rebind.
    /// Marked async void because it's an input-handler trampoline; exceptions are converted to status messages.
    /// </summary>
    private async void TrySubmit(IGameAction action)
    {
        _isAnimating = true;
        try
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

            await PlayPendingEventsAsync();
            ResetMode();
            Rebind();
        }
        finally
        {
            _isAnimating = false;
        }
    }

    private async Task PlayPendingEventsAsync()
    {
        var log = _loop.EventLog;
        while (_consumedEventCount < log.Count)
        {
            var evt = log[_consumedEventCount];
            _consumedEventCount++;
            await PlayAnimationFor(evt);
        }
    }

    private async Task PlayAnimationFor(BoardEvent evt)
    {
        switch (evt)
        {
            case CardDrawnEvent drawn:
                await PlayDrawAnim(drawn);
                break;
            case MinionAttacksEvent atk:
                await PlayAttackLunge(atk);
                break;
            case MinionDamagedEvent md when md.Amount > 0:
                _board.SpawnDamageNumber(md.Target, md.Amount);
                await SmallDelay(0.15);
                break;
            case MinionHealedEvent mh when mh.Amount > 0:
                _board.SpawnHealNumber(mh.Target, mh.Amount);
                await SmallDelay(0.15);
                break;
            case PlayerDamagedEvent pd when pd.Amount > 0:
                _board.SpawnPlayerDamageNumber(pd.Target, pd.Amount);
                await SmallDelay(0.20);
                break;
            case PlayerHealedEvent ph when ph.Amount > 0:
                _board.SpawnPlayerHealNumber(ph.Target, ph.Amount);
                await SmallDelay(0.20);
                break;
            case MinionDestroyedEvent destroyed:
                await PlaySlideToGraveyard(destroyed);
                break;
        }
    }

    private async Task PlayDrawAnim(CardDrawnEvent evt)
    {
        var deckPile = _board.GetDeckPile(evt.Side);
        Control handTarget = evt.Side == LocalSide ? (Control)_board.MyHand : _board.EnemyHand;
        var cardSize = new Vector2(CardView.CardWidth, CardView.CardHeight);
        Vector2 start = deckPile.GlobalPosition + deckPile.Size / 2 - cardSize / 2;
        Vector2 end = handTarget.GlobalPosition + handTarget.Size / 2 - cardSize / 2;

        var ghost = new CardView();
        _board.AddChild(ghost);
        ghost.BindFaceDown();
        ghost.SetSize(cardSize);
        ghost.GlobalPosition = start;
        ghost.ZIndex = 50;

        var tween = ghost.CreateTween();
        tween.TweenProperty(ghost, "global_position", end, 0.32)
             .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(ghost, "scale", new Vector2(0.85f, 0.85f), 0.32);
        tween.TweenProperty(ghost, "modulate:a", 0.0f, 0.10);
        tween.Chain().TweenCallback(Callable.From(ghost.QueueFree));
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async Task PlaySlideToGraveyard(MinionDestroyedEvent evt)
    {
        var cv = _board.GetFieldCardView(evt.Instance);
        if (cv is null) return;

        var grave = _board.GetGravePile(evt.Owner);
        Vector2 endCenter = grave.GlobalPosition + grave.Size / 2;
        Vector2 endTopLeft = endCenter - cv.Size / 2;

        cv.ZIndex = 50;
        var tween = cv.CreateTween().SetParallel();
        tween.TweenProperty(cv, "global_position", endTopLeft, 0.35)
             .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
        tween.TweenProperty(cv, "scale", new Vector2(0.45f, 0.45f), 0.35);
        tween.TweenProperty(cv, "modulate:a", 0.0f, 0.35);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async Task PlayAttackLunge(MinionAttacksEvent atk)
    {
        var attackerCv = _board.GetFieldCardView(atk.Attacker);
        if (attackerCv is null) return;

        Vector2 targetCenter;
        if (atk.TargetMinion.HasValue)
        {
            var targetCv = _board.GetFieldCardView(atk.TargetMinion.Value);
            if (targetCv is null) return;
            targetCenter = targetCv.GlobalPosition + targetCv.Size / 2;
        }
        else if (atk.TargetPlayer.HasValue)
        {
            var panel = _board.GetHeroPanel(atk.TargetPlayer.Value);
            targetCenter = panel.GlobalPosition + panel.Size / 2;
        }
        else return;

        var origin = attackerCv.Position;
        var attackerCenter = attackerCv.GlobalPosition + attackerCv.Size / 2;
        var direction = (targetCenter - attackerCenter).Normalized();
        var lungeTarget = origin + direction * 50;

        var tween = attackerCv.CreateTween();
        tween.TweenProperty(attackerCv, "position", lungeTarget, 0.13)
             .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(attackerCv, "position", origin, 0.18)
             .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async Task SmallDelay(double seconds)
        => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

    private void Rebind() => _board.Rebind(_loop.State, _registry, LocalSide);

    private void ResetMode()
    {
        _mode = Mode.Idle;
        _selectedHandInstance = InstanceId.None;
        _selectedAttacker = InstanceId.None;
        _pickedTargets.Clear();
        _targetsNeeded = 1;
        _pickedChoices = null;
        _pendingTileIndex = null;
        _board.SetStatus("");
        _board.ClearHighlights();
        _board.UnpinDetail();
        if (_board.MyEvolveButton is not null) _board.MyEvolveButton.SetActive(false);
        // Close any open choice popup or hand popup.
        foreach (var c in GetChildren())
        {
            if (c is ChoicePopup cp) cp.QueueFree();
            else if (c is HandPopup hp) { hp.QueueFree(); _board.MyHand.Visible = true; }
        }
    }

    /// <summary>
    /// Listen at the top-level _Input pipeline (fires before any Control consumes mouse events). Without this,
    /// right-clicks on a CardView (whose MouseFilter is Stop by default) would never reach _UnhandledInput.
    /// </summary>
    public override void _Input(InputEvent @event)
    {
        if (_isAnimating || _loop is null) return;
        if (_mode == Mode.Idle) return;
        if (IsPopupOpen()) return; // let the popup own right-click while it is up.

        bool cancel =
            (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Right)
            || (@event is InputEventKey kev && kev.Pressed && kev.Keycode == Key.Escape);

        if (cancel)
        {
            ResetMode();
            GetViewport().SetInputAsHandled();
        }
    }

    private bool IsPopupOpen()
    {
        foreach (var c in GetChildren())
            if (c is PilePopup) return true;
        return false;
    }

    private void HighlightFriendlyEmptyTiles()
    {
        var tiles = _board.MyTiles;
        var p = _loop.State.GetPlayer(LocalSide);
        for (int i = 0; i < tiles.Length; i++) tiles[i].Highlight(p.Field[i].IsEmpty);
    }

    private static string? DiagnoseAttackBlocker(RuntimeCard m)
    {
        // 0 攻击力允许出招（伤害为 0 仍消耗屏障）。
        if (m.AttacksThisTurn > 0) return "本回合已经攻击过";
        if (m.SummonedThisTurn && !m.HasKeyword(Keyword.Rush) && !m.HasKeyword(Keyword.Storm))
            return "召唤晕眩中，下回合才能攻击";
        if (!m.CanAttackThisTurn) return "此随从本回合不能攻击";
        return null;
    }
}
