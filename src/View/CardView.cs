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
    public const int CardWidth = 140;
    public const int CardHeight = 200;

    [Signal] public delegate void ClickedEventHandler(int instanceId);

    public InstanceId Instance { get; private set; }
    public CardId Card { get; private set; }
    public bool IsOnField { get; private set; }

    private Label _name = null!;
    private Label _cost = null!;
    private Label _description = null!;
    private Label _stats = null!;
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

        var stylebox = new StyleBoxFlat
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
        };
        AddThemeStyleboxOverride("panel", stylebox);

        var vb = new VBoxContainer { CustomMinimumSize = new Vector2(CardWidth - 8, CardHeight - 8) };
        vb.AddThemeConstantOverride("separation", 4);
        AddChild(vb);

        var headerRow = new HBoxContainer();
        vb.AddChild(headerRow);
        _cost = new Label { Text = "0", Modulate = new Color(0.6f, 0.8f, 1f) };
        headerRow.AddChild(_cost);
        _name = new Label
        {
            Text = "Card",
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        headerRow.AddChild(_name);

        _description = new Label
        {
            Text = "",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(CardWidth - 16, 0),
        };
        _description.AddThemeFontSizeOverride("font_size", 11);
        vb.AddChild(_description);

        _stats = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Right,
            Modulate = new Color(1f, 0.85f, 0.4f),
        };
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
        _description.Text = onField ? "" : DescribeScript(script);

        if (script.CardType == CardType.Minion)
        {
            _stats.Text = $"{card.CurrentAttack}/{card.CurrentHealth}";
            _stats.Visible = true;
        }
        else if (script.CardType == CardType.Amulet)
        {
            _stats.Text = card.Countdown >= 0 ? $"⏳{card.Countdown}" : "";
            _stats.Visible = card.Countdown >= 0;
        }
        else
        {
            _stats.Text = "";
            _stats.Visible = false;
        }

        if (card.HasKeyword(Keyword.Ward))
            Modulate = new Color(1f, 1f, 0.6f);
        else if (card.IsEvolved)
            Modulate = new Color(1f, 0.7f, 1f);
        else
            Modulate = new Color(1, 1, 1);
    }

    private static string DescribeScript(ICardScript s)
    {
        var tags = s.Tags.Count > 0 ? "[" + string.Join(",", s.Tags) + "] " : "";
        return $"{tags}{s.CardType} {s.Rarity}";
    }

    private void OnGuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            EmitSignal(SignalName.Clicked, Instance.Value);
    }
}
