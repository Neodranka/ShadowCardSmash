using System.Reflection;
using Godot;
using ShadowCardSmash.Cards;
using ShadowCardSmash.Cards.Resources;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;
using ShadowCardSmash.View;

namespace ShadowCardSmash.App;

/// <summary>
/// Pre-battle scene. Two columns (先手 / 后手), each picks a deck. Built-in decks and any user-saved deck show up.
/// "开始对战" stores selections in <see cref="BattleSetup"/> and switches to Battle.tscn.
/// </summary>
public partial class DeckSelectionController : Control
{
    private CardRegistry _registry = null!;
    private List<DeckStorage.DeckEntry> _allDecks = null!;

    private OptionButton _p1Dropdown = null!;
    private OptionButton _p2Dropdown = null!;
    private VBoxContainer _p1Preview = null!;
    private VBoxContainer _p2Preview = null!;
    private Label _p1Summary = null!;
    private Label _p2Summary = null!;
    private Button _startBtn = null!;
    private Label _statusLabel = null!;

    public override void _Ready()
    {
        AnchorRight = 1; AnchorBottom = 1;
        _registry = CardRegistry.ScanAssembly(Assembly.GetExecutingAssembly());
        CardResourceLoader.AttachAll(_registry);
        _allDecks = DeckStorage.ListAllDecks();

        BuildUi();
        PopulateDropdowns();
        if (_allDecks.Count > 0)
        {
            _p1Dropdown.Select(0);
            _p2Dropdown.Select(Math.Min(1, _allDecks.Count - 1));
        }
        RefreshPreviews();
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true, Keycode: Key.Escape })
        {
            OnBack();
            GetViewport().SetInputAsHandled();
        }
    }

    private void BuildUi()
    {
        const int Margin = 32;
        var root = new VBoxContainer
        {
            AnchorRight = 1, AnchorBottom = 1,
            OffsetLeft = Margin, OffsetTop = Margin, OffsetRight = -Margin, OffsetBottom = -Margin,
        };
        root.AddThemeConstantOverride("separation", 16);
        AddChild(root);

        // Title row
        var titleRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        titleRow.AddThemeConstantOverride("separation", 16);
        root.AddChild(titleRow);

        var back = new Button { Text = "← 返回", CustomMinimumSize = new Vector2(120, 40) };
        back.Pressed += OnBack;
        titleRow.AddChild(back);

        var title = new Label
        {
            Text = "选择卡组",
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        title.AddThemeFontSizeOverride("font_size", 28);
        title.Modulate = new Color(1f, 0.9f, 0.6f);
        titleRow.AddChild(title);

        // Spacer to balance the back button
        titleRow.AddChild(new Control { CustomMinimumSize = new Vector2(120, 0) });

        _statusLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _statusLabel.AddThemeFontSizeOverride("font_size", 14);
        _statusLabel.Modulate = new Color(0.95f, 0.75f, 0.4f);
        root.AddChild(_statusLabel);

        // Two columns
        var columnsRow = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        columnsRow.AddThemeConstantOverride("separation", 24);
        root.AddChild(columnsRow);

        (var p1Col, _p1Dropdown, _p1Preview, _p1Summary) = BuildPlayerColumn("先手玩家 (P1)", new Color(0.55f, 0.85f, 1f));
        (var p2Col, _p2Dropdown, _p2Preview, _p2Summary) = BuildPlayerColumn("后手玩家 (P2)", new Color(1f, 0.65f, 0.55f));
        columnsRow.AddChild(p1Col);
        columnsRow.AddChild(p2Col);

        _p1Dropdown.ItemSelected += _ => RefreshPreviews();
        _p2Dropdown.ItemSelected += _ => RefreshPreviews();

        // Start button row
        var startRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        root.AddChild(startRow);
        startRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        _startBtn = new Button { Text = "开始对战", CustomMinimumSize = new Vector2(220, 56) };
        _startBtn.Pressed += OnStart;
        startRow.AddChild(_startBtn);
        startRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
    }

    private (PanelContainer panel, OptionButton dropdown, VBoxContainer preview, Label summary) BuildPlayerColumn(string title, Color accentColor)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.08f, 0.14f),
            BorderColor = accentColor,
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 12, ContentMarginBottom = 12,
        };
        panel.AddThemeStyleboxOverride("panel", sb);

        var vb = new VBoxContainer();
        vb.AddThemeConstantOverride("separation", 8);
        panel.AddChild(vb);

        var header = new Label { Text = title, HorizontalAlignment = HorizontalAlignment.Center, Modulate = accentColor };
        header.AddThemeFontSizeOverride("font_size", 20);
        vb.AddChild(header);

        var dropdown = new OptionButton { CustomMinimumSize = new Vector2(0, 36) };
        vb.AddChild(dropdown);

        var summary = new Label { HorizontalAlignment = HorizontalAlignment.Left };
        summary.AddThemeFontSizeOverride("font_size", 13);
        vb.AddChild(summary);

        vb.AddChild(new HSeparator());

        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        vb.AddChild(scroll);
        var preview = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        preview.AddThemeConstantOverride("separation", 2);
        scroll.AddChild(preview);

        return (panel, dropdown, preview, summary);
    }

    private void PopulateDropdowns()
    {
        _p1Dropdown.Clear();
        _p2Dropdown.Clear();
        if (_allDecks.Count == 0)
        {
            _p1Dropdown.AddItem("(没有可用卡组)");
            _p2Dropdown.AddItem("(没有可用卡组)");
            _statusLabel.Text = "没有可用卡组 — 先去 Deck Builder 建一个，或者带上预制套牌";
            return;
        }
        foreach (var entry in _allDecks)
        {
            _p1Dropdown.AddItem(entry.DisplayName);
            _p2Dropdown.AddItem(entry.DisplayName);
        }
    }

    private void RefreshPreviews()
    {
        RenderPreview(_p1Dropdown, _p1Preview, _p1Summary);
        RenderPreview(_p2Dropdown, _p2Preview, _p2Summary);
    }

    private void RenderPreview(OptionButton dropdown, VBoxContainer preview, Label summary)
    {
        for (int i = preview.GetChildCount() - 1; i >= 0; i--)
        {
            var c = preview.GetChild(i);
            preview.RemoveChild(c);
            c.QueueFree();
        }

        int idx = dropdown.Selected;
        if (idx < 0 || idx >= _allDecks.Count) { summary.Text = ""; return; }
        var entry = _allDecks[idx];
        var deck = entry.Resource;

        string className = HeroClassName(deck.HeroClassEnum);
        summary.Text = $"{className} · {deck.CardIds.Length} 张";

        var counts = new Dictionary<int, int>();
        foreach (var cid in deck.CardIds)
        {
            counts.TryGetValue(cid, out int n);
            counts[cid] = n + 1;
        }

        foreach (var kv in counts.OrderBy(kv =>
        {
            return _registry.TryGet(new CardId(kv.Key), out var s) ? s.Cost : 99;
        }).ThenBy(kv => kv.Key))
        {
            if (!_registry.TryGet(new CardId(kv.Key), out var script)) continue;
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);
            var cost = new Label { Text = script.Cost.ToString(), CustomMinimumSize = new Vector2(28, 0), HorizontalAlignment = HorizontalAlignment.Right };
            cost.Modulate = new Color(0.55f, 0.85f, 1f);
            row.AddChild(cost);
            var name = new Label { Text = script.Name, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddChild(name);
            var count = new Label { Text = $"×{kv.Value}", CustomMinimumSize = new Vector2(36, 0), HorizontalAlignment = HorizontalAlignment.Right };
            count.Modulate = new Color(1f, 0.9f, 0.4f);
            row.AddChild(count);
            preview.AddChild(row);
        }
    }

    private void OnStart()
    {
        if (_allDecks.Count == 0) { _statusLabel.Text = "无可用卡组"; return; }
        int p1 = _p1Dropdown.Selected;
        int p2 = _p2Dropdown.Selected;
        if (p1 < 0 || p2 < 0) { _statusLabel.Text = "请为双方选择卡组"; return; }
        BattleSetup.Player1Deck = _allDecks[p1].Resource;
        BattleSetup.Player2Deck = _allDecks[p2].Resource;
        BattleSetup.Seed = (int)Time.GetTicksMsec();
        GetTree().ChangeSceneToFile("res://scenes/Battle.tscn");
    }

    private void OnBack() => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");

    private static string HeroClassName(HeroClass c) => c switch
    {
        HeroClass.Neutral => "中立",
        HeroClass.Forsaken => "弃绝者",
        HeroClass.Empire => "帝国",
        HeroClass.ClassC => "职业C",
        HeroClass.ClassD => "职业D",
        HeroClass.ClassE => "职业E",
        HeroClass.ClassF => "职业F",
        HeroClass.ClassG => "职业G",
        _ => "?",
    };
}
