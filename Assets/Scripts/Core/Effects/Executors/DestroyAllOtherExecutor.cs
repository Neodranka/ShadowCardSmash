using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 破坏所有其他随从执行器 - 破坏除自身外所有场上随从
    /// 参数:
    ///   heal_by_count - 根据破坏数量治疗（无条件）
    ///   heal_if_condition:condition - 只有满足条件时才治疗（如 heal_if_condition:total_self_damage>=15）
    /// </summary>
    public class DestroyAllOtherExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            bool healByCount = false;
            string healCondition = null;

            if (context.Parameters != null)
            {
                foreach (var param in context.Parameters)
                {
                    if (param == "heal_by_count")
                    {
                        healByCount = true;
                    }
                    else if (param.StartsWith("heal_if_condition:"))
                    {
                        healCondition = param.Substring("heal_if_condition:".Length);
                        healByCount = true; // 也需要根据数量治疗
                    }
                }
            }

            int destroyedCount = 0;

            // 收集所有要破坏的随从
            var toDestroy = new List<(RuntimeCard card, TileState tile, int playerId)>();

            for (int playerId = 0; playerId < 2; playerId++)
            {
                var player = context.GameState.GetPlayer(playerId);
                foreach (var tile in player.field)
                {
                    if (tile.occupant == null)
                        continue;

                    // 排除效果来源（自己）
                    if (context.Source != null && tile.occupant.instanceId == context.Source.instanceId)
                        continue;

                    toDestroy.Add((tile.occupant, tile, playerId));
                }
            }

            // 执行破坏
            foreach (var (card, tile, playerId) in toDestroy)
            {
                int tileIndex = tile.tileIndex;
                int cardId = card.cardId;
                int instanceId = card.instanceId;

                // 移除并加入墓地
                var owner = context.GameState.GetPlayer(playerId);
                owner.graveyard.Add(cardId);
                tile.RemoveUnit();

                // 记录本回合有随从被破坏
                context.GameState.GetPlayer(0).RecordMinionDestroyed();
                context.GameState.GetPlayer(1).RecordMinionDestroyed();

                context.AddEvent(new UnitDestroyedEvent(
                    context.SourcePlayerId,
                    instanceId,
                    cardId,
                    tileIndex,
                    playerId,
                    false
                ));

                destroyedCount++;
            }

            UnityEngine.Debug.Log($"DestroyAllOtherExecutor: 破坏了 {destroyedCount} 个随从");

            // 如果需要根据破坏数量治疗
            if (healByCount && destroyedCount > 0)
            {
                // 检查是否有条件限制
                bool shouldHeal = true;
                if (!string.IsNullOrEmpty(healCondition))
                {
                    shouldHeal = CheckHealCondition(context, healCondition);
                }

                if (shouldHeal)
                {
                    var sourcePlayer = context.GameState.GetPlayer(context.SourcePlayerId);
                    sourcePlayer.Heal(destroyedCount);

                    context.AddEvent(new HealEvent(
                        context.SourcePlayerId,
                        context.Source?.instanceId ?? -1,
                        -1,
                        destroyedCount,
                        true,
                        context.SourcePlayerId
                    ));

                    UnityEngine.Debug.Log($"DestroyAllOtherExecutor: 治疗玩家 {destroyedCount} 点");
                }
                else
                {
                    UnityEngine.Debug.Log($"DestroyAllOtherExecutor: 不满足治疗条件 {healCondition}");
                }
            }
        }

        /// <summary>
        /// 检查治疗条件
        /// </summary>
        private bool CheckHealCondition(EffectContext context, string condition)
        {
            var player = context.GameState.GetPlayer(context.SourcePlayerId);

            // 解析条件：total_self_damage>=15
            if (condition.Contains(">="))
            {
                var parts = condition.Split(new string[] { ">=" }, System.StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string varName = parts[0].Trim();
                    if (int.TryParse(parts[1].Trim(), out int threshold))
                    {
                        int varValue = GetConditionVariable(player, varName);
                        return varValue >= threshold;
                    }
                }
            }
            else if (condition.Contains(">"))
            {
                var parts = condition.Split('>');
                if (parts.Length == 2)
                {
                    string varName = parts[0].Trim();
                    if (int.TryParse(parts[1].Trim(), out int threshold))
                    {
                        int varValue = GetConditionVariable(player, varName);
                        return varValue > threshold;
                    }
                }
            }

            return true; // 默认通过
        }

        private int GetConditionVariable(PlayerState player, string varName)
        {
            switch (varName)
            {
                case "total_self_damage":
                    return player.totalSelfDamage;
                case "self_damage_this_turn":
                    return player.selfDamageThisTurn;
                case "self_damage_count":
                    return player.selfDamageCount;
                case "health":
                    return player.health;
                default:
                    UnityEngine.Debug.LogWarning($"DestroyAllOtherExecutor: 未知条件变量: {varName}");
                    return 0;
            }
        }
    }
}
