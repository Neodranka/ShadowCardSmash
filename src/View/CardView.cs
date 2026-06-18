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
    public const int CardHeight = 210;

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

        _name.Text = script.Name;
        _cost.Text = script.Cost.ToString();
        _typeTags.Text = BuildTypeTagLine(script);
        _description.Text = BuildDescription(script, card);
        _keywords.Text = BuildKeywordLine(card);

        if (script.CardType == CardType.Minion)
        {
            _stats.Text = $"⚔ {card.CurrentAttack}    ♥ {card.CurrentHealth}";
            _stats.Visible = true;
        }
        else if (script.CardType == CardType.Amulet)
        {
            _stats.Text = card.Countdown >= 0 ? $"⏳ {card.Countdown}" : "";
            _stats.Visible = card.Countdown >= 0;
        }
        else
        {
            _stats.Text = "";
            _stats.Visible = false;
        }

        ApplyAccentColors(card, script);
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

    private static string BuildKeywordLine(RuntimeCard card)
    {
        var parts = new List<string>();
        if (card.HasKeyword(Keyword.Ward)) parts.Add("【守护】");
        if (card.HasKeyword(Keyword.Rush)) parts.Add("【突进】");
        if (card.HasKeyword(Keyword.Storm)) parts.Add("【疾驰】");
        if (card.HasKeyword(Keyword.Barrier)) parts.Add("【护盾】");
        if (card.HasKeyword(Keyword.Stealth)) parts.Add("【潜行】");
        if (card.IsEvolved) parts.Add("✦进化");
        if (card.IsSilenced) parts.Add("✕沉默");
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
            lines.Add($"⚔{card.CurrentAttack}  ♥{card.CurrentHealth}/{card.MaxHealth}");
        var desc = BuildDescription(s, card);
        if (!string.IsNullOrEmpty(desc)) lines.Add("");
        if (!string.IsNullOrEmpty(desc)) lines.Add(desc);
        var kw = BuildKeywordLine(card);
        if (!string.IsNullOrEmpty(kw)) lines.Add(kw);
        return string.Join("\n", lines);
    }

    private void ApplyAccentColors(RuntimeCard card, ICardScript script)
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

        // Ward gets a thick gold border so it's hard to miss on the field.
        if (card.HasKeyword(Keyword.Ward))
        {
            borderColor = new Color(1f, 0.85f, 0.3f);
            borderW = 5;
        }
        else if (card.IsEvolved)
        {
            borderColor = new Color(1f, 0.55f, 1f);
            borderW = 4;
        }

        _stylebox.BorderColor = borderColor;
        _stylebox.BorderWidthTop = borderW;
        _stylebox.BorderWidthBottom = borderW;
        _stylebox.BorderWidthLeft = borderW;
        _stylebox.BorderWidthRight = borderW;

        Modulate = card.IsSilenced ? new Color(0.55f, 0.55f, 0.55f) : new Color(1, 1, 1);
    }

    private void OnGuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            EmitSignal(SignalName.Clicked, Instance.Value);
    }
}
