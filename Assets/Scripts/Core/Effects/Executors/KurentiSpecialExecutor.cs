using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 克伦缇特殊效果执行器
    /// 对我方玩家造成12点伤害，随后对所有敌方随从分配X点伤害
    /// X = 12 - 实际对玩家造成的伤害（屏障可以减少伤害）
    /// </summary>
    public class KurentiSpecialExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            var sourcePlayer = context.GameState.GetPlayer(context.SourcePlayerId);
            int baseDamage = context.Value; // 12

            // 对自己玩家造成伤害
            bool hadBarrier = sourcePlayer.hasBarrier;
            int actualDamage = sourcePlayer.TakeSelfDamage(baseDamage);

            context.AddEvent(new DamageEvent(
                context.SourcePlayerId,
                context.Source?.instanceId ?? -1,
                -1,
                actualDamage,
                true,
                context.SourcePlayerId
            ));

            // 检查玩家死亡
            if (sourcePlayer.IsDead())
            {
                context.GameState.phase = GamePhase.GameOver;
                context.AddEvent(new GameOverEvent(
                    1 - context.SourcePlayerId,
                    $"Player {context.SourcePlayerId} died from Kurenti's effect"
                ));
                return;
            }

            // 计算分配给敌方随从的伤害
            int damageToDistribute = baseDamage - actualDamage;
            if (hadBarrier)
            {
                damageToDistribute = baseDamage; // 屏障抵挡了全部伤害
            }

            UnityEngine.Debug.Log($"KurentiSpecialExecutor: 对玩家造成{actualDamage}伤害，分配{damageToDistribute}伤害给敌方随从");

            if (damageToDistribute <= 0)
                return;

            // 获取所有敌方随从
            var opponentPlayer = context.GameState.GetPlayer(1 - context.SourcePlayerId);
            var enemies = new List<RuntimeCard>();
            var enemyTiles = new List<TileState>();

            foreach (var tile in opponentPlayer.field)
            {
                if (tile.occupant != null)
                {
                    enemies.Add(tile.occupant);
                    enemyTiles.Add(tile);
                }
            }

            if (enemies.Count == 0)
            {
                UnityEngine.Debug.Log("KurentiSpecialExecutor: 没有敌方随从可以分配伤害");
                return;
            }

            // 随机分配伤害
            var random = new System.Random();
            int remainingDamage = damageToDistribute;

            while (remainingDamage > 0 && enemies.Count > 0)
            {
                // 随机选择一个随从
                int index = random.Next(enemies.Count);
                var target = enemies[index];
                var targetTile = enemyTiles[index];

                // 对其造成1点伤害
                target.TakeDamage(1);
                remainingDamage--;

                context.AddEvent(new DamageEvent(
                    context.SourcePlayerId,
                    context.Source?.instanceId ?? -1,
                    target.instanceId,
                    1,
                    false,
                    -1
                ));

                // 检查是否死亡
                if (target.IsDead())
                {
                    int tileIndex = targetTile.tileIndex;

                    // 移除并加入墓地
                    opponentPlayer.graveyard.Add(target.cardId);
                    targetTile.RemoveUnit();

                    context.GameState.GetPlayer(0).RecordMinionDestroyed();
                    context.GameState.GetPlayer(1).RecordMinionDestroyed();

                    context.AddEvent(new UnitDestroyedEvent(
                        context.SourcePlayerId,
                        target.instanceId,
                        target.cardId,
                        tileIndex,
                        1 - context.SourcePlayerId,
                        false
                    ));

                    // 从列表中移除
                    enemies.RemoveAt(index);
                    enemyTiles.RemoveAt(index);
                }
            }

            UnityEngine.Debug.Log($"KurentiSpecialExecutor: 完成伤害分配");
        }
    }
}
