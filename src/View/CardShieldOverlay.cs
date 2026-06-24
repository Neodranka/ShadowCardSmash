using Godot;

namespace ShadowCardSmash.View;

/// <summary>
/// Cosmetic overlay layered above a CardView's face. Draws a semi-transparent yellow ellipse for Ward
/// and a semi-transparent light-blue ellipse for Barrier, only while the card is on the field.
/// Mouse events pass through.
/// </summary>
public partial class CardShieldOverlay : Control
{
    public bool ShowWard;
    public bool ShowBarrier;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AnchorRight = 1; AnchorBottom = 1;
        Resized += QueueRedraw;
    }

    public void Refresh(bool ward, bool barrier)
    {
        ShowWard = ward;
        ShowBarrier = barrier;
        Visible = ward || barrier;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!Visible) return;
        var size = Size;
        if (size.X < 10 || size.Y < 10) return;
        var center = size / 2;
        var outer = new Vector2(size.X * 0.47f, size.Y * 0.48f);

        // Yellow Ward ring (outer).
        if (ShowWard)
            DrawEllipseFilled(center, outer, new Color(1f, 0.88f, 0.30f, 0.28f));
        // Light-blue Barrier ring (inner) — drawn on top so it's visible regardless of Ward.
        if (ShowBarrier)
            DrawEllipseFilled(center, outer * 0.78f, new Color(0.50f, 0.85f, 1.00f, 0.38f));
    }

    private void DrawEllipseFilled(Vector2 center, Vector2 radius, Color color)
    {
        const int segments = 40;
        var pts = new Vector2[segments];
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)segments * Mathf.Tau;
            pts[i] = center + new Vector2(Mathf.Cos(t) * radius.X, Mathf.Sin(t) * radius.Y);
        }
        DrawColoredPolygon(pts, color);
    }
}
