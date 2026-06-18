using Godot;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.View;

/// <summary>
/// Single-card visual. Pure presentation: never mutates GameState.
/// Build via <see cref="Bind"/>; emit <see cref="Clicked"/> for the controller to consume.
/// </summary>
public partial class CardView : PanelContainer
{
    public const int CardWidth = 150;
    public const int CardHeight = 190;

    [Signal] public delegate void ClickedEventHandler(int instanceId);

    public InstanceId Instance { get; private set; }
    public CardId Card { get; private set; }
    public bool IsOnField { get; private set; }

    private Label _name = null!;
    private Label _cost = null!;
    private Label _typeTags = null!;
    private Label _description = null!;
    private Label _keywords = null!;
    private Label _stats = null!;
    private StyleBoxFlat _stylebox = null!;
    private bool _builtUi;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(CardWidth, CardHeight);
        BuildUi();
        GuiInput += OnGuiInput;
    }

    private void BuildUi()
    {
        if (_builtUi) return;
        _builtUi = true;

        _stylebox = new StyleBoxFlat
        {
            BgColor = new Color(0.18f, 0.12f, 0.22f),
            BorderColor = new Color(0.6f, 0.5f, 0.8f),
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
        };
        AddThemeStyleboxOverride("panel", _stylebox);

        var vb = new VBoxContainer();
        vb.AddThemeConstantOverride("separation", 2);
        AddChild(vb);

        var headerRow = new HBoxContainer();
        vb.AddChild(headerRow);
        _cost = new Label { Text = "0", Modulate = new Color(0.55f, 0.85f, 1f) };
        _cost.AddThemeFontSizeOverride("font_size", 18);
        headerRow.AddChild(_cost);
        _name = new Label
        {
            Text = "Card",
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _name.AddThemeFontSizeOverride("font_size", 14);
        headerRow.AddChild(_name);

        _typeTags = new Label { Modulate = new Color(0.7f, 0.7f, 0.8f) };
        _typeTags.AddThemeFontSizeOverride("font_size", 10);
        vb.AddChild(_typeTags);

        _description = new Label
        {
            Text = "",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(CardWidth - 16, 0),
        };
        _description.AddThemeFontSizeOverride("font_size", 10);
        vb.AddChild(_description);

        _keywords = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Left,
            Modulate = new Color(1f, 0.85f, 0.3f),
        };
        _keywords.AddThemeFontSizeOverride("font_size", 11);
        vb.AddChild(_keywords);

        _stats = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Right,
            Modulate = new Color(1f, 0.95f, 0.4f),
        };
        _stats.AddThemeFontSizeOverride("font_size", 18);
        vb.AddChild(_stats);
    }

    public void Bind(RuntimeCard card, ICardScript script, bool onField)
    {
        BuildUi();
        Instance = card.Instance;
        Card = card.Card;
        IsOnField = onField;

        _name.Visible = true; _cost.Visible = true; _typeTags.Visible = true;
        _description.Visible = true; _keywords.Visible = true;

        _name.Text = script.Name;
        _cost.Text = script.Cost.ToString();
        _typeTags.Text = BuildTypeTagLine(script);
        _description.Text = BuildDescription(script, card);
        _keywords.Text = BuildKeywordLine(card, script, onField);

        if (script.CardType == CardType.Minion)
        {
            // In hand the RuntimeCard has not been instantiated yet (CurrentAttack/Health are 0), show base stats.
            int atk = onField ? card.CurrentAttack : script.BaseAttack;
            int hp = onField ? card.CurrentHealth : script.BaseHealth;
            _stats.Text = $"⚔ {atk}    ♥ {hp}";
            _stats.Visible = true;
        }
        else if (script.CardType == CardType.Amulet)
        {
            int cd = onField ? card.Countdown : script.InitialCountdown;
            _stats.Text = cd >= 0 ? $"⏳ {cd}" : "";
            _stats.Visible = cd >= 0;
        }
        else
        {
            _stats.Text = "";
            _stats.Visible = false;
        }

        ApplyAccentColors(card, script, onField);
        TooltipText = BuildTooltip(script, card);
    }

    private static string BuildTypeTagLine(ICardScript s)
    {
        string typeText = s.CardType switch
        {
            CardType.Minion => "随从",
            CardType.Spell => "法术",
            CardType.Amulet => "护符",
            _ => "?",
        };
        string rarity = s.Rarity switch
        {
            Rarity.Bronze => "铜",
            Rarity.Silver => "银",
            Rarity.Gold => "金",
            Rarity.Legendary => "彩",
            _ => "",
        };
        string tags = s.Tags.Count > 0 ? " · " + string.Join("/", s.Tags) : "";
        return $"{typeText} · {rarity}{tags}";
    }

    private static string BuildDescription(ICardScript s, RuntimeCard card) => s.Description;

    private static string BuildKeywordLine(RuntimeCard card, ICardScript script, bool onField)
    {
        // In hand, keywords reflect the card-as-printed (script.InitialKeywords).
        // On field, they reflect the current runtime state (which can be silenced / gained later).
        var keywords = onField ? card.Keywords : script.InitialKeywords;
        var parts = new List<string>();
        if ((keywords & Keyword.Ward) == Keyword.Ward) parts.Add("【守护】");
        if ((keywords & Keyword.Rush) == Keyword.Rush) parts.Add("【突进】");
        if ((keywords & Keyword.Storm) == Keyword.Storm) parts.Add("【疾驰】");
        if ((keywords & Keyword.Barrier) == Keyword.Barrier) parts.Add("【护盾】");
        if ((keywords & Keyword.Stealth) == Keyword.Stealth) parts.Add("【潜行】");
        if (onField && card.IsEvolved) parts.Add("✦进化");
        if (onField && card.IsSilenced) parts.Add("✕沉默");
        return string.Join(" ", parts);
    }

    private static string BuildTooltip(ICardScript s, RuntimeCard card)
    {
        var lines = new List<string>
        {
            $"{s.Name}  ({s.Cost}费)",
            BuildTypeTagLine(s),
        };
        if (s.CardType == CardType.Minion)
        {
            int atk = card.Zone == Zone.Field ? card.CurrentAttack : s.BaseAttack;
            int curHp = card.Zone == Zone.Field ? card.CurrentHealth : s.BaseHealth;
            int maxHp = card.Zone == Zone.Field ? card.MaxHealth : s.BaseHealth;
            lines.Add($"⚔{atk}  ♥{curHp}/{maxHp}");
        }
        var desc = BuildDescription(s, card);
        if (!string.IsNullOrEmpty(desc)) lines.Add("");
        if (!string.IsNullOrEmpty(desc)) lines.Add(desc);
        var kw = BuildKeywordLine(card, s, onField: card.Zone == Zone.Field);
        if (!string.IsNullOrEmpty(kw)) lines.Add(kw);
        return string.Join("\n", lines);
    }

    private void ApplyAccentColors(RuntimeCard card, ICardScript script, bool onField)
    {
        // Class-tinted base border.
        var baseBorder = script.HeroClass switch
        {
            HeroClass.Vampire => new Color(0.85f, 0.25f, 0.35f),
            HeroClass.Neutral => new Color(0.55f, 0.55f, 0.65f),
            HeroClass.ClassB => new Color(0.3f, 0.7f, 0.95f),
            HeroClass.ClassC => new Color(0.35f, 0.85f, 0.45f),
            _ => new Color(0.6f, 0.5f, 0.8f),
        };

        int borderW = 2;
        var borderColor = baseBorder;

        // Look at script for hand cards (printed value), runtime card for field cards.
        var effectiveKeywords = onField ? card.Keywords : script.InitialKeywords;
        bool hasWard = (effectiveKeywords & Keyword.Ward) == Keyword.Ward;

        _stylebox.BgColor = new Color(0.18f, 0.12f, 0.22f);

        if (hasWard)
        {
            borderColor = new Color(1f, 0.85f, 0.3f);
            borderW = 5;
        }
        else if (onField && card.IsEvolved)
        {
            borderColor = new Color(1f, 0.55f, 1f);
            borderW = 4;
        }

        _stylebox.BorderColor = borderColor;
        _stylebox.BorderWidthTop = borderW;
        _stylebox.BorderWidthBottom = borderW;
        _stylebox.BorderWidthLeft = borderW;
        _stylebox.BorderWidthRight = borderW;

        Modulate = (onField && card.IsSilenced) ? new Color(0.55f, 0.55f, 0.55f) : new Color(1, 1, 1);
    }

    /// <summary>
    /// Render as an opaque card back: no text, just a generic pattern. Used for the opponent's hand in hot seat.
    /// </summary>
    public void BindFaceDown()
    {
        BuildUi();
        Instance = InstanceId.None;
        Card = CardId.None;
        IsOnField = false;

        _name.Visible = false; _cost.Visible = false; _typeTags.Visible = false;
        _description.Visible = false; _keywords.Visible = false; _stats.Visible = false;

        _stylebox.BgColor = new Color(0.12f, 0.10f, 0.18f);
        _stylebox.BorderColor = new Color(0.45f, 0.4f, 0.55f);
        _stylebox.BorderWidthTop = 3;
        _stylebox.BorderWidthBottom = 3;
        _stylebox.BorderWidthLeft = 3;
        _stylebox.BorderWidthRight = 3;
        Modulate = new Color(1, 1, 1);
        TooltipText = "对手手牌";
    }

    private void OnGuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            EmitSignal(SignalName.Clicked, Instance.Value);
    }
}
