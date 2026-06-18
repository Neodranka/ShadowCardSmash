namespace ShadowCardSmash.Engine;

public delegate void BoardEventHandler<in T>(T evt, GameContext ctx) where T : BoardEvent;

internal sealed record Subscription(int Priority, int Insertion, Delegate Handler, Type EventType);

/// <summary>
/// Priority-ordered publish/subscribe. Listeners with higher priority fire first;
/// ties broken by insertion order (deterministic).
/// Card scripts subscribe on play and unsubscribe on destroy.
/// </summary>
public sealed class EventBus
{
    private readonly Dictionary<Type, List<Subscription>> _subs = new();
    private int _insertionCounter;

    public IDisposable Subscribe<T>(BoardEventHandler<T> handler, int priority = 0) where T : BoardEvent
    {
        var t = typeof(T);
        if (!_subs.TryGetValue(t, out var list)) _subs[t] = list = new List<Subscription>();
        var sub = new Subscription(priority, _insertionCounter++, handler, t);
        InsertSorted(list, sub);
        return new Unsub(this, sub);
    }

    public IDisposable SubscribeAll(BoardEventHandler<BoardEvent> handler, int priority = 0)
        => Subscribe<BoardEvent>(handler, priority);

    public void Publish<T>(T evt, GameContext ctx) where T : BoardEvent
    {
        // Fire handlers for the concrete type, then for every base up to BoardEvent.
        var t = evt.GetType();
        while (t is not null && typeof(BoardEvent).IsAssignableFrom(t))
        {
            if (_subs.TryGetValue(t, out var list))
            {
                // Copy snapshot so handlers can unsubscribe during iteration.
                var snapshot = list.ToArray();
                foreach (var sub in snapshot)
                {
                    sub.Handler.DynamicInvoke(evt, ctx);
                }
            }
            t = t.BaseType;
        }
    }

    private static void InsertSorted(List<Subscription> list, Subscription sub)
    {
        // Higher priority first; ties keep insertion order.
        for (int i = 0; i < list.Count; i++)
        {
            if (sub.Priority > list[i].Priority)
            {
                list.Insert(i, sub);
                return;
            }
        }
        list.Add(sub);
    }

    private sealed class Unsub : IDisposable
    {
        private readonly EventBus _bus;
        private Subscription? _sub;
        public Unsub(EventBus bus, Subscription sub) { _bus = bus; _sub = sub; }
        public void Dispose()
        {
            if (_sub is null) return;
            if (_bus._subs.TryGetValue(_sub.EventType, out var list)) list.Remove(_sub);
            _sub = null;
        }
    }
}
