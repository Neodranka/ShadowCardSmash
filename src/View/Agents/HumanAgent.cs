using System.Threading;
using System.Threading.Tasks;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;
using ShadowCardSmash.Engine.Agents;

namespace ShadowCardSmash.View.Agents;

/// <summary>
/// Bridges the local UI input pipeline (BattleController's mouse/keyboard handlers) to the
/// <see cref="IPlayerAgent"/> contract used by the game-loop driver.
///
/// Lifecycle per turn:
///   1. Driver calls <c>ChooseAction(view, ct)</c>; agent stores a fresh <see cref="TaskCompletionSource{TResult}"/>
///      and returns the Task.
///   2. The UI may walk through several sub-states (pick tile → pick target → pick choice) while the agent is
///      "waiting". The agent ignores these intermediate states.
///   3. When the UI determines the final IGameAction, it calls <see cref="Submit"/>, which completes the Task
///      and unblocks the driver.
///   4. Driver applies the action, animates events, then loops back and calls ChooseAction again.
///
/// Cancellation: the driver's CancellationToken cancels the pending Task; on next ChooseAction call a new TCS is
/// created. This keeps scene teardown clean.
/// </summary>
public sealed class HumanAgent : IPlayerAgent
{
    public PlayerSide Side { get; }

    private TaskCompletionSource<IGameAction>? _pending;

    public HumanAgent(PlayerSide side)
    {
        Side = side;
    }

    public Task<IGameAction> ChooseAction(GameState view, CancellationToken ct)
    {
        // RunContinuationsAsynchronously avoids deadlocks if a continuation tries to re-enter the UI thread.
        _pending = new TaskCompletionSource<IGameAction>(TaskCreationOptions.RunContinuationsAsynchronously);
        ct.Register(() => _pending?.TrySetCanceled());
        return _pending.Task;
    }

    /// <summary>
    /// Called from the UI input handlers when the player has finished assembling an action.
    /// No-op if the agent is not currently being awaited (e.g., clicked during the opponent's turn).
    /// </summary>
    public void Submit(IGameAction action)
    {
        var p = _pending;
        _pending = null;
        p?.TrySetResult(action);
    }

    /// <summary>True when the driver is awaiting input from this agent.</summary>
    public bool IsAwaiting => _pending is { Task.IsCompleted: false };
}
