using Godot;
using ShadowCardSmash.Domain;

namespace ShadowCardSmash.View;

/// <summary>
/// A single slot on the side of the battlefield: deck pile, graveyard pile, or a placeholder reserved
/// for future features. The slot has the same width as a field tile and roughly half the height so
/// that two stack vertically into one tile's worth of space.
/// </summary>
public partial class PileView : PanelContainer
{
    // Pile flanks are visually decoupled from tile width so that the row fits 1920 even with larger tiles.
    public const int SlotWidth = 180;
    public const int SlotHeight = TileSlotView.Height / 2 - 2; // two pile slots = one tile-row height

    public enum Kind { Deck, Graveyard, Placeholder, Banish }

    [Signal] public delegate void ClickedEventHandler(int sideIndex, int kindIndex);

    public PlayerSide Side { get; set; }
    public Kind PileKind { get; set; }

    private Label _title = null!;
    private Label _count = null!;
    private StyleBoxFlat _sb = null!;
    private bool _builtUi;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(SlotWidth, SlotHeight);
        BuildUi();
        GuiInput += OnGuiInput;
    }

    private void BuildUi()
    {
        if (_builtUi) return;
        _builtUi = true;

        _sb = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.10f, 0.18f),
            BorderColor = new Color(0.5f, 0.45f, 0.6f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 4, ContentMarginBottom = 4,
        };
        AddThemeStyleboxOverride("panel", _sb);

        var vb = new VBoxContainer();
        vb.Alignment = BoxContainer.AlignmentMode.Center;
        vb.AddThemeConstantOverride("separation", 2);
        AddChild(vb);

        _title = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(0.85f, 0.8f, 1f),
        };
        _title.AddThemeFontSizeOverride("font_size", 14);
        vb.AddChild(_title);

        _count = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(1f, 1f, 1f),
        };
        _count.AddThemeFontSizeOverride("font_size", 26);
        vb.AddChild(_count);
    }

    public void BindDeck(int count)
    {
        BuildUi();
        PileKind = Kind.Deck;
        _title.Text = "牌库";
        _count.Text = count.ToString();
        _count.Visible = true;
        _sb.BgColor = new Color(0.12f, 0.10f, 0.22f);
        _sb.BorderColor = new Color(0.45f, 0.55f, 0.85f);
        TooltipText = $"牌库（{count} 张） — 点击查看";
    }

    public void BindGraveyard(int count)
    {
        BuildUi();
        PileKind = Kind.Graveyard;
        _title.Text = "墓地";
        _count.Text = count.ToString();
        _count.Visible = true;
        _sb.BgColor = new Color(0.10f, 0.10f, 0.12f);
        _sb.BorderColor = new Color(0.6f, 0.5f, 0.5f);
        TooltipText = $"墓地（{count} 张） — 点击查看";
    }

    public void BindPlaceholder()
    {
        BuildUi();
        PileKind = Kind.Placeholder;
        _title.Text = "";
        _count.Visible = false;
        _sb.BgColor = new Color(0.08f, 0.08f, 0.1f);
        _sb.BorderColor = new Color(0.25f, 0.25f, 0.3f);
        TooltipText = "";
    }

    public void BindBanish(int count)
    {
        BuildUi();
        PileKind = Kind.Banish;
        _title.Text = "放逐";
        _count.Text = count.ToString();
        _count.Visible = true;
        _sb.BgColor = new Color(0.16f, 0.08f, 0.10f);
        _sb.BorderColor = new Color(0.75f, 0.45f, 0.45f);
        TooltipText = $"放逐区（{count} 张） — 点击查看";
    }

    private void OnGuiInput(InputEvent e)
    {
        if (PileKind == Kind.Placeholder) return;
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            EmitSignal(SignalName.Clicked, (int)Side, (int)PileKind);
    }
}
