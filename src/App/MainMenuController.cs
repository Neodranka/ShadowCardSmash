using Godot;

namespace ShadowCardSmash.App;

public partial class MainMenuController : Control
{
    public override void _Ready()
    {
        AnchorRight = 1; AnchorBottom = 1;

        var center = new CenterContainer { AnchorRight = 1, AnchorBottom = 1 };
        AddChild(center);

        var vb = new VBoxContainer();
        vb.AddThemeConstantOverride("separation", 16);
        center.AddChild(vb);

        var title = new Label
        {
            Text = "ShadowCardSmash",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 64);
        title.Modulate = new Color(0.9f, 0.8f, 1.0f);
        vb.AddChild(title);

        var subtitle = new Label
        {
            Text = "(Godot rewrite — V1 hot seat)",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        subtitle.Modulate = new Color(0.6f, 0.6f, 0.7f);
        vb.AddChild(subtitle);

        vb.AddChild(new Control { CustomMinimumSize = new Vector2(0, 32) });

        AddButton(vb, "Hot Seat", OnHotSeat);
        AddButton(vb, "Multiplayer (TODO)", () => { }, disabled: true);
        AddButton(vb, "Deck Builder (TODO)", () => { }, disabled: true);
        AddButton(vb, "Quit", () => GetTree().Quit());
    }

    private static void AddButton(Container parent, string text, System.Action onPressed, bool disabled = false)
    {
        var b = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(280, 56),
            Disabled = disabled,
        };
        b.Pressed += onPressed;
        parent.AddChild(b);
    }

    private void OnHotSeat()
    {
        GetTree().ChangeSceneToFile("res://scenes/Battle.tscn");
    }
}
