using Godot;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.View;

/// <summary>
/// Minimal card face: cost badge, central art placeholder, attack / health corners. All other detail
/// (description, keywords, tags, type) is shown by <see cref="CardDetailPanel"/> when the card is hovered or selected.
/// </summary>
public partial class CardView : PanelContainer
{
    public const int CardWidth = 140;
    public const int CardHeight = 190;

    [Signal] public delegate void ClickedEventHandler(int instanceId);
    [Signal] public delegate void HoverEnteredEventHandler(int instanceId);
    [Signal] public delegate void HoverExitedEventHandler(int instanceId);

    public InstanceId Instance { get; private set; }
    public CardId Card { get; private set; }
    public bool IsOnField { get; private set; }

    private Control _innerRoot = null!;
    private Label _cost = null!;
    private Label _atk = null!;
    private Label _hp = null!;
    private Panel _artPanel = null!;
    private StyleBoxFlat _stylebox = null!;
    private StyleBoxFlat _costStyle = null!;
    private StyleBoxFlat _artStyle = null!;
    private bool _builtUi;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(CardWidth, CardHeight);
        BuildUi();
        GuiInput += OnGuiInput;
        MouseEntered += () => EmitSignal(SignalName.HoverEntered, Instance.Value);
        MouseExited += () => EmitSignal(SignalName.HoverExited, Instance.Value);
    }

    private void BuildUi()
    {
        if (_builtUi) return;
        _builtUi = true;

        _stylebox = new StyleBoxFlat
        {
            BgColor = new Color(0.18f, 0.12f, 0.22f),
            BorderColor = new Color(0.6f, 0.5f, 0.8f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginLeft = 0, ContentMarginRight = 0, ContentMarginTop = 0, ContentMarginBottom = 0,
        };
        AddThemeStyleboxOverride("panel", _stylebox);

        // Inner Control fills the PanelContainer; positioned children use anchors on this Control.
        _innerRoot = new Control { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_innerRoot);

        // Art placeholder fills the middle.
        _artStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.32f, 0.22f, 0.4f),
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
        };
        _artPanel = new Panel
        {
            AnchorLeft = 0, AnchorTop = 0, AnchorRight = 1, AnchorBottom = 1,
            OffsetLeft = 10, OffsetTop = 38, OffsetRight = -10, OffsetBottom = -42,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _artPanel.AddThemeStyleboxOverride("panel", _artStyle);
        _innerRoot.AddChild(_artPanel);

        // Cost badge — circular blue pill in top-left.
        _costStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.35f, 0.7f),
            BorderColor = new Color(0.7f, 0.85f, 1f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusTopLeft = 16, CornerRadiusTopRight = 16, CornerRadiusBottomLeft = 16, CornerRadiusBottomRight = 16,
        };
        var costContainer = new PanelContainer
        {
            AnchorLeft = 0, AnchorTop = 0, AnchorRight = 0, AnchorBottom = 0,
            OffsetLeft = 4, OffsetTop = 4, OffsetRight = 36, OffsetBottom = 36,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        costContainer.AddThemeStyleboxOverride("panel", _costStyle);
        _innerRoot.AddChild(costContainer);
        _cost = new Label { Text = "0", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        _cost.AddThemeFontSizeOverride("font_size", 20);
        costContainer.AddChild(_cost);

        // Attack bottom-left.
        _atk = new Label
        {
            Text = "",
            AnchorLeft = 0, AnchorTop = 1, AnchorRight = 0, AnchorBottom = 1,
            OffsetLeft = 8, OffsetTop = -34, OffsetRight = 40, OffsetBottom = -4,
            Modulate = new Color(1f, 0.85f, 0.4f),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _atk.AddThemeFontSizeOverride("font_size", 24);
        _innerRoot.AddChild(_atk);

        // Health bottom-right.
        _hp = new Label
        {
            Text = "",
            AnchorLeft = 1, AnchorTop = 1, AnchorRight = 1, AnchorBottom = 1,
            OffsetLeft = -40, OffsetTop = -34, OffsetRight = -8, OffsetBottom = -4,
            Modulate = new Color(1f, 0.4f, 0.4f),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _hp.AddThemeFontSizeOverride("font_size", 24);
        _innerRoot.AddChild(_hp);
    }

    public void Bind(RuntimeCard card, ICardScript script, bool onField)
    {
        BuildUi();
        Instance = card.Instance;
        Card = card.Card;
        IsOnField = onField;
        _innerRoot.Visible = true;

        _cost.Text = script.Cost.ToString();

        if (script.CardType == CardType.Minion)
        {
            int atk = onField ? card.CurrentAttack : script.BaseAttack;
            int hp = onField ? card.CurrentHealth : script.BaseHealth;
            _atk.Text = atk.ToString();
            _hp.Text = hp.ToString();
            _atk.Visible = true; _hp.Visible = true;
        }
        else if (script.CardType == CardType.Amulet)
        {
            int cd = onField ? card.Countdown : script.InitialCountdown;
            _atk.Text = "";
            _hp.Text = cd >= 0 ? $"⏳{cd}" : "";
            _atk.Visible = false; _hp.Visible = cd >= 0;
        }
        else
        {
            _atk.Visible = false;
            _hp.Visible = false;
        }

        ApplyAccentColors(card, script, onField);
        TooltipText = $"{script.Name}\n{script.Description}";
    }

    /// <summary>
    /// Render as an opaque card back: hide all face content. Used for the opponent hand in hot seat.
    /// </summary>
    public void BindFaceDown()
    {
        BuildUi();
        Instance = InstanceId.None;
        Card = CardId.None;
        IsOnField = false;
        _innerRoot.Visible = false;

        _stylebox.BgColor = new Color(0.10f, 0.08f, 0.16f);
        _stylebox.BorderColor = new Color(0.45f, 0.4f, 0.55f);
        _stylebox.BorderWidthTop = 3; _stylebox.BorderWidthBottom = 3;
        _stylebox.BorderWidthLeft = 3; _stylebox.BorderWidthRight = 3;
        Modulate = new Color(1, 1, 1);
        TooltipText = "对手手牌";
    }

    private void ApplyAccentColors(RuntimeCard card, ICardScript script, bool onField)
    {
        var baseBorder = script.HeroClass switch
        {
            HeroClass.Forsaken => new Color(0.85f, 0.25f, 0.35f),
            HeroClass.Neutral => new Color(0.55f, 0.55f, 0.65f),
            HeroClass.Empire => new Color(0.85f, 0.75f, 0.35f),
            HeroClass.ClassC => new Color(0.35f, 0.85f, 0.45f),
            _ => new Color(0.6f, 0.5f, 0.8f),
        };
        int borderW = 2;
        var borderColor = baseBorder;

        var effectiveKeywords = onField ? card.Keywords : script.InitialKeywords;
        bool hasWard = (effectiveKeywords & Keyword.Ward) == Keyword.Ward;

        if (hasWard) { borderColor = new Color(1f, 0.85f, 0.3f); borderW = 5; }
        else if (onField && card.IsEvolved) { borderColor = new Color(1f, 0.55f, 1f); borderW = 4; }

        _stylebox.BgColor = new Color(0.18f, 0.12f, 0.22f);
        _stylebox.BorderColor = borderColor;
        _stylebox.BorderWidthTop = borderW; _stylebox.BorderWidthBottom = borderW;
        _stylebox.BorderWidthLeft = borderW; _stylebox.BorderWidthRight = borderW;

        // Art-area tint hints at class so it isn't a featureless box until real art arrives.
        _artStyle.BgColor = baseBorder * new Color(0.5f, 0.5f, 0.5f, 1f);

        Modulate = (onField && card.IsSilenced) ? new Color(0.55f, 0.55f, 0.55f) : new Color(1, 1, 1);
    }

    private void OnGuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            EmitSignal(SignalName.Clicked, Instance.Value);
    }
}
