using System;
using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 添加效果执行器 - 给目标添加持续效果
    /// 参数格式: ["触发器", "效果类型", "数值"]
    /// 例如: ["OnOwnerTurnEnd", "SelfDamage", "1"]
    /// </summary>
    public class AddEffectExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            if (context.Parameters == null || context.Parameters.Count < 3)
            {
                UnityEngine.Debug.LogWarning("AddEffectExecutor: 参数不足");
                return;
            }

            string triggerStr = context.Parameters[0];
            string effectTypeStr = context.Parameters[1];
            string valueStr = context.Parameters[2];

            if (!int.TryParse(valueStr, out int value))
            {
                UnityEngine.Debug.LogWarning($"AddEffectExecutor: 无法解析数值 {valueStr}");
                return;
            }

            // 解析触发器类型
            if (!Enum.TryParse<EffectTrigger>(triggerStr, out var trigger))
            {
                UnityEngine.Debug.LogWarning($"AddEffectExecutor: 无法解析触发器 {triggerStr}");
                return;
            }

            // 解析效果类型
            if (!Enum.TryParse<EffectType>(effectTypeStr, out var effectType))
            {
                UnityEngine.Debug.LogWarning($"AddEffectExecutor: 无法解析效果类型 {effectTypeStr}");
                return;
            }

            foreach (var target in context.Targets)
            {
                if (target == null) continue;

                // 为目标添加额外效果
                if (target.addedEffects == null)
                {
                    target.addedEffects = new List<AddedEffectData>();
                }

                var addedEffect = new AddedEffectData
                {
                    trigger = trigger,
                    effectType = effectType,
                    value = value,
                    targetType = TargetType.AllyPlayer // 默认对友方玩家（如自伤）
                };

                target.addedEffects.Add(addedEffect);

                UnityEngine.Debug.Log($"AddEffectExecutor: 为单位 {target.instanceId} 添加效果 - {triggerStr}/{effectTypeStr}/{value}");

                context.AddEvent(new BuffEvent(
                    context.SourcePlayerId,
                    target.instanceId,
                    0,
                    0,
                    null
                ));
            }
        }
    }
}
