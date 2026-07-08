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
    /// <summary>0..N cards from own hand (multi-select popup). Used by 塔尔莫维奇商队, 摄政议会,
    /// 拖延议程 etc. The actual selection count comes from ExtraTargets length at submit time.</summary>
    MultipleFromHand,
    /// <summary>Single card from own graveyard. Used by 阿尔文大公 OnEvolve.</summary>
    SingleAllyGraveyardCard,
}
