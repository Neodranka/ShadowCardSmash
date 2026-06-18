using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// Declarative target requirement attached to play-time / activation-time targeted cards.
/// The Engine uses this to drive UI prompts and validation; card scripts read ctx.PickedTarget / PickedPlayer.
/// </summary>
public enum TargetSpec
{
    None,
    SingleAnyMinion,
    SingleEnemyMinion,
    SingleAllyMinion,
    SingleAnyCharacter,    // minion or player
    SingleEnemyCharacter,
    EnemyPlayer,
    AllyPlayer,
    EmptyAllyTile,
    EmptyEnemyTile,
}
