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
    public const int EdgeColumnWidth = 156;
    public const int EdgeInfoGap = 90;   // vertical space reserved for info bar + margin
    public const int CenterGap = 32;     // gap between pile column edge and the central separator

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

        // Inner VBox holds the rows. Horizontal offsets leave room for the edge-anchored pile columns.
        int sideGutter = Margin + EdgeColumnWidth + 12;
        var root = new VBoxContainer
        {
            AnchorLeft = 0, AnchorTop = 0, AnchorRight = 1, AnchorBottom = 1,
            OffsetLeft = sideGutter, OffsetTop = Margin,
            OffsetRight = -sideGutter, OffsetBottom = -Margin,
        };
        root.AddThemeConstantOverride("separation", RowSeparation);
        AddChild(root);

        EnemyInfo = new PlayerInfoPanel { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        EnemyInfo.HeroClicked += idx => EmitSignal(SignalName.HeroClicked, idx);
        root.AddChild(EnemyInfo);

        EnemyHand = new HandView { ShowFaces = false };
        EnemyHand.CardSelected += iid => EmitSignal(SignalName.HandCardClicked, (int)EnemyHand.Side, iid);
        root.AddChild(EnemyHand);

        EnemyTiles = BuildFieldRow(root);

        var sep = new HSeparator();
        sep.AddThemeConstantOverride("separation", 12);
        root.AddChild(sep);

        MyTiles = BuildFieldRow(root);

        MyHand = new HandView { ShowFaces = true };
        MyHand.CardSelected += iid => EmitSignal(SignalName.HandCardClicked, (int)MyHand.Side, iid);
        MyHand.CardHovered += OnHandCardHovered;
        MyHand.CardHoverExited += _ => DetailPanel.HoverHide();
        root.AddChild(MyHand);

        MyInfo = new PlayerInfoPanel { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        MyInfo.HeroClicked += idx => EmitSignal(SignalName.HeroClicked, idx);
        root.AddChild(MyInfo);

        // End-Turn floats over the bottom-right corner so it never breaks the mirror symmetry above.
        EndTurnButton = new Button
        {
            Text = "结束回合",
            CustomMinimumSize = new Vector2(140, 56),
            AnchorLeft = 1, AnchorTop = 1, AnchorRight = 1, AnchorBottom = 1,
            OffsetLeft = -156, OffsetTop = -72, OffsetRight = -16, OffsetBottom = -16,
        };
        EndTurnButton.Pressed += () => EmitSignal(SignalName.EndTurnClicked);
        AddChild(EndTurnButton);

        // Status overlay — center of viewport.
        StatusLabel = new Label
        {
            AnchorLeft = 0.5f, AnchorTop = 0.5f, AnchorRight = 0.5f, AnchorBottom = 0.5f,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        StatusLabel.AddThemeFontSizeOverride("font_size", 28);
        StatusLabel.Modulate = new Color(1f, 0.95f, 0.5f);
        AddChild(StatusLabel);

        // Card detail side panel (hidden until hover or pin).
        DetailPanel = new CardDetailPanel();
        AddChild(DetailPanel);

        // Pile columns anchored to the four screen corners. Mirror layout: enemy deck/grave swap left↔right
        // so my-deck (BL) sits diagonally across from enemy-deck (TR), and my-grave (BR) from enemy-grave (TL).
        EnemyTopLeftSlot = MakePileSlot();
        EnemyGrave = MakePileSlot();
        AddEdgePileColumn(EnemyTopLeftSlot, EnemyGrave, leftSide: true, topHalf: true);

        EnemyTopRightSlot = MakePileSlot();
        EnemyDeck = MakePileSlot();
        AddEdgePileColumn(EnemyTopRightSlot, EnemyDeck, leftSide: false, topHalf: true);

        MyTopLeftSlot = MakePileSlot();
        MyDeck = MakePileSlot();
        AddEdgePileColumn(MyTopLeftSlot, MyDeck, leftSide: true, topHalf: false);

        MyTopRightSlot = MakePileSlot();
        MyGrave = MakePileSlot();
        AddEdgePileColumn(MyTopRightSlot, MyGrave, leftSide: false, topHalf: false);
    }

    private TileSlotView[] BuildFieldRow(Container parent)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        row.AddThemeConstantOverride("separation", 12);
        parent.AddChild(row);

        var tiles = new TileSlotView[PlayerState.FieldSize];
        for (int i = 0; i < PlayerState.FieldSize; i++)
        {
            var tile = new TileSlotView { TileIndex = i };
            tile.TileClicked += idx => EmitSignal(SignalName.TileClicked, (int)tile.Side, idx);
            row.AddChild(tile);
            tiles[i] = tile;
        }
        return tiles;
    }

    private PileView MakePileSlot()
    {
        var p = new PileView { SizeFlagsVertical = SizeFlags.ExpandFill };
        p.Clicked += (sideIdx, kindIdx) => EmitSignal(SignalName.PileClicked, sideIdx, kindIdx);
        return p;
    }

    /// <summary>
    /// Anchor a column of two PileViews to a screen corner. The column spans the player's tile + hand area:
    /// for the top half it goes from below the enemy info bar to just above the central separator;
    /// for the bottom half it mirrors below the separator down to above the my-info bar.
    /// </summary>
    private void AddEdgePileColumn(PileView upper, PileView lower, bool leftSide, bool topHalf)
    {
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 6);

        col.AnchorLeft = leftSide ? 0 : 1;
        col.AnchorRight = leftSide ? 0 : 1;
        col.AnchorTop = topHalf ? 0 : 0.5f;
        col.AnchorBottom = topHalf ? 0.5f : 1;

        if (leftSide)
        {
            col.OffsetLeft = Margin;
            col.OffsetRight = Margin + EdgeColumnWidth;
        }
        else
        {
            col.OffsetLeft = -(Margin + EdgeColumnWidth);
            col.OffsetRight = -Margin;
        }

        if (topHalf)
        {
            // Keep the 20-ish gap below EnemyInfo; lift the bottom edge up so a CenterGap forms above the divider.
            col.OffsetTop = Margin + EdgeInfoGap;
            col.OffsetBottom = -CenterGap;
        }
        else
        {
            // Keep the bottom edge's relative position to MyInfo; push the top edge down to mirror the gap above.
            col.OffsetTop = CenterGap;
            col.OffsetBottom = -(Margin + EdgeInfoGap);
        }

        col.AddChild(upper);
        col.AddChild(lower);
        AddChild(col);
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
