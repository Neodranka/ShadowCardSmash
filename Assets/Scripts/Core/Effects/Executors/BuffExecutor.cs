using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 增益效果执行器
    /// </summary>
    public class BuffExecutor : IEffectExecutor
    {
        private static int _nextBuffId = 1;

        public void Execute(EffectContext context)
        {
            // 解析参数: parameters[0] = "攻击力,生命值" 如 "+2,+2" 或 "2,2"
            int attackMod = 0;
            int healthMod = 0;

            if (context.Parameters != null && context.Parameters.Count >= 2)
            {
                // parameters[0] = 攻击力, parameters[1] = 生命值
                int.TryParse(context.Parameters[0].Trim().Replace("+", ""), out attackMod);
                int.TryParse(context.Parameters[1].Trim().Replace("+", ""), out healthMod);
            }
            else if (context.Parameters != null && context.Parameters.Count == 1)
            {
                // 如果只有一个参数，检查是否是 "攻击力,生命值" 格式
                string[] parts = context.Parameters[0].Split(',');
                if (parts.Length >= 2)
                {
                    // 移除+号并解析
                    string attackStr = parts[0].Trim().Replace("+", "");
                    string healthStr = parts[1].Trim().Replace("+", "");

                    int.TryParse(attackStr, out attackMod);
                    int.TryParse(healthStr, out healthMod);
                }
                else
                {
                    // 如果只有一个值，同时用于攻击和生命
                    string valueStr = parts[0].Trim().Replace("+", "");
                    if (int.TryParse(valueStr, out int value))
                    {
                        attackMod = value;
                        healthMod = value;
                    }
                }
            }
            else
            {
                // 如果没有参数，使用Value作为攻击增益值，SecondaryValue作为生命增益
                attackMod = context.Value;
                healthMod = context.SecondaryValue;
            }

            int sourceCardId = context.Source?.cardId ?? -1;

            foreach (var target in context.Targets)
            {
                if (target == null) continue;

                // 创建Buff数据
                var buff = new BuffData
                {
                    buffId = _nextBuffId++,
                    sourceCardId = sourceCardId,
                    attackModifier = attackMod,
                    healthModifier = healthMod,
                    duration = -1, // 默认永久
                    grantedKeywords = new List<Keyword>()
                };

                // 应用到目标
                target.buffs.Add(buff);
                target.currentAttack += attackMod;
                target.currentHealth += healthMod;
                target.maxHealth += healthMod;

                // 确保生命值不低于1（如果是减益）
                if (target.currentHealth < 1 && healthMod < 0)
                {
                    // 减益导致死亡的情况在DamageExecutor处理
                }

                context.AddEvent(new BuffEvent(
                    context.SourcePlayerId,
                    target.instanceId,
                    attackMod,
                    healthMod,
                    null
                ));

                // 检查是否因减益而死亡
                if (target.IsDead())
                {
                    var tile = context.GameState.FindTileByInstanceId(target.instanceId);
                    if (tile != null)
                    {
                        var owner = context.GameState.GetPlayer(target.ownerId);
                        owner.graveyard.Add(target.cardId);
                        tile.RemoveUnit();

                        context.AddEvent(new UnitDestroyedEvent(
                            context.SourcePlayerId,
                            target.instanceId,
                            target.cardId,
                            tile.tileIndex,
                            target.ownerId,
                            false
                        ));
                    }
                }
            }
        }

        /// <summary>
        /// 重置Buff ID计数器（用于测试）
        /// </summary>
        public static void ResetBuffIdCounter()
        {
            _nextBuffId = 1;
        }
    }
}
