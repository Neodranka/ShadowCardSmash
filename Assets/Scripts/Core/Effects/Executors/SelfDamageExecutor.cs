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

            // 检查参数是否要从目标获取值
            foreach (var param in context.Parameters)
            {
                if (param == "value_from:target_cost")
                {
                    // 从目标卡牌获取费用作为伤害值
                    if (context.Targets != null && context.Targets.Count > 0)
                    {
                        var target = context.Targets[0];
                        var targetCardData = context.CardDatabase?.GetCardById(target.cardId);
                        if (targetCardData != null)
                        {
                            damage = targetCardData.cost;
                            UnityEngine.Debug.Log($"SelfDamageExecutor: 从目标获取费用 {damage}");
                        }
                    }
                    break;
                }
            }

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

                // 触发 OnOwnerDamaged 效果
                TriggerOwnerDamagedEffects(context, actualDamage);

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

        /// <summary>
        /// 触发 OnOwnerDamaged 效果（玩家受伤时触发场上单位的效果）
        /// </summary>
        private void TriggerOwnerDamagedEffects(EffectContext context, int damageAmount)
        {
            var player = context.GameState.GetPlayer(context.SourcePlayerId);

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
                                conditionMet = context.GameState.currentPlayerId == context.SourcePlayerId;
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
                                context.SourcePlayerId,
                                unit.instanceId,
                                effect.value,
                                effect.secondaryValue,
                                null
                            ));

                            UnityEngine.Debug.Log($"SelfDamageExecutor: {cardData.cardName} 触发 OnOwnerDamaged, +{effect.value}/+{effect.secondaryValue}");
                        }
                    }
                }
            }
        }
    }
}
