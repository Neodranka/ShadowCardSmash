using Godot;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.View;

/// <summary>
/// Modal popup that reveals the top N cards of the player's own deck (N ≤ 3). Two exit paths:
///   • ChosenPromote(index): pick one of the shown cards to become the topmost of the deck.
///   • ChosenShuffleAndPickHand: shuffle those N back into the deck, then place a hand card on top
///     and refund 1 mana. Followed up by <see cref="HandMultiSelectPopup"/> to pick the hand card.
///   • Cancelled: player backs out.
/// Used by 利害权衡; the two branches align with ChoiceIndices[0] = 0 vs 1 on the wire.
/// </summary>
public partial class ScryPopup : Control
{
    [Signal] public delegate void ChosenPromoteEventHandler(int index);
    [Signal] public delegate void ChosenShuffleAndPickHandEventHandler();
    [Signal] public delegate void CancelledEventHandler();

    private bool _builtUi;

    public override void _Ready()
    {
        AnchorRight = 1; AnchorBottom = 1;
        ProcessMode = ProcessModeEnum.Always;
    }

    public void Populate(string title, System.Collections.Generic.IReadOnlyList<RuntimeCard> topN, ICardDatabase db)
    {
        BuildUi(title, topN, db);
    }

    private void BuildUi(string title, System.Collections.Generic.IReadOnlyList<RuntimeCard> topN, ICardDatabase db)
    {
        if (_builtUi) return;
        _builtUi = true;

        var dim = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.65f),
            AnchorRight = 1, AnchorBottom = 1,
            MouseFilter = MouseFilterEnum.Stop,
        };
        AddChild(dim);

        var panel = new PanelContainer
        {
            AnchorLeft = 0.5f, AnchorTop = 0.5f, AnchorRight = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft = -520, OffsetTop = -300, OffsetRight = 520, OffsetBottom = 300,
        };
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.08f, 0.16f, 0.97f),
            BorderColor = new Color(0.7f, 0.6f, 0.9f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 16, ContentMarginBottom = 16,
        };
        panel.AddThemeStyleboxOverride("panel", sb);
        AddChild(panel);

        var vb = new VBoxContainer();
        vb.AddThemeConstantOverride("separation", 12);
        panel.AddChild(vb);

        var titleLabel = new Label { Text = title, HorizontalAlignment = HorizontalAlignment.Center };
        titleLabel.AddThemeFontSizeOverride("font_size", 22);
        titleLabel.Modulate = new Color(0.95f, 0.9f, 1f);
        vb.AddChild(titleLabel);

        var hint = new Label
        {
            Text = "选择一张作为牌库顶（左至右为原本的顶三张），或选择「洗回牌库并从手牌置顶」。",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        hint.AddThemeFontSizeOverride("font_size", 14);
        hint.Modulate = new Color(0.85f, 0.85f, 0.95f);
        vb.AddChild(hint);

        vb.AddChild(new HSeparator());

        // 3-card grid with per-card "选此置顶" button underneath.
        var grid = new HBoxContainer();
        grid.AddThemeConstantOverride("separation", 12);
        grid.Alignment = BoxContainer.AlignmentMode.Center;
        vb.AddChild(grid);

        for (int i = 0; i < topN.Count; i++)
        {
            int captured = i;
            var col = new VBoxContainer();
            col.AddThemeConstantOverride("separation", 6);
            grid.AddChild(col);

            var cv = new CardView();
            col.AddChild(cv);
            cv.Bind(topN[i], db.Get(topN[i].Card), onField: false);

            var btn = new Button { Text = $"选此置顶（第 {i + 1} 张）", CustomMinimumSize = new Vector2(0, 36) };
            btn.Pressed += () => { EmitSignal(SignalName.ChosenPromote, captured); QueueFree(); };
            col.AddChild(btn);
        }

        vb.AddChild(new HSeparator());

        var bottomRow = new HBoxContainer();
        bottomRow.AddThemeConstantOverride("separation", 12);
        bottomRow.Alignment = BoxContainer.AlignmentMode.Center;
        vb.AddChild(bottomRow);

        var shuffleBtn = new Button
        {
            Text = "洗入牌库 + 从手牌选一张置牌库顶（回复 1 费）",
            CustomMinimumSize = new Vector2(360, 40),
            Modulate = new Color(1f, 0.9f, 0.7f),
        };
        shuffleBtn.Pressed += () => { EmitSignal(SignalName.ChosenShuffleAndPickHand); QueueFree(); };
        bottomRow.AddChild(shuffleBtn);

        var cancelBtn = new Button { Text = "取消", CustomMinimumSize = new Vector2(120, 40) };
        cancelBtn.Pressed += () => { EmitSignal(SignalName.Cancelled); QueueFree(); };
        bottomRow.AddChild(cancelBtn);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if ((@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right })
            || (@event is InputEventKey { Pressed: true, Keycode: Key.Escape }))
        {
            EmitSignal(SignalName.Cancelled);
            QueueFree();
            GetViewport().SetInputAsHandled();
        }
    }
}
