using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Cards;

/// <summary>
/// Marks a class as a card whose script body lives in C#.
/// The CardRegistry scans the assembly at startup and instantiates one singleton per [Card(id)] class.
/// Two classes sharing an id is a fatal error caught at registration time.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CardAttribute : Attribute
{
    public CardId Id { get; }
    public CardAttribute(int id) { Id = new CardId(id); }
}
