using System.Reflection;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards;

/// <summary>
/// Reflection-driven ICardDatabase. Scans the calling assembly (or explicitly provided assemblies)
/// for every non-abstract CardScript subclass marked with [Card(id)], instantiates one singleton each.
///
/// Adding a card: drop a new file in src/Cards/&lt;Class&gt;/ — no edits here, no registration table.
/// </summary>
public sealed class CardRegistry : ICardDatabase
{
    private readonly Dictionary<CardId, ICardScript> _byId = new();

    public int Count => _byId.Count;

    public ICardScript Get(CardId id)
    {
        if (_byId.TryGetValue(id, out var s)) return s;
        throw new KeyNotFoundException($"No card registered for id {id}");
    }

    public bool TryGet(CardId id, out ICardScript script)
    {
        if (_byId.TryGetValue(id, out var s)) { script = s; return true; }
        script = default!;
        return false;
    }

    public IEnumerable<ICardScript> All() => _byId.Values;

    public static CardRegistry ScanAssembly(params Assembly[] assemblies)
    {
        var registry = new CardRegistry();
        if (assemblies.Length == 0) assemblies = new[] { Assembly.GetCallingAssembly() };

        foreach (var asm in assemblies)
        {
            foreach (var type in asm.GetTypes())
            {
                if (type.IsAbstract || !typeof(CardScript).IsAssignableFrom(type)) continue;
                var attr = (CardAttribute?)Attribute.GetCustomAttribute(type, typeof(CardAttribute));
                if (attr is null) continue;

                if (registry._byId.ContainsKey(attr.Id))
                    throw new InvalidOperationException(
                        $"Duplicate CardId {attr.Id.Value} on {type.FullName} (already registered: {registry._byId[attr.Id].GetType().FullName})");

                var instance = (CardScript)Activator.CreateInstance(type)!;
                registry._byId[attr.Id] = instance;
            }
        }
        return registry;
    }

    /// <summary>Programmatic registration; useful for tests with stub scripts.</summary>
    public void Register(ICardScript script)
    {
        if (_byId.ContainsKey(script.Id))
            throw new InvalidOperationException($"Card {script.Id} already registered.");
        _byId[script.Id] = script;
    }
}
