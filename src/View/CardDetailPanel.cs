using Godot;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.View;

/// <summary>
/// Right-edge slide-out panel showing the full detail of a card the player is hovering or has selected.
/// Two display modes:
///   - Pinned: BattleController is in AwaitPlayTarget / AwaitAttackTarget — the panel reflects the selected card
///     and is not dismissed by hover-exit on other cards.
///   - Hover: showing transient preview that hides when the mouse leaves.
/// </summary>
public partial class CardDetailPanel : PanelContainer
{
    public const int PanelWidth = 320;

    private Label _name = null!;
    private Label _typeRow = null!;
    private Label _description = null!;
    private Label _keywords = null!;
    private Label _stats = null!;
    private bool _builtUi;

    public bool IsPinned { get; private set; }

    public override void _Ready()
    {
        AnchorLeft = 1; AnchorTop = 0; AnchorRight = 1; AnchorBottom = 1;
        OffsetLeft = -PanelWidth - 16; OffsetTop = 80; OffsetRight = -16; OffsetBottom = -80;
        MouseFilter = MouseFilterEnum.Ignore;
        BuildUi();
        Visible = false;

        // Float above everything: pile columns within the BoardView and PilePopup overlay outside it.
        // Absolute z (z_as_relative=false) escapes both parent and sibling order.
        TopLevel = true;
        ZIndex = 2048;
        ZAsRelative = false;
    }

    private void BuildUi()
    {
        if (_builtUi) return;
        _builtUi = true;

        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.06f, 0.12f, 0.95f),
            BorderColor = new Color(0.7f, 0.6f, 0.9f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 14, ContentMarginBottom = 14,
        };
        AddThemeStyleboxOverride("panel", sb);

        var vb = new VBoxContainer();
        vb.AddThemeConstantOverride("separation", 8);
        AddChild(vb);

        _name = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _name.AddThemeFontSizeOverride("font_size", 22);
        _name.Modulate = new Color(1f, 0.95f, 0.85f);
        vb.AddChild(_name);

        _typeRow = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _typeRow.AddThemeFontSizeOverride("font_size", 13);
        _typeRow.Modulate = new Color(0.7f, 0.7f, 0.85f);
        vb.AddChild(_typeRow);

        vb.AddChild(new HSeparator());

        _description = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _description.AddThemeFontSizeOverride("font_size", 16);
        vb.AddChild(_description);

        vb.AddChild(new HSeparator());

        _keywords = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _keywords.AddThemeFontSizeOverride("font_size", 16);
        _keywords.Modulate = new Color(1f, 0.85f, 0.3f);
        vb.AddChild(_keywords);

        _stats = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _stats.AddThemeFontSizeOverride("font_size", 18);
        _stats.Modulate = new Color(1f, 0.95f, 0.4f);
        vb.AddChild(_stats);
    }

    public void ShowFor(RuntimeCard card, ICardScript script, bool onField, bool pin)
    {
        BuildUi();
        if (IsPinned && !pin) return; // hover preview cannot replace a pinned selection.

        _name.Text = script.Name;
        _typeRow.Text = BuildTypeLine(script);
        _description.Text = string.IsNullOrEmpty(script.Description) ? "(无描述)" : script.Description;
        _keywords.Text = BuildKeywordLine(card, script, onField);
        _stats.Text = BuildStatsLine(card, script, onField);
        Visible = true;
        if (pin) IsPinned = true;
    }

    public void Unpin()
    {
        IsPinned = false;
        Visible = false;
    }

    public void HoverHide()
    {
        if (IsPinned) return;
        Visible = false;
    }

    private static string BuildTypeLine(ICardScript s)
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
        string heroClass = s.HeroClass switch
        {
            HeroClass.Neutral => "中立",
            HeroClass.Forsaken => "弃绝者",
            HeroClass.Empire => "帝国",
            HeroClass.ClassC => "职业C",
            _ => "",
        };
        string tags = s.Tags.Count > 0 ? "  ·  " + string.Join("/", s.Tags) : "";
        return $"{heroClass} · {typeText} · {rarity}{tags}";
    }

    private static string BuildKeywordLine(RuntimeCard card, ICardScript script, bool onField)
    {
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

    private static string BuildStatsLine(RuntimeCard card, ICardScript script, bool onField)
    {
        if (script.CardType == CardType.Minion)
        {
            int atk = onField ? card.CurrentAttack : script.BaseAttack;
            int hp = onField ? card.CurrentHealth : script.BaseHealth;
            int maxHp = onField ? card.MaxHealth : script.BaseHealth;
            return $"费用 {script.Cost}    攻击 {atk}    生命 {hp}/{maxHp}";
        }
        if (script.CardType == CardType.Amulet)
        {
            int cd = onField ? card.Countdown : script.InitialCountdown;
            return cd >= 0 ? $"费用 {script.Cost}    倒计时 {cd}" : $"费用 {script.Cost}";
        }
        return $"费用 {script.Cost}";
    }
}
