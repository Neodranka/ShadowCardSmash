using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 生命值均等化执行器 - 对生命值高的一方造成伤害使双方相等
    /// </summary>
    public class EqualizeHealthExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            var sourcePlayer = context.GameState.GetPlayer(context.SourcePlayerId);
            var opponentPlayer = context.GameState.GetPlayer(1 - context.SourcePlayerId);

            int myHealth = sourcePlayer.health;
            int enemyHealth = opponentPlayer.health;

            if (myHealth == enemyHealth)
            {
                UnityEngine.Debug.Log("EqualizeHealthExecutor: 双方生命值已相等，无需操作");
                return;
            }

            int damage = System.Math.Abs(myHealth - enemyHealth);
            int targetPlayerId = myHealth > enemyHealth ? context.SourcePlayerId : 1 - context.SourcePlayerId;
            var targetPlayer = context.GameState.GetPlayer(targetPlayerId);

            targetPlayer.TakeDamage(damage);

            context.AddEvent(new DamageEvent(
                context.SourcePlayerId,
                context.Source?.instanceId ?? -1,
                -1,
                damage,
                true,
                targetPlayerId
            ));

            // 检查玩家死亡
            if (targetPlayer.IsDead())
            {
                context.GameState.phase = GamePhase.GameOver;
                context.AddEvent(new GameOverEvent(
                    1 - targetPlayerId,
                    $"Player {targetPlayerId} health reduced to {targetPlayer.health}"
                ));
            }

            UnityEngine.Debug.Log($"EqualizeHealthExecutor: 对玩家{targetPlayerId}造成{damage}点伤害，双方生命值现在都是{System.Math.Min(myHealth, enemyHealth)}");
        }
    }

    /// <summary>
    /// 生命值交换执行器 - 交换双方玩家的生命值
    /// </summary>
    public class SwapHealthExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            var sourcePlayer = context.GameState.GetPlayer(context.SourcePlayerId);
            var opponentPlayer = context.GameState.GetPlayer(1 - context.SourcePlayerId);

            int myHealth = sourcePlayer.health;
            int enemyHealth = opponentPlayer.health;

            // 直接设置生命值
            sourcePlayer.health = enemyHealth;
            opponentPlayer.health = myHealth;

            // 生成事件
            context.AddEvent(new HealEvent(
                context.SourcePlayerId,
                context.Source?.instanceId ?? -1,
                -1,
                enemyHealth - myHealth,
                true,
                context.SourcePlayerId
            ));

            context.AddEvent(new HealEvent(
                context.SourcePlayerId,
                context.Source?.instanceId ?? -1,
                -1,
                myHealth - enemyHealth,
                true,
                1 - context.SourcePlayerId
            ));

            // 检查玩家死亡
            if (sourcePlayer.IsDead())
            {
                context.GameState.phase = GamePhase.GameOver;
                context.AddEvent(new GameOverEvent(
                    1 - context.SourcePlayerId,
                    $"Player {context.SourcePlayerId} health reduced to {sourcePlayer.health}"
                ));
            }
            else if (opponentPlayer.IsDead())
            {
                context.GameState.phase = GamePhase.GameOver;
                context.AddEvent(new GameOverEvent(
                    context.SourcePlayerId,
                    $"Player {1 - context.SourcePlayerId} health reduced to {opponentPlayer.health}"
                ));
            }

            UnityEngine.Debug.Log($"SwapHealthExecutor: 交换生命值 - 玩家0:{sourcePlayer.health}, 玩家1:{opponentPlayer.health}");
        }
    }
}
