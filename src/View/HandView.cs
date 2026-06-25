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
        if (_cardsCache.Count == 0) return;

        int cardW = CardView.CardWidth;
        // Peek: heavy overlap; Full: separate with small gap.
        int step = Mode == DisplayMode.Peek
            ? (int)(cardW * 0.32f)
            : cardW + 12;
        int totalWidth = (_cardsCache.Count - 1) * step + cardW;
        float startX = Math.Max(0f, ((float)Size.X - totalWidth) / 2f);

        for (int i = 0; i < _cardsCache.Count; i++)
        {
            var card = _cardsCache[i];
            var cv = new CardView();
            AddChild(cv);

            if (ShowFaces) cv.Bind(card, _dbCache.Get(card.Card), onField: false, viewerMana: _manaCache);
            else cv.BindFaceDown();

            cv.Position = new Vector2(startX + i * step, 0);
            cv.ZIndex = i; // rightmost on top

            int captured = card.Instance.Value;
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
            }
        }
    }

    private void OnStripBackgroundInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            EmitSignal(SignalName.StripClicked, (int)Side);
    }
}
