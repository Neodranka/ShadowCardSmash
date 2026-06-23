using Godot;

namespace ShadowCardSmash.View;

/// <summary>
/// Floating "-N" label that drifts upward and fades out, then frees itself.
/// Spawn via <see cref="Spawn"/>; the caller is responsible only for parent + world coordinate.
/// </summary>
public partial class DamageNumber : Label
{
    public static DamageNumber Spawn(Control parent, Vector2 globalPos, string text, Color color, int fontSize = 36)
    {
        var lbl = new DamageNumber
        {
            Text = text,
            Modulate = color,
            ZIndex = 100,
        };
        lbl.AddThemeFontSizeOverride("font_size", fontSize);
        // Add to outer overlay so the number renders above the board layout.
        parent.AddChild(lbl);
        // Center on the requested world point.
        lbl.GlobalPosition = globalPos - lbl.Size / 2;
        lbl.CallDeferred(MethodName.PlayAfterLayout);
        return lbl;
    }

    private void PlayAfterLayout()
    {
        // After Godot completes its layout pass we know our Size, so we can recenter precisely.
        GlobalPosition -= Size / 2;
        var endPos = GlobalPosition + new Vector2(0, -70);
        var tween = CreateTween().SetParallel();
        tween.TweenProperty(this, "global_position", endPos, 0.7).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "modulate:a", 0.0, 0.7).SetDelay(0.15);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }
}
