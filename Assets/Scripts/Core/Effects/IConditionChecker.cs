using System.Collections.Generic;
using ShadowCardSmash.Core.Data;

namespace ShadowCardSmash.Core.Effects
{
    /// <summary>
    /// 条件检查器接口
    /// </summary>
    public interface IConditionChecker
    {
        /// <summary>
        /// 检查条件是否满足
        /// </summary>
        /// <param name="state">游戏状态</param>
        /// <param name="source">效果来源</param>
        /// <param name="sourcePlayerId">来源玩家ID</param>
        /// <param name="conditionType">条件类型</param>
        /// <param name="conditionParams">条件参数</param>
        /// <returns>条件是否满足</returns>
        bool CheckCondition(GameState state, RuntimeCard source, int sourcePlayerId,
            string conditionType, List<string> conditionParams);
    }

    /// <summary>
    /// 默认条件检查器实现
    /// </summary>
    public class DefaultConditionChecker : IConditionChecker
    {
        public bool CheckCondition(GameState state, RuntimeCard source, int sourcePlayerId,
            string conditionType, List<string> conditionParams)
        {
            // 空条件总是满足
            if (string.IsNullOrEmpty(conditionType))
                return true;

            var player = state.GetPlayer(sourcePlayerId);
            var opponent = state.GetPlayer(1 - sourcePlayerId);

            switch (conditionType.ToLower())
            {
                // 场上随从数量条件
                case "field_count_gte":
                    // 参数: [0]=最小数量
                    if (conditionParams.Count > 0 && int.TryParse(conditionParams[0], out int minCount))
                    {
                        return player.GetAllFieldUnits().Count >= minCount;
                    }
                    return false;

                case "field_count_lte":
                    // 参数: [0]=最大数量
                    if (conditionParams.Count > 0 && int.TryParse(conditionParams[0], out int maxCount))
                    {
                        return player.GetAllFieldUnits().Count <= maxCount;
                    }
                    return false;

                // 手牌数量条件
                case "hand_count_gte":
                    if (conditionParams.Count > 0 && int.TryParse(conditionParams[0], out int minHand))
                    {
                        return player.hand.Count >= minHand;
                    }
                    return false;

                case "hand_count_lte":
                    if (conditionParams.Count > 0 && int.TryParse(conditionParams[0], out int maxHand))
                    {
                        return player.hand.Count <= maxHand;
                    }
                    return false;

                // 生命值条件
                case "health_gte":
                    if (conditionParams.Count > 0 && int.TryParse(conditionParams[0], out int minHealth))
                    {
                        return player.health >= minHealth;
                    }
                    return false;

                case "health_lte":
                    if (conditionParams.Count > 0 && int.TryParse(conditionParams[0], out int maxHealth))
                    {
                        return player.health <= maxHealth;
                    }
                    return false;

                // 费用条件
                case "mana_gte":
                    if (conditionParams.Count > 0 && int.TryParse(conditionParams[0], out int minMana))
                    {
                        return player.mana >= minMana;
                    }
                    return false;

                // 进化条件
                case "evolved":
                    return source != null && source.isEvolved;

                case "not_evolved":
                    return source != null && !source.isEvolved;

                // 回合数条件
                case "turn_gte":
                    if (conditionParams.Count > 0 && int.TryParse(conditionParams[0], out int minTurn))
                    {
                        return state.turnNumber >= minTurn;
                    }
                    return false;

                // 是否有特定标签的随从在场
                case "has_tag_on_field":
                    if (conditionParams.Count > 0)
                    {
                        string tag = conditionParams[0];
                        // 需要CardDatabase来检查标签，暂时返回false
                        // TODO: 实现标签检查
                    }
                    return false;

                // 敌方场上是否有随从
                case "enemy_has_minions":
                    return opponent.GetAllFieldUnits().Count > 0;

                // 我方场上是否有随从
                case "ally_has_minions":
                    return player.GetAllFieldUnits().Count > 0;

                // EP条件
                case "has_ep":
                    return player.evolutionPoints > 0;

                // 牌库是否为空
                case "deck_empty":
                    return player.IsDeckEmpty();

                default:
                    // 未知条件类型，默认满足
                    UnityEngine.Debug.LogWarning($"Unknown condition type: {conditionType}");
                    return true;
            }
        }
    }
}
