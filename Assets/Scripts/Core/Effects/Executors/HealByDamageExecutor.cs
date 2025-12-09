using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 根据伤害量回复生命执行器 - 吸血鬼职业用
    /// parameters[0] = 数据来源 ("total_self_damage", "self_damage_this_turn")
    /// parameters[1] = 修正 ("half" 减半, 不填则全额)
    /// </summary>
    public class HealByDamageExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            var player = context.GetSourcePlayer();
            if (player == null) return;

            // 获取数据来源
            string source = "total_self_damage";
            bool isHalf = false;

            if (context.Parameters != null)
            {
                if (context.Parameters.Count > 0)
                {
                    source = context.Parameters[0];
                }
                if (context.Parameters.Count > 1 && context.Parameters[1] == "half")
                {
                    isHalf = true;
                }
            }

            // 获取基础值
            int baseValue = 0;
            switch (source)
            {
                case "total_self_damage":
                    baseValue = player.totalSelfDamage;
                    break;
                case "self_damage_this_turn":
                    baseValue = player.selfDamageThisTurn;
                    break;
                default:
                    baseValue = context.Value;
                    break;
            }

            // 应用修正
            int healAmount = isHalf ? baseValue / 2 : baseValue;

            if (healAmount <= 0)
            {
                UnityEngine.Debug.Log($"HealByDamageExecutor: 回复量为0，跳过回复");
                return;
            }

            // 执行回复
            int oldHealth = player.health;
            player.Heal(healAmount);
            int actualHeal = player.health - oldHealth;

            context.AddEvent(new HealEvent(
                context.SourcePlayerId,
                -1, // 没有随从目标
                healAmount,
                actualHeal,
                true,
                context.SourcePlayerId
            ));

            UnityEngine.Debug.Log($"HealByDamageExecutor: 玩家{context.SourcePlayerId}根据{source}({baseValue})回复{actualHeal}点生命");
        }
    }
}
