using Godot;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.View;

/// <summary>
/// Player hand display. Two display modes:
///   • Peek: a short strip showing only the top portion of overlapping cards. Clicking any card
///     (or the strip area) emits <see cref="StripClickedEventHandler"/> so the controller can open a full popup.
///   • Full: cards laid out side-by-side. Click/hover events drive the normal play flow.
/// Switch via <see cref="Mode"/> before adding to scene.
/// </summary>
public partial class HandView : Control
{
    public enum DisplayMode { Full, Peek }

    public const int PeekStripHeight = 84;        // height of the visible "head" strip

    [Signal] public delegate void CardSelectedEventHandler(int instanceId);
    [Signal] public delegate void CardHoveredEventHandler(int instanceId);
    [Signal] public delegate void CardHoverExitedEventHandler(int instanceId);
    [Signal] public delegate void StripClickedEventHandler(int sideIndex);

    public PlayerSide Side { get; set; }
    public bool ShowFaces { get; set; } = true;
    public DisplayMode Mode { get; set; } = DisplayMode.Full;

    private List<RuntimeCard> _cardsCache = new();
    private ICardDatabase? _dbCache;
    private int _manaCache;

    // Full-mode visual constants — kept here so tuning is one place.
    private const float FanAngleDeg = 10f;            // ± spread between leftmost and rightmost card
    private const float HoverLift = 30f;              // px upward on hover
    private const float HoverTweenSec = 0.12f;
    private const int HoverZIndex = 100;

    // One running tween per card (keyed by instance id) so spamming hover doesn't stack tweens.
    private readonly Dictionary<ulong, Tween> _hoverTweens = new();

    public override void _Ready()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        if (Mode == DisplayMode.Peek)
        {
            CustomMinimumSize = new Vector2(0, PeekStripHeight);
            ClipContents = true;
            // Clickable background for cases where the user clicks between/around cards.
            MouseFilter = MouseFilterEnum.Stop;
            GuiInput += OnStripBackgroundInput;
        }
        Resized += DoLayout;
    }

    public void Rebind(IReadOnlyList<RuntimeCard> hand, ICardDatabase db, int viewerMana = 0)
    {
        _cardsCache = hand.ToList();
        _dbCache = db;
        _manaCache = viewerMana;
        DoLayout();
    }

    private void DoLayout()
    {
        if (_dbCache is null) return;

        for (int i = GetChildCount() - 1; i >= 0; i--)
        {
            var c = GetChild(i);
            RemoveChild(c);
            c.QueueFree();
        }
        _hoverTweens.Clear();
        if (_cardsCache.Count == 0) return;

        int cardW = CardView.CardWidth;
        int cardH = CardView.CardHeight;
        // Peek: heavy overlap; Full: wider step so rotated corners don't crowd.
        int step = Mode == DisplayMode.Peek
            ? (int)(cardW * 0.32f)
            : cardW + 24;
        int totalWidth = (_cardsCache.Count - 1) * step + cardW;
        float startX = Math.Max(0f, ((float)Size.X - totalWidth) / 2f);

        bool fan = Mode == DisplayMode.Full && _cardsCache.Count > 1;

        for (int i = 0; i < _cardsCache.Count; i++)
        {
            var card = _cardsCache[i];
            var cv = new CardView();
            AddChild(cv);

            if (ShowFaces) cv.Bind(card, _dbCache.Get(card.Card), onField: false, viewerMana: _manaCache);
            else cv.BindFaceDown();

            // Rotate around card center so the visual "splay" pivots cleanly.
            cv.PivotOffset = new Vector2(cardW / 2f, cardH / 2f);
            var basePos = new Vector2(startX + i * step, 0);
            cv.Position = basePos;
            cv.ZIndex = i; // rightmost on top
            int baseZ = i;

            float baseRotRad = 0f;
            if (fan)
            {
                float t = (float)i / (_cardsCache.Count - 1);
                baseRotRad = Mathf.DegToRad(Mathf.Lerp(-FanAngleDeg, FanAngleDeg, t));
            }
            cv.Rotation = baseRotRad;

            if (Mode == DisplayMode.Peek)
            {
                // Any click in peek mode → ask the controller to expand the popup.
                int sideIdx = (int)Side;
                cv.Clicked += _ => EmitSignal(SignalName.StripClicked, sideIdx);
                // Hover preview is skipped in peek mode (cards are too cropped).
            }
            else
            {
                cv.Clicked += iid => EmitSignal(SignalName.CardSelected, iid);
                cv.HoverEntered += iid => EmitSignal(SignalName.CardHovered, iid);
                cv.HoverExited += iid => EmitSignal(SignalName.CardHoverExited, iid);
                // Hover lift: rise + straighten + raise Z. Capture base in closures.
                var capturedCv = cv;
                var capturedBasePos = basePos;
                var capturedBaseRot = baseRotRad;
                int capturedBaseZ = baseZ;
                cv.MouseEntered += () => TweenHover(capturedCv, capturedBasePos + new Vector2(0, -HoverLift), 0f, HoverZIndex);
                cv.MouseExited  += () => TweenHover(capturedCv, capturedBasePos, capturedBaseRot, capturedBaseZ);
            }
        }
    }

    private void TweenHover(CardView cv, Vector2 targetPos, float targetRot, int targetZ)
    {
        var id = cv.GetInstanceId();
        if (_hoverTweens.TryGetValue(id, out var prev) && GodotObject.IsInstanceValid(prev))
            prev.Kill();
        var t = cv.CreateTween();
        t.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        t.TweenProperty(cv, "position", targetPos, HoverTweenSec);
        t.Parallel().TweenProperty(cv, "rotation", targetRot, HoverTweenSec);
        _hoverTweens[id] = t;
        cv.ZIndex = targetZ;
    }

    private void OnStripBackgroundInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            EmitSignal(SignalName.StripClicked, (int)Side);
    }
}
