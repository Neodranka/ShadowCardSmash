using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// Per-viewer filtering for <see cref="BoardEvent"/>s sent over the wire to a remote peer.
/// Mirror of <see cref="GameState.FilterFor"/> for the event stream — together they ensure the
/// client never sees the CardId of any card that is still in opponent private zones (hand/deck).
///
/// Most events are public (field/graveyard moves, attacks, damage) and pass through unchanged.
/// Only events that would reveal a still-hidden card need masking:
///   • <see cref="CardDrawnEvent"/> for the opponent — they drew "a" card; mask its identity.
///
/// Events that involve a card transitioning OUT of a private zone (e.g., CardPlayed, MinionSummoned)
/// are public: by the time the event fires the card is on field / in graveyard, fully visible.
/// </summary>
public static class EventFilter
{
    public static BoardEvent FilterFor(BoardEvent evt, PlayerSide viewer) => evt switch
    {
        CardDrawnEvent d when d.Side != viewer => d with { Card = CardId.Hidden },
        _ => evt,
    };

    public static BoardEvent[] FilterAll(BoardEvent[] events, PlayerSide viewer)
    {
        var result = new BoardEvent[events.Length];
        for (int i = 0; i < events.Length; i++)
            result[i] = FilterFor(events[i], viewer);
        return result;
    }
}
