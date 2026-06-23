using Godot;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.View;

/// <summary>
/// Modal overlay showing every card currently in a pile (deck or graveyard).
/// Closes on the X button, right-click, or Esc.
/// </summary>
public partial class PilePopup : Control
{
    [Signal] public delegate void ClosedEventHandler();

    private GridContainer _grid = null!;
    private Label _title = null!;
    private bool _builtUi;

    public override void _Ready()
    {
        AnchorRight = 1; AnchorBottom = 1;
        BuildUi();
        ProcessMode = ProcessModeEnum.Always;
    }

    private void BuildUi()
    {
        if (_builtUi) return;
        _builtUi = true;

        // Dim background that absorbs clicks (so background board doesn't respond).
        var dim = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.6f),
            AnchorRight = 1, AnchorBottom = 1,
            MouseFilter = MouseFilterEnum.Stop,
        };
        dim.GuiInput += OnDimInput;
        AddChild(dim);

        // Centered panel.
        var panel = new PanelContainer
        {
            AnchorLeft = 0.5f, AnchorTop = 0.5f, AnchorRight = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft = -540, OffsetTop = -400, OffsetRight = 540, OffsetBottom = 400,
        };
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.08f, 0.16f, 0.97f),
            BorderColor = new Color(0.7f, 0.6f, 0.9f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 14, ContentMarginBottom = 14,
        };
        panel.AddThemeStyleboxOverride("panel", sb);
        AddChild(panel);

        var vb = new VBoxContainer();
        vb.AddThemeConstantOverride("separation", 10);
        panel.AddChild(vb);

        var header = new HBoxContainer();
        vb.AddChild(header);

        _title = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Left,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _title.AddThemeFontSizeOverride("font_size", 22);
        _title.Modulate = new Color(0.95f, 0.9f, 1f);
        header.AddChild(_title);

        var closeBtn = new Button { Text = "✕ 关闭", CustomMinimumSize = new Vector2(96, 36) };
        closeBtn.Pressed += Close;
        header.AddChild(closeBtn);

        vb.AddChild(new HSeparator());

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        vb.AddChild(scroll);

        _grid = new GridContainer { Columns = 6 };
        _grid.AddThemeConstantOverride("h_separation", 8);
        _grid.AddThemeConstantOverride("v_separation", 8);
        scroll.AddChild(_grid);
    }

    public void Populate(string title, IReadOnlyList<RuntimeCard> cards, ICardDatabase db)
    {
        BuildUi();
        _title.Text = title;
        for (int i = _grid.GetChildCount() - 1; i >= 0; i--)
        {
            var c = _grid.GetChild(i);
            _grid.RemoveChild(c);
            c.QueueFree();
        }
        foreach (var card in cards)
        {
            var cv = new CardView();
            _grid.AddChild(cv);
            cv.Bind(card, db.Get(card.Card), onField: false);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if ((@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right })
            || (@event is InputEventKey { Pressed: true, Keycode: Key.Escape }))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnDimInput(InputEvent e)
    {
        // Right-click on the dimmed background also closes.
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right })
            Close();
    }

    private void Close()
    {
        EmitSignal(SignalName.Closed);
        QueueFree();
    }
}
