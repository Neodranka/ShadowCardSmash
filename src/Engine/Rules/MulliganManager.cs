using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// Replaces a chosen subset of starting-hand cards with fresh draws from the deck.
/// Swapped cards are returned to the deck and reshuffled before redraw (so a swapped card can come back —
/// matches Shadowverse convention). Both seats must confirm before the first real turn begins.
/// </summary>
public static class MulliganManager
{
    public static void Apply(GameContext ctx, PlayerSide side, IReadOnlyList<int> swapIndices)
    {
        var state = ctx.State;
        var p = state.GetPlayer(side);

        // Validate uniqueness and bounds.
        var seen = new HashSet<int>();
        foreach (var idx in swapIndices)
        {
            if (idx < 0 || idx >= p.Hand.Count) throw new InvalidActionException($"Mulligan index {idx} out of range.");
            if (!seen.Add(idx)) throw new InvalidActionException($"Duplicate mulligan index {idx}.");
        }

        // Return targeted cards to deck (bottom).
        var returned = new List<RuntimeCard>();
        foreach (var idx in swapIndices.OrderByDescending(x => x))
        {
            var card = p.Hand[idx];
            p.Hand.RemoveAt(idx);
            card.Zone = Zone.Deck;
            returned.Add(card);
        }
        foreach (var c in returned) p.Deck.Insert(0, c);
        ctx.Rng.Shuffle(p.Deck);

        // Draw the same count back.
        for (int i = 0; i < swapIndices.Count && p.Deck.Count > 0; i++)
        {
            var card = p.Deck[^1];
            p.Deck.RemoveAt(p.Deck.Count - 1);
            card.Zone = Zone.Hand;
            p.Hand.Add(card);
        }

        state.Mulligan.ChosenSwapIndices[(int)side] = new List<int>(swapIndices);
        state.Mulligan.Confirmed[(int)side] = true;
        ctx.Loop.Publish(new MulliganConfirmedEvent(side, swapIndices.ToArray()), ctx);
    }

    public static bool BothConfirmed(GameState state)
        => state.Mulligan.Confirmed[0] && state.Mulligan.Confirmed[1];

    /// <summary>
    /// After both players confirm: hand second player their compensation card and start turn 1.
    /// </summary>
    public static void FinalizeAndStart(GameContext ctx)
    {
        var state = ctx.State;
        if (state.GetPlayer(PlayerSide.Second).CompensationCard.Value != 0)
            EffectPrimitives.AddToHand(ctx, PlayerSide.Second, state.GetPlayer(PlayerSide.Second).CompensationCard);

        state.TurnNumber = 0; // StartTurn will bump to 1
        TurnManager.StartTurn(ctx, PlayerSide.First);
    }
}
