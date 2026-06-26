using Godot;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.View;

/// <summary>
/// Card face built from a rarity-specific frame PNG (art/ui/cards/frame_*.png, 414×594 source)
/// rendered as a TextureRect, with the card art and dynamic text/numbers overlaid at fixed coordinates.
///
/// Native scale factor: 200/414 ≈ 0.483; text size + coordinates derive from the original 414×594 design.
/// See 设定文档/卡牌框架坐标_v1.txt for the source-of-truth layout spec.
/// </summary>
public partial class CardView : PanelContainer
{
    public const int CardWidth = 216;
    public const int CardHeight = 314;

    // Source design constants (414×594) — labels scale per FrameScale but minimum font sizes enforced.
    private const float SrcW = 414f;
    private const float SrcH = 594f;
    private static readonly float FrameScale = CardWidth / SrcW;

    [Signal] public delegate void ClickedEventHandler(int instanceId);
    [Signal] public delegate void HoverEnteredEventHandler(int instanceId);
    [Signal] public delegate void HoverExitedEventHandler(int instanceId);

    public InstanceId Instance { get; private set; }
    public CardId Card { get; private set; }
    public bool IsOnField { get; private set; }

    private Control _innerRoot = null!;
    private TextureRect _frameTex = null!;
    private TextureRect _artTex = null!;
    private Label _artPlaceholder = null!;
    private Label _cost = null!;
    private Label _name = null!;
    private Label _atk = null!;
    private Label _hp = null!;
    private CardShieldOverlay _shieldOverlay = null!;
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

        // PanelContainer needs a transparent style so the PNG frame is unobstructed by any default panel chrome.
        var empty = new StyleBoxEmpty();
        AddThemeStyleboxOverride("panel", empty);

        // One inner Control hosts all positioned children with full anchors.
        _innerRoot = new Control { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_innerRoot);

        // Layer 0: card art texture (in art_rect 57,97 → 357,537 of source).
        _artTex = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AnchorBox(_artTex, 57, 97, 357, 537);
        _innerRoot.AddChild(_artTex);

        // Art placeholder text (shown when no ArtPath given).
        _artPlaceholder = new Label
        {
            Text = "CARD\nART",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Modulate = new Color(0.5f, 0.42f, 0.32f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _artPlaceholder.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(28 * FrameScale));
        AnchorBox(_artPlaceholder, 57, 97, 357, 537);
        _innerRoot.AddChild(_artPlaceholder);

        // Layer 1: frame texture (full card area).
        _frameTex = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore,
            AnchorRight = 1, AnchorBottom = 1,
        };
        _innerRoot.AddChild(_frameTex);

        // Layer 2: text overlays. Each label is sized into a box big enough for centered text (incl. 2-digit values)
        // and positioned so its center equals the source diamond coordinate.
        _cost = MakeBadgeLabel(36, new Color(1, 1, 1), minPx: 13);
        AnchorCentered(_cost, 57, 57, halfBox: 38);
        _innerRoot.AddChild(_cost);

        _name = MakeBadgeLabel(22, new Color(0.95f, 0.88f, 0.7f), minPx: 12, bold: true);
        _name.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.85f));
        _name.AddThemeConstantOverride("outline_size", 4);
        AnchorCentered(_name, 207, 54, halfBoxW: 165, halfBoxH: 22);
        _innerRoot.AddChild(_name);

        _atk = MakeBadgeLabel(34, new Color(1, 1, 1), minPx: 13);
        AnchorCentered(_atk, 57, 537, halfBox: 38);
        _innerRoot.AddChild(_atk);

        _hp = MakeBadgeLabel(34, new Color(1, 1, 1), minPx: 13);
        AnchorCentered(_hp, 357, 537, halfBox: 38);
        _innerRoot.AddChild(_hp);

        // Shield overlay — added last so it draws on top of everything else.
        _shieldOverlay = new CardShieldOverlay();
        AddChild(_shieldOverlay);
        _shieldOverlay.Visible = false;
    }

    public void Bind(RuntimeCard card, ICardScript script, bool onField, int viewerMana = 0)
    {
        BuildUi();
        Instance = card.Instance;
        Card = card.Card;
        IsOnField = onField;
        _innerRoot.Visible = true;

        // Frame by rarity (frame_bronze / silver / gold / legendary).
        _frameTex.Texture = LoadFrameTexture(script.Rarity);

        // Card art (placeholder when ArtPath is empty/missing).
        var artPath = script.ArtPath;
        if (!string.IsNullOrEmpty(artPath) && ResourceLoader.Exists(artPath))
        {
            _artTex.Texture = GD.Load<Texture2D>(artPath);
            _artTex.Visible = true;
            _artPlaceholder.Visible = false;
        }
        else
        {
            _artTex.Texture = null;
            _artTex.Visible = false;
            _artPlaceholder.Visible = true;
        }

        // 强化预览：手牌中费用满足时显示绿色 X，否则原费用。
        bool enhanceActive = !onField && script.EnhanceCost > 0 && viewerMana >= script.EnhanceCost;
        int displayCost = enhanceActive ? script.EnhanceCost : script.Cost;
        _cost.Text = displayCost.ToString();
        _cost.Modulate = enhanceActive ? new Color(0.55f, 1f, 0.55f) : new Color(1, 1, 1);

        _name.Text = script.Name;

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
            _atk.Visible = false;
            _hp.Text = cd >= 0 ? cd.ToString() : "";
            _hp.Visible = cd >= 0;
        }
        else
        {
            _atk.Visible = false;
            _hp.Visible = false;
        }

        // Silence dims the whole card.
        Modulate = (onField && card.IsSilenced) ? new Color(0.55f, 0.55f, 0.55f) : new Color(1, 1, 1);

        TooltipText = $"{script.Name}\n{script.Description}";

        // Field-only shield ellipses.
        if (onField)
        {
            bool ward = card.HasKeyword(Keyword.Ward);
            bool barrier = card.BarrierStacks > 0;
            _shieldOverlay.Refresh(ward, barrier);
        }
        else
        {
            _shieldOverlay.Refresh(false, false);
        }
    }

    /// <summary>Render as an opaque card back (opponent hand in hot seat).</summary>
    public void BindFaceDown()
    {
        BuildUi();
        Instance = InstanceId.None;
        Card = CardId.None;
        IsOnField = false;
        _innerRoot.Visible = false;
        // Reuse the bronze frame as the universal "card back" without overlays.
        // A dedicated card-back PNG can replace this later.
        _frameTex.Texture = LoadFrameTexture(Rarity.Bronze);
        Modulate = new Color(0.35f, 0.32f, 0.45f);
        TooltipText = "对手手牌";
    }

    private static Texture2D? LoadFrameTexture(Rarity rarity)
    {
        var path = $"res://art/ui/cards/frame_{rarity.ToString().ToLower()}.png";
        return ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
    }

    private static Label MakeBadgeLabel(int srcFontSize, Color color, int minPx = 10, bool bold = false)
    {
        var l = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Modulate = color,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        int scaledPx = Mathf.RoundToInt(srcFontSize * FrameScale);
        l.AddThemeFontSizeOverride("font_size", Math.Max(minPx, scaledPx));
        return l;
    }

    private static void AnchorBox(Control c, float srcLeft, float srcTop, float srcRight, float srcBottom)
    {
        c.AnchorLeft = 0; c.AnchorTop = 0; c.AnchorRight = 0; c.AnchorBottom = 0;
        c.OffsetLeft = srcLeft * FrameScale;
        c.OffsetTop = srcTop * FrameScale;
        c.OffsetRight = srcRight * FrameScale;
        c.OffsetBottom = srcBottom * FrameScale;
    }

    private static void AnchorCentered(Control c, float srcX, float srcY, float halfBox)
        => AnchorCentered(c, srcX, srcY, halfBox, halfBox);

    private static void AnchorCentered(Control c, float srcX, float srcY, float halfBoxW, float halfBoxH)
    {
        float cx = srcX * FrameScale;
        float cy = srcY * FrameScale;
        float hw = halfBoxW * FrameScale;
        float hh = halfBoxH * FrameScale;
        c.AnchorLeft = 0; c.AnchorTop = 0; c.AnchorRight = 0; c.AnchorBottom = 0;
        c.OffsetLeft = cx - hw;
        c.OffsetTop = cy - hh;
        c.OffsetRight = cx + hw;
        c.OffsetBottom = cy + hh;
    }

    private void OnGuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            EmitSignal(SignalName.Clicked, Instance.Value);
    }
}
