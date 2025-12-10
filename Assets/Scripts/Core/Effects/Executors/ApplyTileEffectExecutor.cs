using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 地格效果施加执行器 - 给敌方地格施加效果（如倾盆大雨）
    /// 参数: [效果类型, 持续回合数, 选择数量]
    /// 例如: ["DownpourRain", "3", "3"]
    /// </summary>
    public class ApplyTileEffectExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            if (context.Parameters == null || context.Parameters.Count < 3)
            {
                UnityEngine.Debug.LogWarning("ApplyTileEffectExecutor: 参数不足");
                return;
            }

            // 解析参数
            string effectTypeStr = context.Parameters[0];
            if (!System.Enum.TryParse<TileEffectType>(effectTypeStr, out var effectType))
            {
                UnityEngine.Debug.LogWarning($"ApplyTileEffectExecutor: 无法解析效果类型 {effectTypeStr}");
                return;
            }

            if (!int.TryParse(context.Parameters[1], out int duration))
            {
                UnityEngine.Debug.LogWarning($"ApplyTileEffectExecutor: 无法解析持续回合 {context.Parameters[1]}");
                return;
            }

            if (!int.TryParse(context.Parameters[2], out int selectCount))
            {
                UnityEngine.Debug.LogWarning($"ApplyTileEffectExecutor: 无法解析选择数量 {context.Parameters[2]}");
                return;
            }

            // 获取敌方玩家的场地
            int enemyId = 1 - context.SourcePlayerId;
            var enemyPlayer = context.GameState.GetPlayer(enemyId);

            // 检查是否有玩家选择的地格
            if (context.SelectedTileIndices != null && context.SelectedTileIndices.Count > 0)
            {
                // 使用玩家选择的地格
                int selected = 0;
                foreach (int tileIndex in context.SelectedTileIndices)
                {
                    if (tileIndex >= 0 && tileIndex < enemyPlayer.field.Length)
                    {
                        var tile = enemyPlayer.field[tileIndex];
                        if (!tile.HasTileEffect(effectType))
                        {
                            // 施加效果（ownerId 是敌方玩家，因为效果在敌方回合结束时触发）
                            tile.ApplyTileEffect(effectType, duration, enemyId);
                            selected++;
                            UnityEngine.Debug.Log($"ApplyTileEffectExecutor: 玩家选择的地格 {tileIndex} 被施加了 {effectType} 效果，持续 {duration} 回合");
                        }
                    }
                }
                UnityEngine.Debug.Log($"ApplyTileEffectExecutor: 共施加了 {selected} 个地格效果（玩家选择）");
                return;
            }

            // 没有玩家选择，使用随机选择（AI或未连接UI时的后备方案）
            // 收集可用的地格（没有该效果的格子）
            var availableTiles = new List<int>();
            for (int i = 0; i < enemyPlayer.field.Length; i++)
            {
                var tile = enemyPlayer.field[i];
                if (!tile.HasTileEffect(effectType))
                {
                    availableTiles.Add(i);
                }
            }

            if (availableTiles.Count == 0)
            {
                UnityEngine.Debug.Log("ApplyTileEffectExecutor: 没有可用的地格");
                return;
            }

            // 随机选择指定数量的地格
            var random = new System.Random();
            int selectedCount = 0;

            while (selectedCount < selectCount && availableTiles.Count > 0)
            {
                int randomIndex = random.Next(availableTiles.Count);
                int tileIndex = availableTiles[randomIndex];

                // 施加效果（ownerId 是敌方玩家，因为效果在敌方回合结束时触发）
                enemyPlayer.field[tileIndex].ApplyTileEffect(effectType, duration, enemyId);

                availableTiles.RemoveAt(randomIndex);
                selectedCount++;

                UnityEngine.Debug.Log($"ApplyTileEffectExecutor: 地格 {tileIndex} 被施加了 {effectType} 效果，持续 {duration} 回合");
            }

            UnityEngine.Debug.Log($"ApplyTileEffectExecutor: 共施加了 {selectedCount} 个地格效果（随机选择）");
        }
    }
}
