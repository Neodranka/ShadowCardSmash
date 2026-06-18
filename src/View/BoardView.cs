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
    public const int Margin = 24;

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
            OffsetLeft = Margin, OffsetTop = Margin, OffsetRight = -Margin, OffsetBottom = -Margin,
        };
        root.AddThemeConstantOverride("separation", 8);
        AddChild(root);

        // Top: enemy info.
        EnemyInfo = new PlayerInfoPanel { SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        EnemyInfo.HeroClicked += idx => EmitSignal(SignalName.HeroClicked, idx);
        root.AddChild(EnemyInfo);

        // Enemy hand (face-down).
        EnemyHand = new HandView { ShowFaces = false };
        EnemyHand.CardSelected += iid => EmitSignal(SignalName.HandCardClicked, (int)EnemyHand.Side, iid);
        root.AddChild(EnemyHand);

        // Enemy field.
        EnemyTiles = BuildFieldRow(root);

        var sep = new HSeparator();
        root.AddChild(sep);

        // My field.
        MyTiles = BuildFieldRow(root);

        // My hand.
        MyHand = new HandView { ShowFaces = true };
        MyHand.CardSelected += iid => EmitSignal(SignalName.HandCardClicked, (int)MyHand.Side, iid);
        root.AddChild(MyHand);

        // Bottom row: my info + end-turn.
        var bottomRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        bottomRow.AddThemeConstantOverride("separation", 16);
        root.AddChild(bottomRow);

        MyInfo = new PlayerInfoPanel { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        MyInfo.HeroClicked += idx => EmitSignal(SignalName.HeroClicked, idx);
        bottomRow.AddChild(MyInfo);

        EndTurnButton = new Button { Text = "End Turn", CustomMinimumSize = new Vector2(160, 80) };
        EndTurnButton.Pressed += () => EmitSignal(SignalName.EndTurnClicked);
        bottomRow.AddChild(EndTurnButton);

        // Status overlay.
        StatusLabel = new Label
        {
            AnchorLeft = 0.5f, AnchorTop = 0.5f, AnchorRight = 0.5f, AnchorBottom = 0.5f,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        StatusLabel.AddThemeFontSizeOverride("font_size", 28);
        StatusLabel.Modulate = new Color(1f, 0.95f, 0.5f);
        AddChild(StatusLabel);
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
            // Read the Side at click time so hot-seat side flips are honoured without rewiring listeners.
            tile.TileClicked += idx => EmitSignal(SignalName.TileClicked, (int)tile.Side, idx);
            row.AddChild(tile);
            tiles[i] = tile;
        }
        return tiles;
    }

    public void Rebind(GameState state, ICardDatabase db, PlayerSide localSide)
    {
        BuildUi();

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

        MyInfo.Rebind(me, myTurn);
        EnemyInfo.Rebind(enemy, !myTurn);

        MyHand.Rebind(me.Hand, db);
        EnemyHand.Rebind(enemy.Hand, db);

        RebindRow(MyTiles, me, db, OnMinionClicked);
        RebindRow(EnemyTiles, enemy, db, OnMinionClicked);

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

    private static void RebindRow(TileSlotView[] tiles, PlayerState p, ICardDatabase db,
        System.Action<TileSlotView, int> onMinionClicked)
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
        }
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
