using Godot;
using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Cards.Resources;

/// <summary>
/// Persisted deck: saved as <c>user://decks/&lt;name&gt;.tres</c>. Card list is flat (one entry per copy),
/// so 3 copies of card 2001 means CardIds contains three 2001 entries — easier than serializing a dictionary.
/// </summary>
[GlobalClass]
public partial class DeckResource : Godot.Resource
{
    [Export] public string DeckName { get; set; } = "新卡组";
    [Export] public HeroClass HeroClassEnum { get; set; } = HeroClass.Neutral;
    [Export] public int[] CardIds { get; set; } = System.Array.Empty<int>();
    [Export] public int CompensationCardId { get; set; }
    [Export] public long LastModifiedTicks { get; set; }
}
