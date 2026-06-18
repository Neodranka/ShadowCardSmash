using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>GDD §2.1: 40 cards, max 3 copies of any card, only Neutral or matching class.</summary>
public static class DeckValidator
{
    public const int DeckSize = 40;
    public const int MaxCopiesPerCard = 3;

    public readonly record struct Result(bool IsValid, string? Reason)
    {
        public static Result Ok() => new(true, null);
        public static Result Fail(string r) => new(false, r);
    }

    public static Result Validate(IReadOnlyList<CardId> cards, HeroClass owningClass, ICardDatabase db)
    {
        if (cards.Count != DeckSize) return Result.Fail($"Deck must contain exactly {DeckSize} cards (was {cards.Count}).");

        var counts = new Dictionary<CardId, int>();
        foreach (var id in cards)
        {
            if (!db.TryGet(id, out var script)) return Result.Fail($"Unknown card id {id}.");
            if (script.HeroClass != HeroClass.Neutral && script.HeroClass != owningClass)
                return Result.Fail($"{script.Name} ({script.HeroClass}) cannot be in a {owningClass} deck.");

            counts.TryGetValue(id, out int n);
            counts[id] = n + 1;
            if (counts[id] > MaxCopiesPerCard)
                return Result.Fail($"Too many copies of {script.Name} (limit {MaxCopiesPerCard}).");
        }
        return Result.Ok();
    }
}
