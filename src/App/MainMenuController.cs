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
        AddButton(vb, "Multiplayer (联机)", OnMultiplayer);
        if (PersistedNetSession.Exists())
        {
            var resumeRow = new HBoxContainer();
            resumeRow.AddThemeConstantOverride("separation", 6);
            vb.AddChild(resumeRow);

            var resumeBtn = new Button
            {
                Text = "重连上一局 (Resume)",
                CustomMinimumSize = new Vector2(230, 56),
                Modulate = new Color(0.75f, 1.0f, 0.75f),
            };
            resumeBtn.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/NetResume.tscn");
            resumeRow.AddChild(resumeBtn);

            // Manual dismiss: for cases where the host is definitively gone and the user doesn't want
            // to wait for the 5s connect-timeout to auto-clear.
            var dismissBtn = new Button
            {
                Text = "×",
                CustomMinimumSize = new Vector2(44, 56),
                Modulate = new Color(1.0f, 0.7f, 0.7f),
                TooltipText = "清除续接数据（不会通知对方，只删本地文件）",
            };
            dismissBtn.Pressed += () =>
            {
                PersistedNetSession.Clear();
                // Reload menu so the resume row disappears.
                GetTree().ReloadCurrentScene();
            };
            resumeRow.AddChild(dismissBtn);
        }
        AddButton(vb, "Deck Builder", OnDeckBuilder);
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
        GetTree().ChangeSceneToFile("res://scenes/DeckSelection.tscn");
    }

    private void OnDeckBuilder()
    {
        GetTree().ChangeSceneToFile("res://scenes/DeckBuilder.tscn");
    }

    private void OnMultiplayer()
    {
        GetTree().ChangeSceneToFile("res://scenes/MultiplayerLobby.tscn");
    }
}
