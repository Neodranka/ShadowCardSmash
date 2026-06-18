using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// The deterministic engine driver. Owns the event bus, event log, RNG, and depth-tracking
/// for card-script triggered effect chains.
///
/// Inputs:   IGameAction (player commands)
/// Outputs:  IReadOnlyList&lt;BoardEvent&gt; (the chronological event log View / Net consume)
///
/// Threading: single-threaded. Run one Action at a time, fully resolve, then accept the next.
/// </summary>
public sealed class GameLoop
{
    public GameState State { get; }
    public ICardDatabase CardDatabase { get; }
    public EventBus Bus { get; } = new();
    public IRng Rng { get; private set; }

    private readonly List<BoardEvent> _eventLog = new();
    public IReadOnlyList<BoardEvent> EventLog => _eventLog;

    public int MaxDepth { get; set; } = 32;
    public int MaxSamePublishCount { get; set; } = 8;

    private int _currentDepth;
    private int _sequenceCounter;
    private readonly Dictionary<Type, int> _publishCountsInChain = new();

    /// <summary>
    /// Set when CheckGameEnd determines one or both players have hit 0 HP.
    /// Subsequent primitives become no-ops; the current Action winds down naturally.
    /// </summary>
    public bool IsGameOver => State.Result != GameResult.InProgress;

    public GameLoop(GameState state, ICardDatabase cardDb, IRng? rng = null)
    {
        State = state;
        CardDatabase = cardDb;
        Rng = rng ?? new DeterministicRng(state.RandomSeed, state.RandomCounter);
    }

    /// <summary>
    /// Process one player Action end-to-end. Returns the slice of events produced (a view onto the log).
    /// Throws InvalidActionException if Validate fails; the caller should validate at UI/Net boundary first.
    /// </summary>
    public IReadOnlyList<BoardEvent> Submit(IGameAction action)
    {
        if (IsGameOver) throw new InvalidActionException("Game is already over.");

        var result = action.Validate(State);
        if (!result.IsOk) throw new InvalidActionException(result.Reason ?? "invalid action");

        int sliceStart = _eventLog.Count;
        _publishCountsInChain.Clear();
        _currentDepth = 0;

        var ctx = new GameContext(State, Rng, this, CardDatabase, action.Issuer);
        action.Apply(ctx);

        CheckGameEnd();
        return _eventLog.GetRange(sliceStart, _eventLog.Count - sliceStart);
    }

    /// <summary>
    /// Called by EffectPrimitives to log an event and notify subscribers. Tracks depth + per-type publish counts
    /// inside one Submit() call to surface runaway chains (e.g., two cards mutually triggering each other).
    /// </summary>
    public void Publish<T>(T evt, GameContext ctx) where T : BoardEvent
    {
        if (_currentDepth >= MaxDepth)
            throw new RuleViolationException($"Effect resolution exceeded MaxDepth={MaxDepth}. Last event: {evt.GetType().Name}");

        var t = typeof(T);
        _publishCountsInChain.TryGetValue(t, out int n);
        if (++n > MaxSamePublishCount)
            throw new RuleViolationException($"Event {t.Name} fired {n} times in one resolution chain.");
        _publishCountsInChain[t] = n;

        var stamped = evt with { Sequence = _sequenceCounter++, Depth = _currentDepth };
        _eventLog.Add(stamped);

        _currentDepth++;
        try { Bus.Publish(stamped, ctx); }
        finally { _currentDepth--; }
    }

    public void CheckGameEnd()
    {
        if (State.Result != GameResult.InProgress) return;
        var p0 = State.GetPlayer(PlayerSide.First).Health <= 0;
        var p1 = State.GetPlayer(PlayerSide.Second).Health <= 0;
        if (!p0 && !p1) return;

        if (p0 && p1)
            State.Result = State.CurrentPlayer == PlayerSide.First ? GameResult.SecondWins : GameResult.FirstWins;
        else if (p0) State.Result = GameResult.SecondWins;
        else State.Result = GameResult.FirstWins;

        State.Phase = GamePhase.GameOver;
        _eventLog.Add(new GameEndedEvent(State.Result) { Sequence = _sequenceCounter++, Depth = 0 });
    }

    public void ClearEventLog() => _eventLog.Clear();
}

public sealed class InvalidActionException : Exception { public InvalidActionException(string msg) : base(msg) { } }
public sealed class RuleViolationException : Exception { public RuleViolationException(string msg) : base(msg) { } }
