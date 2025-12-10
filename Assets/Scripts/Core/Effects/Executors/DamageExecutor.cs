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

            // 检查是否使用目标属性作为伤害值
            if (context.Parameters != null && context.Parameters.Count > 0)
            {
                foreach (var param in context.Parameters)
                {
                    if (param.StartsWith("value_from:"))
                    {
                        string valueSource = param.Substring("value_from:".Length);
                        if (valueSource == "target_attack" && context.Targets.Count > 0)
                        {
                            // 使用目标的攻击力作为伤害值
                            var target = context.Targets[0];
                            damage = target.currentAttack;
                            UnityEngine.Debug.Log($"DamageExecutor: 使用目标攻击力作为伤害值: {damage}");
                        }
                    }
                }
            }

            // 如果是全体目标（所有随从+双方玩家）
            if (context.TargetAll)
            {
                // 对所有随从造成伤害
                foreach (var target in context.Targets)
                {
                    if (target == null) continue;
                    DamageMinion(context, target, damage, sourceInstanceId);
                }

                // 对双方玩家造成伤害
                DamagePlayer(context, 0, damage, sourceInstanceId);
                DamagePlayer(context, 1, damage, sourceInstanceId);

                return;
            }

            // 如果目标是玩家
            if (context.TargetIsPlayer)
            {
                DamagePlayer(context, context.TargetPlayerId, damage, sourceInstanceId);
                return;
            }

            // 对随从造成伤害
            foreach (var target in context.Targets)
            {
                if (target == null) continue;
                DamageMinion(context, target, damage, sourceInstanceId);
            }
        }

        private void DamagePlayer(EffectContext context, int playerId, int damage, int sourceInstanceId)
        {
            var targetPlayer = context.GameState.GetPlayer(playerId);
            int actualDamage;

            // 如果是对自己玩家造成伤害，使用 TakeSelfDamage 来统计自伤
            if (playerId == context.SourcePlayerId)
            {
                actualDamage = targetPlayer.TakeSelfDamage(damage);
            }
            else
            {
                actualDamage = targetPlayer.TakeDamage(damage);
            }

            context.AddEvent(new DamageEvent(
                context.SourcePlayerId,
                sourceInstanceId,
                -1,
                damage,
                true,
                playerId
            ));

            // 如果是对自己玩家造成伤害，触发 OnOwnerDamaged 效果
            if (actualDamage > 0)
            {
                TriggerOwnerDamagedEffects(context, playerId, actualDamage);
            }

            // 检查玩家是否死亡
            if (targetPlayer.IsDead())
            {
                context.GameState.phase = GamePhase.GameOver;
                context.AddEvent(new GameOverEvent(
                    context.SourcePlayerId,
                    $"Player {playerId} health reduced to {targetPlayer.health}"
                ));
            }
        }

        /// <summary>
        /// 触发 OnOwnerDamaged 效果（玩家受伤时触发场上单位的效果）
        /// </summary>
        private void TriggerOwnerDamagedEffects(EffectContext context, int damagedPlayerId, int damageAmount)
        {
            var player = context.GameState.GetPlayer(damagedPlayerId);

            foreach (var tile in player.field)
            {
                if (tile.occupant == null || tile.occupant.isSilenced)
                    continue;

                var unit = tile.occupant;
                var cardData = context.CardDatabase?.GetCardById(unit.cardId);

                if (cardData?.effects == null)
                    continue;

                foreach (var effect in cardData.effects)
                {
                    if (effect.trigger != EffectTrigger.OnOwnerDamaged)
                        continue;

                    // 检查条件（如 is_own_turn）
                    bool conditionMet = true;
                    if (!string.IsNullOrEmpty(effect.condition))
                    {
                        switch (effect.condition)
                        {
                            case "is_own_turn":
                            case "is_my_turn":
                                conditionMet = context.GameState.currentPlayerId == damagedPlayerId;
                                break;
                        }
                    }

                    if (conditionMet)
                    {
                        // 执行效果（如 Buff）
                        if (effect.effectType == EffectType.Buff)
                        {
                            unit.currentAttack += effect.value;
                            unit.currentHealth += effect.secondaryValue;
                            unit.maxHealth += effect.secondaryValue;

                            context.AddEvent(new BuffEvent(
                                damagedPlayerId,
                                unit.instanceId,
                                effect.value,
                                effect.secondaryValue,
                                null
                            ));

                            UnityEngine.Debug.Log($"DamageExecutor: {cardData.cardName} 触发 OnOwnerDamaged, +{effect.value}/+{effect.secondaryValue}");
                        }
                    }
                }
            }
        }

        private void DamageMinion(EffectContext context, RuntimeCard target, int damage, int sourceInstanceId)
        {
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

                    // 记录本回合有随从被破坏（双方都记录）
                    context.GameState.GetPlayer(0).RecordMinionDestroyed();
                    context.GameState.GetPlayer(1).RecordMinionDestroyed();

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
