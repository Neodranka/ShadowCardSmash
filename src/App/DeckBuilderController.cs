using System.Reflection;
using Godot;
using ShadowCardSmash.Cards;
using ShadowCardSmash.Cards.Resources;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;
using ShadowCardSmash.View;

namespace ShadowCardSmash.App;

/// <summary>
/// Deck builder scene controller. Layout:
///   [TopBar: ← Back | Class ▾ | Deck name | Cards x/40 | Save]
///   [Left: scrollable card grid filtered by class] [Right: current deck list with -1 buttons]
/// </summary>
public partial class DeckBuilderController : Control
{
    private CardRegistry _registry = null!;
    private readonly Dictionary<CardId, int> _deckCounts = new();
    private HeroClass _selectedClass = HeroClass.Forsaken;
    private string? _loadedDeckName;

    private LineEdit _nameInput = null!;
    private OptionButton _classDropdown = null!;
    private Label _countLabel = null!;
    private Label _statusLabel = null!;
    private OptionButton _loadDropdown = null!;
    private GridContainer _cardGrid = null!;
    private VBoxContainer _deckList = null!;
    private CardDetailPanel _detailPanel = null!;

    public override void _Ready()
    {
        AnchorRight = 1; AnchorBottom = 1;
        _registry = CardRegistry.ScanAssembly(Assembly.GetExecutingAssembly());
        CardResourceLoader.AttachAll(_registry);
        BuildUi();
        RefreshLoadDropdown();
        RefreshCollection();
        RefreshDeckList();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
        {
            OnBack();
            GetViewport().SetInputAsHandled();
        }
    }

    private void BuildUi()
    {
        const int Margin = 24;
        var root = new VBoxContainer
        {
            AnchorRight = 1, AnchorBottom = 1,
            OffsetLeft = Margin, OffsetTop = Margin,
            OffsetRight = -Margin, OffsetBottom = -Margin,
        };
        root.AddThemeConstantOverride("separation", 12);
        AddChild(root);

        // === Top bar ===
        var topBar = new HBoxContainer();
        topBar.AddThemeConstantOverride("separation", 12);
        root.AddChild(topBar);

        var backBtn = new Button { Text = "← 返回主菜单", CustomMinimumSize = new Vector2(160, 40) };
        backBtn.Pressed += OnBack;
        topBar.AddChild(backBtn);

        topBar.AddChild(MakeLabel("职业:"));
        _classDropdown = new OptionButton { CustomMinimumSize = new Vector2(140, 40) };
        foreach (var name in System.Enum.GetNames(typeof(HeroClass)))
            _classDropdown.AddItem(name);
        _classDropdown.Select((int)_selectedClass);
        _classDropdown.ItemSelected += OnClassChanged;
        topBar.AddChild(_classDropdown);

        topBar.AddChild(MakeLabel("卡组名:"));
        _nameInput = new LineEdit
        {
            Text = "新卡组",
            CustomMinimumSize = new Vector2(220, 40),
            SizeFlagsHorizontal = SizeFlags.Fill,
        };
        topBar.AddChild(_nameInput);

        _countLabel = MakeLabel("0/40");
        _countLabel.CustomMinimumSize = new Vector2(80, 40);
        topBar.AddChild(_countLabel);

        var saveBtn = new Button { Text = "保存", CustomMinimumSize = new Vector2(100, 40) };
        saveBtn.Pressed += OnSave;
        topBar.AddChild(saveBtn);

        topBar.AddChild(new VSeparator());
        topBar.AddChild(MakeLabel("已存卡组:"));
        _loadDropdown = new OptionButton { CustomMinimumSize = new Vector2(180, 40) };
        topBar.AddChild(_loadDropdown);
        var loadBtn = new Button { Text = "读取", CustomMinimumSize = new Vector2(80, 40) };
        loadBtn.Pressed += OnLoad;
        topBar.AddChild(loadBtn);

        // === Status line ===
        _statusLabel = MakeLabel("");
        _statusLabel.Modulate = new Color(1f, 0.85f, 0.5f);
        root.AddChild(_statusLabel);

        // === Main area: collection (left) + deck list (right) ===
        var mainRow = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        mainRow.AddThemeConstantOverride("separation", 16);
        root.AddChild(mainRow);

        // Left: card grid in a scroll container.
        var leftPanel = MakeBoxedPanel(new Color(0.10f, 0.08f, 0.14f));
        leftPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        leftPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        mainRow.AddChild(leftPanel);

        var leftScroll = new ScrollContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        leftPanel.AddChild(leftScroll);
        _cardGrid = new GridContainer { Columns = 6 };
        _cardGrid.AddThemeConstantOverride("h_separation", 12);
        _cardGrid.AddThemeConstantOverride("v_separation", 12);
        leftScroll.AddChild(_cardGrid);

        // Right: deck contents.
        var rightPanel = MakeBoxedPanel(new Color(0.08f, 0.10f, 0.16f));
        rightPanel.CustomMinimumSize = new Vector2(380, 0);
        rightPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        mainRow.AddChild(rightPanel);

        var rightScroll = new ScrollContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        rightPanel.AddChild(rightScroll);
        _deckList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _deckList.AddThemeConstantOverride("separation", 4);
        rightScroll.AddChild(_deckList);

        // Detail panel anchored to top-right of screen (above everything via TopLevel + Z).
        _detailPanel = new CardDetailPanel();
        AddChild(_detailPanel);
    }

    private void RefreshCollection()
    {
        for (int i = _cardGrid.GetChildCount() - 1; i >= 0; i--)
        {
            var c = _cardGrid.GetChild(i);
            _cardGrid.RemoveChild(c);
            c.QueueFree();
        }

        foreach (var script in _registry.All().OrderBy(s => s.Cost).ThenBy(s => s.Id.Value))
        {
            // Only show this class + neutral cards.
            if (script.HeroClass != HeroClass.Neutral && script.HeroClass != _selectedClass) continue;

            var fake = new RuntimeCard { Card = script.Id };
            var cv = new CardView();
            _cardGrid.AddChild(cv);
            cv.Bind(fake, script, onField: false);
            cv.Clicked += _ => OnAddCard(script.Id);
            cv.HoverEntered += _ => _detailPanel.ShowFor(fake, script, onField: false, pin: false);
            cv.HoverExited += _ => _detailPanel.HoverHide();
        }
    }

    private void RefreshDeckList()
    {
        for (int i = _deckList.GetChildCount() - 1; i >= 0; i--)
        {
            var c = _deckList.GetChild(i);
            _deckList.RemoveChild(c);
            c.QueueFree();
        }

        int total = 0;
        foreach (var entry in _deckCounts.OrderBy(kv => _registry.Get(kv.Key).Cost).ThenBy(kv => kv.Key.Value))
        {
            total += entry.Value;
            var script = _registry.Get(entry.Key);

            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddThemeConstantOverride("separation", 8);

            var costLabel = new Label { Text = script.Cost.ToString(), CustomMinimumSize = new Vector2(28, 0), HorizontalAlignment = HorizontalAlignment.Right };
            costLabel.Modulate = new Color(0.55f, 0.85f, 1f);
            row.AddChild(costLabel);

            var nameLabel = new Label
            {
                Text = $"{script.Name}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            row.AddChild(nameLabel);

            var countLabel = new Label { Text = $"×{entry.Value}", CustomMinimumSize = new Vector2(40, 0), HorizontalAlignment = HorizontalAlignment.Right };
            countLabel.Modulate = new Color(1f, 0.9f, 0.4f);
            row.AddChild(countLabel);

            var minus = new Button { Text = "-", CustomMinimumSize = new Vector2(30, 26) };
            var capturedId = entry.Key;
            minus.Pressed += () => OnRemoveCard(capturedId);
            row.AddChild(minus);

            _deckList.AddChild(row);
        }

        _countLabel.Text = $"{total}/40";
        _countLabel.Modulate = total == 40 ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 1f, 1f);
    }

    private void OnAddCard(CardId id)
    {
        _deckCounts.TryGetValue(id, out int n);
        if (n >= DeckValidator.MaxCopiesPerCard)
        {
            SetStatus($"{_registry.Get(id).Name} 已达 {DeckValidator.MaxCopiesPerCard} 张上限");
            return;
        }
        int total = _deckCounts.Values.Sum();
        if (total >= DeckValidator.DeckSize)
        {
            SetStatus($"卡组已满 ({DeckValidator.DeckSize} 张)");
            return;
        }
        _deckCounts[id] = n + 1;
        SetStatus("");
        RefreshDeckList();
    }

    private void OnRemoveCard(CardId id)
    {
        if (!_deckCounts.TryGetValue(id, out int n)) return;
        if (n <= 1) _deckCounts.Remove(id);
        else _deckCounts[id] = n - 1;
        RefreshDeckList();
    }

    private void OnClassChanged(long index)
    {
        _selectedClass = (HeroClass)index;
        RefreshCollection();
    }

    private void OnSave()
    {
        var name = _nameInput.Text.Trim();
        if (string.IsNullOrEmpty(name)) { SetStatus("请输入卡组名"); return; }

        var cardList = new List<int>();
        foreach (var kv in _deckCounts)
            for (int i = 0; i < kv.Value; i++) cardList.Add(kv.Key.Value);

        var cardIds = cardList.Select(i => new CardId(i)).ToArray();
        var validation = DeckValidator.Validate(cardIds, _selectedClass, _registry);
        if (!validation.IsValid)
        {
            SetStatus($"非法卡组：{validation.Reason}");
            return;
        }

        var deck = new DeckResource
        {
            DeckName = name,
            HeroClassEnum = _selectedClass,
            CardIds = cardList.ToArray(),
        };
        var err = DeckStorage.Save(deck);
        if (err != Error.Ok) { SetStatus($"保存失败: {err}"); return; }
        _loadedDeckName = name;
        SetStatus($"已保存「{name}」");
        RefreshLoadDropdown();
    }

    private void OnLoad()
    {
        if (_loadDropdown.Selected < 0) { SetStatus("没有可读取的卡组"); return; }
        var name = _loadDropdown.GetItemText(_loadDropdown.Selected);
        var deck = DeckStorage.Load(name);
        if (deck is null) { SetStatus($"无法读取「{name}」"); return; }

        _deckCounts.Clear();
        foreach (var id in deck.CardIds)
        {
            var cid = new CardId(id);
            _deckCounts.TryGetValue(cid, out int n);
            _deckCounts[cid] = n + 1;
        }
        _selectedClass = deck.HeroClassEnum;
        _classDropdown.Select((int)_selectedClass);
        _nameInput.Text = deck.DeckName;
        _loadedDeckName = deck.DeckName;
        SetStatus($"已读取「{deck.DeckName}」");
        RefreshCollection();
        RefreshDeckList();
    }

    private void RefreshLoadDropdown()
    {
        _loadDropdown.Clear();
        var names = DeckStorage.ListDeckNames();
        foreach (var name in names) _loadDropdown.AddItem(name);
        if (names.Count == 0) _loadDropdown.AddItem("(空)");
    }

    private void OnBack() => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");

    private void SetStatus(string text) => _statusLabel.Text = text;

    private static Label MakeLabel(string text)
    {
        var l = new Label { Text = text, VerticalAlignment = VerticalAlignment.Center };
        l.AddThemeFontSizeOverride("font_size", 16);
        return l;
    }

    private static PanelContainer MakeBoxedPanel(Color bg)
    {
        var p = new PanelContainer();
        var sb = new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = new Color(0.4f, 0.4f, 0.55f),
            BorderWidthTop = 1, BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 8, ContentMarginBottom = 8,
        };
        p.AddThemeStyleboxOverride("panel", sb);
        return p;
    }
}
