using System.Collections.Generic;
using ShadowCardSmash.Core.Data;

namespace ShadowCardSmash.Core.Effects
{
    /// <summary>
    /// 目标选择器接口
    /// </summary>
    public interface ITargetSelector
    {
        /// <summary>
        /// 选择目标
        /// </summary>
        /// <param name="state">游戏状态</param>
        /// <param name="source">效果来源</param>
        /// <param name="sourcePlayerId">来源玩家ID</param>
        /// <param name="targetType">目标类型</param>
        /// <returns>目标列表</returns>
        List<RuntimeCard> SelectTargets(GameState state, RuntimeCard source, int sourcePlayerId, TargetType targetType);

        /// <summary>
        /// 检查是否需要玩家选择目标
        /// </summary>
        bool RequiresPlayerChoice(TargetType targetType);

        /// <summary>
        /// 获取可选目标列表（用于UI显示）
        /// </summary>
        List<RuntimeCard> GetValidTargets(GameState state, RuntimeCard source, int sourcePlayerId, TargetType targetType);
    }

    /// <summary>
    /// 默认目标选择器实现
    /// </summary>
    public class DefaultTargetSelector : ITargetSelector
    {
        private System.Random _random;

        public DefaultTargetSelector(System.Random random = null)
        {
            _random = random ?? new System.Random();
        }

        public List<RuntimeCard> SelectTargets(GameState state, RuntimeCard source, int sourcePlayerId, TargetType targetType)
        {
            var targets = new List<RuntimeCard>();
            var sourcePlayer = state.GetPlayer(sourcePlayerId);
            var opponentPlayer = state.GetPlayer(1 - sourcePlayerId);

            switch (targetType)
            {
                case TargetType.Self:
                    if (source != null)
                        targets.Add(source);
                    break;

                case TargetType.AllEnemies:
                    targets.AddRange(opponentPlayer.GetAllFieldUnits());
                    break;

                case TargetType.AllAllies:
                    targets.AddRange(sourcePlayer.GetAllFieldUnits());
                    break;

                case TargetType.AllMinions:
                    targets.AddRange(sourcePlayer.GetAllFieldUnits());
                    targets.AddRange(opponentPlayer.GetAllFieldUnits());
                    break;

                case TargetType.RandomEnemy:
                    var enemies = opponentPlayer.GetAllFieldUnits();
                    if (enemies.Count > 0)
                    {
                        int index = _random.Next(enemies.Count);
                        targets.Add(enemies[index]);
                    }
                    break;

                case TargetType.RandomAlly:
                    var allies = sourcePlayer.GetAllFieldUnits();
                    if (allies.Count > 0)
                    {
                        int index = _random.Next(allies.Count);
                        targets.Add(allies[index]);
                    }
                    break;

                case TargetType.AdjacentTiles:
                    if (source != null)
                    {
                        // 找到source所在的格子
                        var tile = state.FindTileByInstanceId(source.instanceId);
                        if (tile != null)
                        {
                            int tileIndex = tile.tileIndex;
                            // 左边格子
                            if (tileIndex > 0)
                            {
                                var leftTile = sourcePlayer.field[tileIndex - 1];
                                if (leftTile.occupant != null)
                                    targets.Add(leftTile.occupant);
                            }
                            // 右边格子
                            if (tileIndex < PlayerState.FIELD_SIZE - 1)
                            {
                                var rightTile = sourcePlayer.field[tileIndex + 1];
                                if (rightTile.occupant != null)
                                    targets.Add(rightTile.occupant);
                            }
                        }
                    }
                    break;

                // 需要玩家选择的类型，返回空列表
                case TargetType.SingleEnemy:
                case TargetType.SingleAlly:
                case TargetType.PlayerChoice:
                case TargetType.EnemyPlayer:
                case TargetType.AllyPlayer:
                    // 这些需要外部指定
                    break;
            }

            return targets;
        }

        public bool RequiresPlayerChoice(TargetType targetType)
        {
            switch (targetType)
            {
                case TargetType.SingleEnemy:
                case TargetType.SingleAlly:
                case TargetType.PlayerChoice:
                    return true;
                default:
                    return false;
            }
        }

        public List<RuntimeCard> GetValidTargets(GameState state, RuntimeCard source, int sourcePlayerId, TargetType targetType)
        {
            var targets = new List<RuntimeCard>();
            var sourcePlayer = state.GetPlayer(sourcePlayerId);
            var opponentPlayer = state.GetPlayer(1 - sourcePlayerId);

            switch (targetType)
            {
                case TargetType.SingleEnemy:
                    targets.AddRange(opponentPlayer.GetAllFieldUnits());
                    break;

                case TargetType.SingleAlly:
                    foreach (var unit in sourcePlayer.GetAllFieldUnits())
                    {
                        // 排除自身（如果需要）
                        if (source == null || unit.instanceId != source.instanceId)
                            targets.Add(unit);
                    }
                    break;

                case TargetType.PlayerChoice:
                    // 所有场上单位都可选
                    targets.AddRange(sourcePlayer.GetAllFieldUnits());
                    targets.AddRange(opponentPlayer.GetAllFieldUnits());
                    break;

                default:
                    // 自动选择的类型，返回SelectTargets的结果
                    targets = SelectTargets(state, source, sourcePlayerId, targetType);
                    break;
            }

            return targets;
        }
    }
}
