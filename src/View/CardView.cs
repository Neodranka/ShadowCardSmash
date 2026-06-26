using Godot;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.View;

/// <summary>
/// Card face built from a rarity-specific no-diamond frame PNG (art/ui/cards/frame_*_nd.png, 360×540 source)
/// rendered as a TextureRect, with the card art and dynamic text/numbers overlaid at fixed coordinates,
/// plus three independent diamond TextureRects (cost/atk/hp) that protrude beyond the frame body.
///
/// See 设定文档/卡牌框架坐标_v1.txt for the source-of-truth layout spec.
/// </summary>
public partial class CardView : PanelContainer
{
    // 2:3 ratio matches the new frame_*_nd asset (360×540 source).
    public const int CardWidth = 192;
    public const int CardHeight = 288;

    // Source design constants (360×540). FrameScale derives uniformly from width — control aspect matches asset.
    private const float SrcW = 360f;
    private const float SrcH = 540f;
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
    private TextureRect _diamondCost = null!;
    private TextureRect _diamondAtk = null!;
    private TextureRect _diamondHp = null!;
    private TextureRect _shieldWard = null!;
    private TextureRect _shieldBarrier = null!;
    private TextureRect _attackIcon = null!;
    private Label _cost = null!;
    private Label _name = null!;
    private Label _atk = null!;
    private Label _hp = null!;
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

        // Layer 0: card art texture (art_rect 30,70 → 330,510 — 300×440 safe zone).
        _artTex = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AnchorBox(_artTex, 30, 70, 330, 510);
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
        _artPlaceholder.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(22 * FrameScale));
        AnchorBox(_artPlaceholder, 30, 70, 330, 510);
        _innerRoot.AddChild(_artPlaceholder);

        // Layer 1: no-diamond frame texture (full card area). Diamonds rendered as separate layers below.
        _frameTex = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore,
            AnchorRight = 1, AnchorBottom = 1,
        };
        _innerRoot.AddChild(_frameTex);

        // Layer 2: diamonds (140×140 source, center at PNG (70,70)). They protrude past the card body
        // by ~40 src units on the cost (top-left), atk (bottom-left), and hp (bottom-right) corners.
        _diamondCost = MakeDiamondRect();
        AnchorBox(_diamondCost, -40, -40, 100, 100);   // center at source (30, 30)
        _innerRoot.AddChild(_diamondCost);

        _diamondAtk = MakeDiamondRect();
        AnchorBox(_diamondAtk, -40, 440, 100, 580);    // center at source (30, 510)
        _innerRoot.AddChild(_diamondAtk);

        _diamondHp = MakeDiamondRect();
        AnchorBox(_diamondHp, 260, 440, 400, 580);     // center at source (330, 510)
        _innerRoot.AddChild(_diamondHp);

        // Layer 3a: shield overlays — semi-transparent PNGs covering the card art area.
        // Ward PNG is opaque-ish by design; dim it heavily via Modulate so it reads as a subtle aura.
        _shieldWard = MakeOverlayRect();
        AnchorBox(_shieldWard, 0, 30, 360, 510);       // 360×480 centered with 30 src margin top/bottom
        _shieldWard.Modulate = new Color(1f, 1f, 1f, 0.30f);
        _innerRoot.AddChild(_shieldWard);

        _shieldBarrier = MakeOverlayRect();
        AnchorBox(_shieldBarrier, 20, 40, 340, 500);   // 320×460 centered (user-supplied alpha kept as-is)
        _innerRoot.AddChild(_shieldBarrier);

        // Layer 3b: attack-ready icon (storm = can attack hero+minion, rush = minion-only).
        // Full card-body PNG drawn above diamonds, below text so numbers stay readable.
        _attackIcon = MakeOverlayRect();
        AnchorBox(_attackIcon, 0, 0, 360, 540);        // full body, the PNGs already align to (0,0)-(360,540)
        _innerRoot.AddChild(_attackIcon);

        // Layer 4: text overlays — centered on the diamond / ribbon coordinates.
        // Source font sizes calibrated for the 360×540 layout (annotation v3).
        _cost = MakeBadgeLabel(28, new Color(1, 1, 1), minPx: 13);
        AnchorCentered(_cost, 30, 30, halfBox: 30);
        _innerRoot.AddChild(_cost);

        _name = MakeBadgeLabel(18, new Color(0.95f, 0.88f, 0.7f), minPx: 11, bold: true);
        _name.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.85f));
        _name.AddThemeConstantOverride("outline_size", 4);
        AnchorCentered(_name, 180, 27, halfBoxW: 150, halfBoxH: 18);
        _innerRoot.AddChild(_name);

        _atk = MakeBadgeLabel(26, new Color(1, 1, 1), minPx: 13);
        AnchorCentered(_atk, 30, 510, halfBox: 30);
        _innerRoot.AddChild(_atk);

        _hp = MakeBadgeLabel(26, new Color(1, 1, 1), minPx: 13);
        AnchorCentered(_hp, 330, 510, halfBox: 30);
        _innerRoot.AddChild(_hp);
    }

    public void Bind(RuntimeCard card, ICardScript script, bool onField, int viewerMana = 0)
    {
        BuildUi();
        Instance = card.Instance;
        Card = card.Card;
        IsOnField = onField;
        _innerRoot.Visible = true;

        // No-diamond frame by rarity + separate diamond layers (cost diamond color follows rarity).
        _frameTex.Texture = LoadFrameTexture(script.Rarity);
        _diamondCost.Texture = LoadDiamondCost(script.Rarity);
        _diamondAtk.Texture = LoadDiamondAtk();
        _diamondHp.Texture = LoadDiamondHp();
        _diamondCost.Visible = true;

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
        _diamondAtk.Visible = _atk.Visible;
        _diamondHp.Visible = _hp.Visible;

        // Silence dims the whole card.
        Modulate = (onField && card.IsSilenced) ? new Color(0.55f, 0.55f, 0.55f) : new Color(1, 1, 1);

        TooltipText = $"{script.Name}\n{script.Description}";

        // Field-only overlays: ward/barrier shields + attack-ready icon (storm/rush).
        if (onField)
        {
            _shieldWard.Texture = LoadShieldWard();
            _shieldBarrier.Texture = LoadShieldBarrier();
            _shieldWard.Visible = card.HasKeyword(Keyword.Ward);
            _shieldBarrier.Visible = card.BarrierStacks > 0;

            // Storm icon: can attack any target (incl. hero). Rush icon: minion-only on summon turn.
            bool stormReady = card.CanAttackThisTurn && (!card.SummonedThisTurn || card.HasKeyword(Keyword.Storm));
            bool rushReady  = card.CanAttackThisTurn && card.SummonedThisTurn
                              && card.HasKeyword(Keyword.Rush) && !card.HasKeyword(Keyword.Storm);
            if (stormReady)      { _attackIcon.Texture = LoadAttackIconStorm(); _attackIcon.Visible = true; }
            else if (rushReady)  { _attackIcon.Texture = LoadAttackIconRush();  _attackIcon.Visible = true; }
            else                 { _attackIcon.Visible = false; }
        }
        else
        {
            _shieldWard.Visible = false;
            _shieldBarrier.Visible = false;
            _attackIcon.Visible = false;
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
        // Reuse the bronze no-diamond frame as the universal "card back" without overlays.
        // A dedicated card-back PNG can replace this later.
        _frameTex.Texture = LoadFrameTexture(Rarity.Bronze);
        _diamondCost.Visible = false;
        _diamondAtk.Visible = false;
        _diamondHp.Visible = false;
        _shieldWard.Visible = false;
        _shieldBarrier.Visible = false;
        _attackIcon.Visible = false;
        Modulate = new Color(0.35f, 0.32f, 0.45f);
        TooltipText = "对手手牌";
    }

    private static Texture2D? LoadFrameTexture(Rarity rarity)
    {
        // Uses no-diamond frames; diamonds are rendered as separate TextureRect layers above.
        var path = $"res://art/ui/cards/frame_{rarity.ToString().ToLower()}_nd.png";
        return ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
    }

    private static Texture2D? LoadDiamondCost(Rarity rarity)
    {
        var path = $"res://art/ui/cards/diamond_cost_{rarity.ToString().ToLower()}.png";
        return ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
    }

    private static Texture2D? _diamondAtkCache;
    private static Texture2D? _diamondHpCache;
    private static Texture2D? LoadDiamondAtk()
        => _diamondAtkCache ??= ResourceLoader.Exists("res://art/ui/cards/diamond_atk.png")
            ? GD.Load<Texture2D>("res://art/ui/cards/diamond_atk.png") : null;
    private static Texture2D? LoadDiamondHp()
        => _diamondHpCache ??= ResourceLoader.Exists("res://art/ui/cards/diamond_hp.png")
            ? GD.Load<Texture2D>("res://art/ui/cards/diamond_hp.png") : null;

    private static TextureRect MakeDiamondRect() => new()
    {
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.Scale,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    private static TextureRect MakeOverlayRect() => new()
    {
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.Scale,
        MouseFilter = MouseFilterEnum.Ignore,
        Visible = false,
    };

    private static Texture2D? _shieldWardCache;
    private static Texture2D? _shieldBarrierCache;
    private static Texture2D? _attackIconStormCache;
    private static Texture2D? _attackIconRushCache;
    private static Texture2D? LoadShieldWard()
        => _shieldWardCache ??= ResourceLoader.Exists("res://art/ui/cards/shield_yellow.png")
            ? GD.Load<Texture2D>("res://art/ui/cards/shield_yellow.png") : null;
    private static Texture2D? LoadShieldBarrier()
        => _shieldBarrierCache ??= ResourceLoader.Exists("res://art/ui/cards/shield_barrier_blue.png")
            ? GD.Load<Texture2D>("res://art/ui/cards/shield_barrier_blue.png") : null;
    private static Texture2D? LoadAttackIconStorm()
        => _attackIconStormCache ??= ResourceLoader.Exists("res://art/ui/cards/icon_storm.png")
            ? GD.Load<Texture2D>("res://art/ui/cards/icon_storm.png") : null;
    private static Texture2D? LoadAttackIconRush()
        => _attackIconRushCache ??= ResourceLoader.Exists("res://art/ui/cards/icon_rush.png")
            ? GD.Load<Texture2D>("res://art/ui/cards/icon_rush.png") : null;

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
