using Godot;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.View;

/// <summary>
/// Visualizes the GameState, top-down: enemy panel, enemy hand, enemy field, my field, my hand, my panel.
/// All it does is read state and rebuild child views; never writes state.
/// The BattleController owns this BoardView, wires its signals to actions, and rebinds after every event chain.
///
/// The two slot rows and two hands are *physical* (top/bottom) — their `Side` is rewritten on every Rebind to match
/// the active hot-seat perspective, so events report the correct side regardless of which pane the user clicked.
/// </summary>
public partial class BoardView : Control
{
    public const int Margin = 16;
    public const int RowSeparation = 4;

    [Signal] public delegate void HandCardClickedEventHandler(int sideIndex, int instanceId);
    [Signal] public delegate void TileClickedEventHandler(int sideIndex, int tileIndex);
    [Signal] public delegate void MinionClickedEventHandler(int sideIndex, int instanceId);
    [Signal] public delegate void HeroClickedEventHandler(int sideIndex);
    [Signal] public delegate void EndTurnClickedEventHandler();

    public PlayerInfoPanel EnemyInfo = null!;
    public HandView EnemyHand = null!;
    public TileSlotView[] EnemyTiles = null!;
    public TileSlotView[] MyTiles = null!;
    public HandView MyHand = null!;
    public PlayerInfoPanel MyInfo = null!;
    public Button EndTurnButton = null!;
    public Label StatusLabel = null!;
    public CardDetailPanel DetailPanel = null!;

    public PileView EnemyDeck = null!;
    public PileView EnemyGrave = null!;
    public PileView EnemyTopLeftSlot = null!;
    public PileView EnemyTopRightSlot = null!;
    public PileView MyDeck = null!;
    public PileView MyGrave = null!;
    public PileView MyTopLeftSlot = null!;
    public PileView MyTopRightSlot = null!;

    [Signal] public delegate void PileClickedEventHandler(int sideIndex, int kindIndex);

    /// <summary>Maps InstanceId → on-field CardView so animation code can locate a card by id.</summary>
    private readonly Dictionary<InstanceId, CardView> _fieldCardLookup = new();

    private GameState? _lastState;
    private ICardDatabase? _lastDb;

    private bool _builtUi;

    public override void _Ready()
    {
        AnchorLeft = 0; AnchorTop = 0; AnchorRight = 1; AnchorBottom = 1;
        BuildUi();
    }

    private void BuildUi()
    {
        if (_builtUi) return;
        _builtUi = true;

        // Inner VBox spans the full viewport. Pile flanks now live INSIDE each FieldRow, so no side gutter is needed.
        var root = new VBoxContainer
        {
            AnchorLeft = 0, AnchorTop = 0, AnchorRight = 1, AnchorBottom = 1,
            OffsetLeft = Margin, OffsetTop = Margin,
            OffsetRight = -Margin, OffsetBottom = -Margin,
        };
        root.AddThemeConstantOverride("separation", RowSeparation);
        AddChild(root);

        EnemyInfo = new PlayerInfoPanel { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        EnemyInfo.HeroClicked += idx => EmitSignal(SignalName.HeroClicked, idx);
        root.AddChild(EnemyInfo);

        EnemyHand = new HandView { ShowFaces = false };
        EnemyHand.CardSelected += iid => EmitSignal(SignalName.HandCardClicked, (int)EnemyHand.Side, iid);
        root.AddChild(EnemyHand);

        // Enemy field row, with mirrored pile flanks (top-left=grave, top-right=deck).
        EnemyTiles = BuildFieldRowWithFlanks(root,
            out EnemyTopLeftSlot, out EnemyGrave,
            out EnemyTopRightSlot, out EnemyDeck);

        var sep = new HSeparator();
        sep.AddThemeConstantOverride("separation", 12);
        root.AddChild(sep);

        // My field row, mirrored: bottom-left=deck, bottom-right=grave.
        MyTiles = BuildFieldRowWithFlanks(root,
            out MyTopLeftSlot, out MyDeck,
            out MyTopRightSlot, out MyGrave);

        MyHand = new HandView { ShowFaces = true };
        MyHand.CardSelected += iid => EmitSignal(SignalName.HandCardClicked, (int)MyHand.Side, iid);
        MyHand.CardHovered += OnHandCardHovered;
        MyHand.CardHoverExited += _ => DetailPanel.HoverHide();
        root.AddChild(MyHand);

        // End-Turn row: right-aligned strip between MyHand and MyInfo.
        var endTurnRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        root.AddChild(endTurnRow);
        endTurnRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        EndTurnButton = new Button { Text = "结束回合", CustomMinimumSize = new Vector2(160, 56) };
        EndTurnButton.Pressed += () => EmitSignal(SignalName.EndTurnClicked);
        endTurnRow.AddChild(EndTurnButton);

        MyInfo = new PlayerInfoPanel { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        MyInfo.HeroClicked += idx => EmitSignal(SignalName.HeroClicked, idx);
        root.AddChild(MyInfo);

        // Status overlay — center of viewport.
        StatusLabel = new Label
        {
            AnchorLeft = 0.5f, AnchorTop = 0.5f, AnchorRight = 0.5f, AnchorBottom = 0.5f,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        StatusLabel.AddThemeFontSizeOverride("font_size", 28);
        StatusLabel.Modulate = new Color(1f, 0.95f, 0.5f);
        AddChild(StatusLabel);

        // Card detail side panel (hidden until hover or pin). TopLevel + absolute Z keeps it above piles and popups.
        DetailPanel = new CardDetailPanel();
        AddChild(DetailPanel);
    }

    /// <summary>
    /// One field row: left pile column + spacer + 6 tile slots + spacer + right pile column.
    /// Spacers (ExpandFill) push the pile columns to the screen edges; tiles stay centered in the middle.
    /// Because the row sits inside the inner VBox, pile columns auto-align with the tile row Y.
    /// </summary>
    private TileSlotView[] BuildFieldRowWithFlanks(
        Container parent,
        out PileView leftUpper, out PileView leftLower,
        out PileView rightUpper, out PileView rightLower)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 8);
        parent.AddChild(row);

        // Left flank column.
        leftUpper = MakePileSlot();
        leftLower = MakePileSlot();
        var leftCol = new VBoxContainer();
        leftCol.AddThemeConstantOverride("separation", 4);
        leftCol.AddChild(leftUpper);
        leftCol.AddChild(leftLower);
        row.AddChild(leftCol);

        // Left spacer pushes left flank to the screen edge.
        row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        // Six field tiles, centered between the spacers.
        var tilesRow = new HBoxContainer();
        tilesRow.AddThemeConstantOverride("separation", 8);
        row.AddChild(tilesRow);
        var tiles = new TileSlotView[PlayerState.FieldSize];
        for (int i = 0; i < PlayerState.FieldSize; i++)
        {
            var tile = new TileSlotView { TileIndex = i };
            tile.TileClicked += idx => EmitSignal(SignalName.TileClicked, (int)tile.Side, idx);
            tilesRow.AddChild(tile);
            tiles[i] = tile;
        }

        // Right spacer pushes right flank to the screen edge.
        row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        // Right flank column.
        rightUpper = MakePileSlot();
        rightLower = MakePileSlot();
        var rightCol = new VBoxContainer();
        rightCol.AddThemeConstantOverride("separation", 4);
        rightCol.AddChild(rightUpper);
        rightCol.AddChild(rightLower);
        row.AddChild(rightCol);

        return tiles;
    }

    private PileView MakePileSlot()
    {
        var p = new PileView { SizeFlagsVertical = SizeFlags.ExpandFill };
        p.Clicked += (sideIdx, kindIdx) => EmitSignal(SignalName.PileClicked, sideIdx, kindIdx);
        return p;
    }

    public void Rebind(GameState state, ICardDatabase db, PlayerSide localSide)
    {
        BuildUi();
        _lastState = state;
        _lastDb = db;

        var me = state.GetPlayer(localSide);
        var enemy = state.GetPlayer(localSide.Opponent());
        bool myTurn = state.CurrentPlayer == localSide;

        // Re-tag panes with the current perspective so clicks report the right side.
        MyInfo.Side = localSide;
        EnemyInfo.Side = localSide.Opponent();
        MyHand.Side = localSide;
        EnemyHand.Side = localSide.Opponent();
        foreach (var t in MyTiles) t.Side = localSide;
        foreach (var t in EnemyTiles) t.Side = localSide.Opponent();

        MyDeck.Side = localSide; MyGrave.Side = localSide;
        MyTopLeftSlot.Side = localSide; MyTopRightSlot.Side = localSide;
        EnemyDeck.Side = localSide.Opponent(); EnemyGrave.Side = localSide.Opponent();
        EnemyTopLeftSlot.Side = localSide.Opponent(); EnemyTopRightSlot.Side = localSide.Opponent();

        MyDeck.BindDeck(me.Deck.Count);
        MyGrave.BindGraveyard(me.Graveyard.Count);
        MyTopLeftSlot.BindPlaceholder();
        MyTopRightSlot.BindPlaceholder();
        EnemyDeck.BindDeck(enemy.Deck.Count);
        EnemyGrave.BindGraveyard(enemy.Graveyard.Count);
        EnemyTopLeftSlot.BindPlaceholder();
        EnemyTopRightSlot.BindPlaceholder();

        MyInfo.Rebind(me, myTurn);
        EnemyInfo.Rebind(enemy, !myTurn);

        MyHand.Rebind(me.Hand, db);
        EnemyHand.Rebind(enemy.Hand, db);

        _fieldCardLookup.Clear();
        RebindRow(MyTiles, me, db, OnMinionClicked, _fieldCardLookup);
        RebindRow(EnemyTiles, enemy, db, OnMinionClicked, _fieldCardLookup);

        EndTurnButton.Disabled = !myTurn || state.Phase != GamePhase.Main;
        StatusLabel.Text = state.Phase switch
        {
            GamePhase.Mulligan => "Mulligan",
            GamePhase.GameOver => state.Result switch
            {
                GameResult.FirstWins => localSide == PlayerSide.First ? "VICTORY" : "DEFEAT",
                GameResult.SecondWins => localSide == PlayerSide.Second ? "VICTORY" : "DEFEAT",
                GameResult.Draw => "DRAW",
                _ => "",
            },
            _ => "",
        };
    }

    private void RebindRow(TileSlotView[] tiles, PlayerState p, ICardDatabase db,
        System.Action<TileSlotView, int> onMinionClicked,
        Dictionary<InstanceId, CardView> lookup)
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i].Highlight(false);
            var occ = p.Field[i].Occupant;
            if (occ is null) { tiles[i].SetOccupant(null); continue; }
            var cv = new CardView();
            tiles[i].SetOccupant(cv);
            cv.Bind(occ, db.Get(occ.Card), onField: true);
            var capturedTile = tiles[i];
            cv.Clicked += iid => onMinionClicked(capturedTile, iid);
            cv.HoverEntered += iid => OnFieldCardHovered(iid);
            cv.HoverExited += _ => DetailPanel.HoverHide();
            lookup[occ.Instance] = cv;
        }
    }

    private void OnHandCardHovered(int instanceId)
    {
        if (_lastState is null || _lastDb is null) return;
        var iid = new InstanceId(instanceId);
        foreach (var p in _lastState.Players)
        {
            var card = p.Hand.FirstOrDefault(c => c.Instance == iid);
            if (card is not null)
            {
                DetailPanel.ShowFor(card, _lastDb.Get(card.Card), onField: false, pin: false);
                return;
            }
        }
    }

    private void OnFieldCardHovered(int instanceId)
    {
        if (_lastState is null || _lastDb is null) return;
        var iid = new InstanceId(instanceId);
        foreach (var p in _lastState.Players)
        {
            foreach (var t in p.Field)
            {
                if (t.Occupant is { } occ && occ.Instance == iid)
                {
                    DetailPanel.ShowFor(occ, _lastDb.Get(occ.Card), onField: true, pin: false);
                    return;
                }
            }
        }
    }

    public void PinDetail(InstanceId id)
    {
        if (_lastState is null || _lastDb is null) return;
        foreach (var p in _lastState.Players)
        {
            var hc = p.Hand.FirstOrDefault(c => c.Instance == id);
            if (hc is not null) { DetailPanel.ShowFor(hc, _lastDb.Get(hc.Card), onField: false, pin: true); return; }
            var fc = p.FindOnField(id);
            if (fc is not null) { DetailPanel.ShowFor(fc, _lastDb.Get(fc.Card), onField: true, pin: true); return; }
        }
    }

    public void UnpinDetail() => DetailPanel.Unpin();

    /// <summary>
    /// Hover-show detail for any card by id, searching across hand/field/deck/graveyard. Used by PilePopup.
    /// </summary>
    public void HoverDetailForId(int instanceId)
    {
        if (_lastState is null || _lastDb is null) return;
        var iid = new InstanceId(instanceId);
        foreach (var p in _lastState.Players)
        {
            var hc = p.Hand.FirstOrDefault(c => c.Instance == iid);
            if (hc is not null) { DetailPanel.ShowFor(hc, _lastDb.Get(hc.Card), onField: false, pin: false); return; }
            var fc = p.FindOnField(iid);
            if (fc is not null) { DetailPanel.ShowFor(fc, _lastDb.Get(fc.Card), onField: true, pin: false); return; }
            var dc = p.Deck.FirstOrDefault(c => c.Instance == iid);
            if (dc is not null) { DetailPanel.ShowFor(dc, _lastDb.Get(dc.Card), onField: false, pin: false); return; }
            var gc = p.Graveyard.FirstOrDefault(c => c.Instance == iid);
            if (gc is not null) { DetailPanel.ShowFor(gc, _lastDb.Get(gc.Card), onField: false, pin: false); return; }
        }
    }

    public PileView GetDeckPile(PlayerSide side) => MyDeck.Side == side ? MyDeck : EnemyDeck;
    public PileView GetGravePile(PlayerSide side) => MyGrave.Side == side ? MyGrave : EnemyGrave;

    public CardView? GetFieldCardView(InstanceId id)
        => _fieldCardLookup.TryGetValue(id, out var cv) ? cv : null;

    public PlayerInfoPanel GetHeroPanel(PlayerSide side)
        => MyInfo.Side == side ? MyInfo : EnemyInfo;

    public void SpawnDamageNumber(InstanceId target, int amount)
    {
        if (amount <= 0) return;
        var cv = GetFieldCardView(target);
        if (cv is null) return;
        var pos = cv.GlobalPosition + cv.Size / 2;
        DamageNumber.Spawn(this, pos, $"-{amount}", new Color(1f, 0.4f, 0.4f));
    }

    public void SpawnHealNumber(InstanceId target, int amount)
    {
        if (amount <= 0) return;
        var cv = GetFieldCardView(target);
        if (cv is null) return;
        var pos = cv.GlobalPosition + cv.Size / 2;
        DamageNumber.Spawn(this, pos, $"+{amount}", new Color(0.5f, 1f, 0.6f));
    }

    public void SpawnPlayerDamageNumber(PlayerSide side, int amount)
    {
        if (amount <= 0) return;
        var panel = GetHeroPanel(side);
        var pos = panel.GlobalPosition + panel.Size / 2;
        DamageNumber.Spawn(this, pos, $"-{amount}", new Color(1f, 0.4f, 0.4f), fontSize: 44);
    }

    public void SpawnPlayerHealNumber(PlayerSide side, int amount)
    {
        if (amount <= 0) return;
        var panel = GetHeroPanel(side);
        var pos = panel.GlobalPosition + panel.Size / 2;
        DamageNumber.Spawn(this, pos, $"+{amount}", new Color(0.5f, 1f, 0.6f), fontSize: 44);
    }

    private void OnMinionClicked(TileSlotView tile, int instanceId)
        => EmitSignal(SignalName.MinionClicked, (int)tile.Side, instanceId);

    public void SetStatus(string msg) => StatusLabel.Text = msg;

    public void HighlightTiles(PlayerSide side, System.Func<int, bool> shouldHighlight)
    {
        var row = side == MyTiles[0].Side ? MyTiles : EnemyTiles;
        for (int i = 0; i < row.Length; i++) row[i].Highlight(shouldHighlight(i));
    }

    public void ClearHighlights()
    {
        foreach (var t in MyTiles) t.Highlight(false);
        foreach (var t in EnemyTiles) t.Highlight(false);
    }
}
