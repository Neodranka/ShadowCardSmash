using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 伤害效果执行器
    /// </summary>
    public class DamageExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            int damage = context.Value;
            int sourceInstanceId = context.Source?.instanceId ?? -1;

            // 如果目标是玩家
            if (context.TargetIsPlayer)
            {
                var targetPlayer = context.GameState.GetPlayer(context.TargetPlayerId);
                targetPlayer.TakeDamage(damage);

                context.AddEvent(new DamageEvent(
                    context.SourcePlayerId,
                    sourceInstanceId,
                    -1,
                    damage,
                    true,
                    context.TargetPlayerId
                ));

                // 检查玩家是否死亡
                if (targetPlayer.IsDead())
                {
                    context.GameState.phase = GamePhase.GameOver;
                    context.AddEvent(new GameOverEvent(
                        context.SourcePlayerId,
                        $"Player {context.TargetPlayerId} health reduced to {targetPlayer.health}"
                    ));
                }

                return;
            }

            // 对随从造成伤害
            foreach (var target in context.Targets)
            {
                if (target == null) continue;

                int oldHealth = target.currentHealth;
                target.TakeDamage(damage);

                context.AddEvent(new DamageEvent(
                    context.SourcePlayerId,
                    sourceInstanceId,
                    target.instanceId,
                    damage,
                    false,
                    -1
                ));

                // 检查目标是否死亡
                if (target.IsDead())
                {
                    // 找到目标所在格子
                    var tile = context.GameState.FindTileByInstanceId(target.instanceId);
                    if (tile != null)
                    {
                        int tileIndex = tile.tileIndex;

                        // 将单位移到墓地
                        var owner = context.GameState.GetPlayer(target.ownerId);
                        owner.graveyard.Add(target.cardId);
                        tile.RemoveUnit();

                        context.AddEvent(new UnitDestroyedEvent(
                            context.SourcePlayerId,
                            target.instanceId,
                            target.cardId,
                            tileIndex,
                            target.ownerId,
                            false
                        ));
                    }
                }
            }
        }
    }
}
