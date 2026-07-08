using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// Godot-free data view of a single card. The runtime impl is <c>CardDataResource</c> in the Cards layer
/// (a Godot Resource loaded from <c>res://resources/cards/&lt;id&gt;.tres</c>), but tests can stub this directly.
///
/// CardScript reads its display + stat fields through this interface, so balance changes flow from .tres
/// without recompiling C# code.
/// </summary>
public interface ICardData
{
    string Name { get; }
    string Description { get; }
    int Cost { get; }
    int BaseAttack { get; }
    int BaseHealth { get; }
    int EvolvedAttack { get; }
    int EvolvedHealth { get; }
    CardType CardType { get; }
    HeroClass HeroClass { get; }
    Rarity Rarity { get; }
    IReadOnlyList<string> Tags { get; }
    Keyword InitialKeywords { get; }
    int InitialCountdown { get; }
    string ArtPath { get; }
    /// <summary>True = special Amulet that goes to TerrainSlot and overwrites any existing occupant.</summary>
    bool IsLandmark { get; }
}
