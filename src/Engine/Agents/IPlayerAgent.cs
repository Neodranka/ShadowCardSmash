using System.Threading;
using System.Threading.Tasks;
using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine.Agents;

/// <summary>
/// Source of one player's actions. The GameLoop driver (BattleController) polls the agent whose
/// <see cref="Side"/> matches <see cref="GameState.CurrentPlayer"/> and awaits the next action.
///
/// Implementations:
///   • <c>HumanAgent</c>     — wraps UI input via TaskCompletionSource (current hotseat / local).
///   • <c>NetClientAgent</c> — Phase 3+: awaits action arriving over the wire from a remote peer.
///
/// The agent should not mutate game state directly; it only chooses what to submit.
/// Cancellation is used to tear down the agent cleanly on scene exit or disconnect.
/// </summary>
public interface IPlayerAgent
{
    PlayerSide Side { get; }
    Task<IGameAction> ChooseAction(GameState view, CancellationToken ct);
}
