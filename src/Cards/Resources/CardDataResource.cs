using Godot;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Resources;

/// <summary>
/// Godot Resource holding all editor-tweakable fields for a single card. One .tres file per card,
/// stored under <c>res://resources/cards/&lt;cardId&gt;.tres</c>. The matching C# class
/// (<c>[Card(cardId)] CardScript</c>) only carries behavior; data lives here.
///
/// In the Godot editor, choose "New Resource" → CardDataResource to author cards.
/// </summary>
[GlobalClass]
public partial class CardDataResource : Godot.Resource, ICardData
{
    [Export] public string Name { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";

    [Export] public int Cost { get; set; }
    [Export] public int BaseAttack { get; set; }
    [Export] public int BaseHealth { get; set; }
    [Export] public int EvolvedAttack { get; set; }
    [Export] public int EvolvedHealth { get; set; }

    [Export] public CardType CardTypeEnum { get; set; } = CardType.Minion;
    [Export] public HeroClass HeroClassEnum { get; set; } = HeroClass.Neutral;
    [Export] public Rarity RarityEnum { get; set; } = Rarity.Bronze;

    [Export] public string[] TagsArray { get; set; } = System.Array.Empty<string>();

    /// <summary>Bit-flags from <see cref="Keyword"/> (Ward=1, Rush=2, Storm=4, Barrier=8, Stealth=16).</summary>
    [Export] public int InitialKeywordsFlags { get; set; }
    /// <summary>For amulets. -1 means no countdown.</summary>
    [Export] public int InitialCountdownValue { get; set; } = -1;

    [Export(PropertyHint.File, "*.png,*.jpg,*.svg,*.webp")]
    public string ArtPath { get; set; } = "";

    // ICardData wrappers — interface needs read-only access to the enums and richer types.
    CardType ICardData.CardType => CardTypeEnum;
    HeroClass ICardData.HeroClass => HeroClassEnum;
    Rarity ICardData.Rarity => RarityEnum;
    IReadOnlyList<string> ICardData.Tags => TagsArray;
    Keyword ICardData.InitialKeywords => (Keyword)InitialKeywordsFlags;
    int ICardData.InitialCountdown => InitialCountdownValue;
}
