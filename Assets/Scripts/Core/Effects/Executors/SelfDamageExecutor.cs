using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 自伤效果执行器 - 对自己玩家造成伤害（吸血鬼用）
    /// </summary>
    public class SelfDamageExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            var player = context.GetSourcePlayer();
            if (player == null) return;

            int damage = context.Value;
            if (damage <= 0) return;

            // 使用自伤方法（会记录统计）
            int actualDamage = player.TakeSelfDamage(damage);

            if (actualDamage > 0)
            {
                context.AddEvent(new DamageEvent(
                    context.SourcePlayerId,
                    context.Source?.instanceId ?? -1,
                    -1,
                    actualDamage,
                    true,
                    context.SourcePlayerId
                ));

                // 检查玩家死亡
                if (player.IsDead())
                {
                    context.GameState.phase = GamePhase.GameOver;
                    context.AddEvent(new GameOverEvent(
                        1 - context.SourcePlayerId,
                        $"Player {context.SourcePlayerId} died from self-damage"
                    ));
                }
            }

            UnityEngine.Debug.Log($"SelfDamageExecutor: 玩家{context.SourcePlayerId}自伤{actualDamage}点伤害");
        }
    }
}
