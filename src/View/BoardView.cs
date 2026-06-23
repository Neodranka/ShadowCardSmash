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
    // Tile row width = 6 slots * slot-width + 5 separations; info bars constrain themselves to this width.
    public const int TileRowWidth = PlayerState.FieldSize * TileSlotView.Width + (PlayerState.FieldSize - 1) * 8;

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

        var root = new VBoxContainer
        {
            AnchorLeft = 0, AnchorTop = 0, AnchorRight = 1, AnchorBottom = 1,
            OffsetLeft = Margin, OffsetTop = Margin,
            OffsetRight = -Margin, OffsetBottom = -Margin,
        };
        root.AddThemeConstantOverride("separation", RowSeparation);
        AddChild(root);

        // Enemy section — flanks on the screen edges, middle column (info + hand + field) is centered and
        // capped at tile-row width. Info now sits INSIDE this middle column so there are no spacers next to it.
        EnemyTopLeftSlot = MakePileSlot();
        EnemyGrave = MakePileSlot();
        EnemyTopRightSlot = MakePileSlot();
        EnemyDeck = MakePileSlot();
        EnemyHand = new HandView { ShowFaces = false };
        EnemyHand.CardSelected += iid => EmitSignal(SignalName.HandCardClicked, (int)EnemyHand.Side, iid);
        EnemyInfo = new PlayerInfoPanel { CustomMinimumSize = new Vector2(TileRowWidth, 70), SizeFlagsHorizontal = SizeFlags.Fill };
        EnemyInfo.HeroClicked += idx => EmitSignal(SignalName.HeroClicked, idx);
        EnemyTiles = BuildPlayerSection(
            root, isTopSide: true,
            EnemyInfo, EnemyHand,
            placeholderUpper: EnemyTopLeftSlot, leftFlankLower: EnemyGrave,
            placeholderUpper2: EnemyTopRightSlot, rightFlankLower: EnemyDeck);

        // Vertical Spacer + narrow HSep + Spacer: pushes EnemySection up and MySection down, HSep at viewport center.
        root.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });

        var sep = new HSeparator
        {
            CustomMinimumSize = new Vector2(720, 4),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
        };
        root.AddChild(sep);

        root.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });

        // My section: middle is (field + hand + info). Flanks have piles ABOVE placeholders (deck/grave near HSep).
        MyTopLeftSlot = MakePileSlot();
        MyDeck = MakePileSlot();
        MyTopRightSlot = MakePileSlot();
        MyGrave = MakePileSlot();
        MyHand = new HandView { ShowFaces = true };
        MyHand.CardSelected += iid => EmitSignal(SignalName.HandCardClicked, (int)MyHand.Side, iid);
        MyHand.CardHovered += OnHandCardHovered;
        MyHand.CardHoverExited += _ => DetailPanel.HoverHide();
        MyInfo = new PlayerInfoPanel { CustomMinimumSize = new Vector2(TileRowWidth, 70), SizeFlagsHorizontal = SizeFlags.Fill };
        MyInfo.HeroClicked += idx => EmitSignal(SignalName.HeroClicked, idx);
        MyTiles = BuildPlayerSection(
            root, isTopSide: false,
            MyInfo, MyHand,
            placeholderUpper: MyTopLeftSlot, leftFlankLower: MyDeck,
            placeholderUpper2: MyTopRightSlot, rightFlankLower: MyGrave);

        // End Turn anchored to right edge, vertical center. Sits in the gap between flanks (above MyRightFlank,
        // below EnemyRightFlank) since sections shrink to their natural height and don't reach the screen center.
        EndTurnButton = new Button
        {
            Text = "结束回合",
            CustomMinimumSize = new Vector2(160, 60),
            AnchorLeft = 1, AnchorTop = 0.5f, AnchorRight = 1, AnchorBottom = 0.5f,
            OffsetLeft = -176, OffsetTop = -30, OffsetRight = -16, OffsetBottom = 30,
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

        // Card detail side panel (hidden until hover or pin). TopLevel + absolute Z keeps it above piles and popups.
        DetailPanel = new CardDetailPanel();
        AddChild(DetailPanel);
    }

    /// <summary>
    /// Build one player's section HBox: left flank | spacer | middle (info + hand + field) | spacer | right flank.
    /// Info bar sits at the outer edge of the middle column so it lines up with hand/field, no side spacers.
    /// Top side: info → hand → field, flank cells stack placeholder ↑ then deck/grave ↓ (deck/grave near HSep).
    /// Bottom side: field → hand → info, flank cells stack deck/grave ↑ then placeholder ↓ (deck/grave near HSep).
    /// </summary>
    private TileSlotView[] BuildPlayerSection(
        Container parent, bool isTopSide,
        PlayerInfoPanel infoPanel, HandView handView,
        PileView placeholderUpper, PileView leftFlankLower,
        PileView placeholderUpper2, PileView rightFlankLower)
    {
        var section = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        section.AddThemeConstantOverride("separation", 8);
        parent.AddChild(section);

        // Left flank column.
        var leftCol = new VBoxContainer();
        leftCol.AddThemeConstantOverride("separation", RowSeparation);
        if (isTopSide) { leftCol.AddChild(placeholderUpper); leftCol.AddChild(leftFlankLower); }
        else           { leftCol.AddChild(leftFlankLower); leftCol.AddChild(placeholderUpper); }
        section.AddChild(leftCol);

        section.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        // Middle column: info + hand + field (or field + hand + info for bottom side).
        var middle = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(TileRowWidth, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
        };
        middle.AddThemeConstantOverride("separation", RowSeparation);
        var tiles = new TileSlotView[PlayerState.FieldSize];
        var fieldRow = BuildBareFieldRow(tiles);
        if (isTopSide)
        {
            middle.AddChild(infoPanel);
            middle.AddChild(handView);
            middle.AddChild(fieldRow);
        }
        else
        {
            middle.AddChild(fieldRow);
            middle.AddChild(handView);
            middle.AddChild(infoPanel);
        }
        section.AddChild(middle);

        section.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        // Right flank column.
        var rightCol = new VBoxContainer();
        rightCol.AddThemeConstantOverride("separation", RowSeparation);
        if (isTopSide) { rightCol.AddChild(placeholderUpper2); rightCol.AddChild(rightFlankLower); }
        else           { rightCol.AddChild(rightFlankLower); rightCol.AddChild(placeholderUpper2); }
        section.AddChild(rightCol);

        return tiles;
    }

    private HBoxContainer BuildBareFieldRow(TileSlotView[] tilesOut)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        row.AddThemeConstantOverride("separation", 8);
        for (int i = 0; i < PlayerState.FieldSize; i++)
        {
            var tile = new TileSlotView { TileIndex = i };
            tile.TileClicked += idx => EmitSignal(SignalName.TileClicked, (int)tile.Side, idx);
            row.AddChild(tile);
            tilesOut[i] = tile;
        }
        return row;
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
