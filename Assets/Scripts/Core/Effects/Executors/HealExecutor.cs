using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 治疗效果执行器
    /// </summary>
    public class HealExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            int healAmount = context.Value;

            // 如果目标是玩家
            if (context.TargetIsPlayer)
            {
                var targetPlayer = context.GameState.GetPlayer(context.TargetPlayerId);
                int oldHealth = targetPlayer.health;
                targetPlayer.Heal(healAmount);
                int actualHealed = targetPlayer.health - oldHealth;

                context.AddEvent(new HealEvent(
                    context.SourcePlayerId,
                    -1,
                    healAmount,
                    actualHealed,
                    true,
                    context.TargetPlayerId
                ));

                return;
            }

            // 对随从治疗
            foreach (var target in context.Targets)
            {
                if (target == null) continue;

                int oldHealth = target.currentHealth;
                target.Heal(healAmount);
                int actualHealed = target.currentHealth - oldHealth;

                context.AddEvent(new HealEvent(
                    context.SourcePlayerId,
                    target.instanceId,
                    healAmount,
                    actualHealed,
                    false,
                    -1
                ));
            }
        }
    }
}
