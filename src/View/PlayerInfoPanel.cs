using Godot;
using ShadowCardSmash.Domain;

namespace ShadowCardSmash.View;

/// <summary>
/// Hero portrait area: HP, mana, deck/graveyard counts, EP, turn indicator. Also acts as the click target
/// when the player intends to attack the enemy hero.
/// </summary>
public partial class PlayerInfoPanel : PanelContainer
{
    [Signal] public delegate void HeroClickedEventHandler(int sideIndex);

    public PlayerSide Side { get; set; }

    private Label _hp = null!;
    private Label _mana = null!;
    private Label _deck = null!;
    private Label _ep = null!;
    private Label _turn = null!;
    private bool _builtUi;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(300, 70);
        BuildUi();
        GuiInput += OnGuiInput;
    }

    private void BuildUi()
    {
        if (_builtUi) return;
        _builtUi = true;
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.13f, 0.13f, 0.18f),
            BorderColor = new Color(0.4f, 0.4f, 0.5f),
            BorderWidthTop = 1, BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
        };
        AddThemeStyleboxOverride("panel", sb);

        var hb = new HBoxContainer();
        hb.AddThemeConstantOverride("separation", 16);
        AddChild(hb);

        _hp = MakeLabel("HP 40", new Color(1f, 0.55f, 0.55f));
        _mana = MakeLabel("0/0", new Color(0.5f, 0.8f, 1f));
        _deck = MakeLabel("Deck 40", new Color(0.85f, 0.85f, 0.85f));
        _ep = MakeLabel("EP 0", new Color(1f, 0.8f, 1f));
        _turn = MakeLabel("", new Color(1f, 1f, 0.6f));
        hb.AddChild(_hp); hb.AddChild(_mana); hb.AddChild(_deck); hb.AddChild(_ep); hb.AddChild(_turn);
    }

    private static Label MakeLabel(string text, Color color)
    {
        var l = new Label { Text = text, Modulate = color };
        l.AddThemeFontSizeOverride("font_size", 20);
        return l;
    }

    public void Rebind(PlayerState p, bool isMyTurn)
    {
        BuildUi();
        _hp.Text = $"HP {p.Health}/{p.MaxHealth}";
        _mana.Text = $"{p.Mana}/{p.MaxMana}";
        _deck.Text = $"Deck {p.Deck.Count} / Grave {p.Graveyard.Count}";
        _ep.Text = $"EP {p.EvolutionPoints}";
        _turn.Text = isMyTurn ? "▶ YOUR TURN" : "";
    }

    private void OnGuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            EmitSignal(SignalName.HeroClicked, (int)Side);
    }
}
