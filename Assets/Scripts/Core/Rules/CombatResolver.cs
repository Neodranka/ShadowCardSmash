using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;
using ShadowCardSmash.Core.Effects;

namespace ShadowCardSmash.Core.Rules
{
    /// <summary>
    /// 战斗结算器 - 处理攻击和战斗逻辑
    /// </summary>
    public class CombatResolver
    {
        private EffectSystem _effectSystem;
        private ICardDatabase _cardDatabase;

        public CombatResolver(EffectSystem effectSystem, ICardDatabase cardDatabase)
        {
            _effectSystem = effectSystem;
            _cardDatabase = cardDatabase;
        }

        /// <summary>
        /// 结算随从对随从的攻击
        /// </summary>
        public List<GameEvent> ResolveAttack(GameState state, RuntimeCard attacker, RuntimeCard defender)
        {
            var events = new List<GameEvent>();
            int attackerPlayerId = attacker.ownerId;

            // 生成攻击事件
            events.Add(new AttackEvent(
                attackerPlayerId,
                attacker.instanceId,
                defender.instanceId,
                false,
                -1
            ));

            // 触发攻击时效果
            if (!attacker.isSilenced)
            {
                var attackEvents = _effectSystem.TriggerEffects(state, EffectTrigger.OnAttack, attacker, attackerPlayerId);
                events.AddRange(attackEvents);
            }

            // 双方互相造成伤害
            int attackerDamage = attacker.currentAttack;
            int defenderDamage = defender.currentAttack;

            // 攻击者对防守者造成伤害
            int actualAttackerDamage = defender.TakeDamage(attackerDamage);
            events.Add(new DamageEvent(
                attackerPlayerId,
                attacker.instanceId,
                defender.instanceId,
                actualAttackerDamage,
                false,
                -1
            ));

            // 吸血效果：攻击者造成伤害后回复玩家生命
            if (attacker.hasDrain && actualAttackerDamage > 0 && !attacker.isSilenced)
            {
                var attackerOwner = state.GetPlayer(attackerPlayerId);
                attackerOwner.Heal(actualAttackerDamage);
                events.Add(new DrainEvent(
                    attackerPlayerId,
                    attacker.instanceId,
                    actualAttackerDamage,
                    actualAttackerDamage
                ));
                UnityEngine.Debug.Log($"CombatResolver: 吸血效果 - 玩家{attackerPlayerId}回复{actualAttackerDamage}生命");
            }

            // 防守者对攻击者造成伤害（反击）
            int actualDefenderDamage = attacker.TakeDamage(defenderDamage);
            events.Add(new DamageEvent(
                defender.ownerId,
                defender.instanceId,
                attacker.instanceId,
                actualDefenderDamage,
                false,
                -1
            ));

            // 防守者的吸血效果
            if (defender.hasDrain && actualDefenderDamage > 0 && !defender.isSilenced)
            {
                var defenderOwner = state.GetPlayer(defender.ownerId);
                defenderOwner.Heal(actualDefenderDamage);
                events.Add(new DrainEvent(
                    defender.ownerId,
                    defender.instanceId,
                    actualDefenderDamage,
                    actualDefenderDamage
                ));
                UnityEngine.Debug.Log($"CombatResolver: 吸血效果 - 玩家{defender.ownerId}回复{actualDefenderDamage}生命");
            }

            // 触发受伤效果（只有实际受到伤害才触发）
            if (!attacker.isSilenced && actualDefenderDamage > 0)
            {
                var damagedEvents = _effectSystem.TriggerEffects(state, EffectTrigger.OnDamaged, attacker, attackerPlayerId);
                events.AddRange(damagedEvents);
            }
            if (!defender.isSilenced && actualAttackerDamage > 0)
            {
                var damagedEvents = _effectSystem.TriggerEffects(state, EffectTrigger.OnDamaged, defender, defender.ownerId);
                events.AddRange(damagedEvents);
            }

            // 设置攻击者状态
            attacker.attackedThisTurn = true;
            attacker.canAttack = false;

            // 检查死亡
            var deathEvents = CheckDeaths(state, attacker, defender);
            events.AddRange(deathEvents);

            return events;
        }

        /// <summary>
        /// 结算随从对玩家的攻击
        /// </summary>
        public List<GameEvent> ResolveAttackPlayer(GameState state, RuntimeCard attacker, int targetPlayerId)
        {
            var events = new List<GameEvent>();
            int attackerPlayerId = attacker.ownerId;
            var targetPlayer = state.GetPlayer(targetPlayerId);

            // 生成攻击事件
            events.Add(new AttackEvent(
                attackerPlayerId,
                attacker.instanceId,
                -1,
                true,
                targetPlayerId
            ));

            // 触发攻击时效果
            if (!attacker.isSilenced)
            {
                var attackEvents = _effectSystem.TriggerEffects(state, EffectTrigger.OnAttack, attacker, attackerPlayerId);
                events.AddRange(attackEvents);
            }

            // 对玩家造成伤害
            int damage = attacker.currentAttack;
            int actualDamage = targetPlayer.TakeDamage(damage);

            events.Add(new DamageEvent(
                attackerPlayerId,
                attacker.instanceId,
                -1,
                actualDamage,
                true,
                targetPlayerId
            ));

            // 吸血效果：攻击者造成伤害后回复玩家生命
            if (attacker.hasDrain && actualDamage > 0 && !attacker.isSilenced)
            {
                var attackerOwner = state.GetPlayer(attackerPlayerId);
                attackerOwner.Heal(actualDamage);
                events.Add(new DrainEvent(
                    attackerPlayerId,
                    attacker.instanceId,
                    actualDamage,
                    actualDamage
                ));
                UnityEngine.Debug.Log($"CombatResolver: 吸血效果 - 玩家{attackerPlayerId}回复{actualDamage}生命");
            }

            // 设置攻击者状态
            attacker.attackedThisTurn = true;
            attacker.canAttack = false;

            // 检查玩家死亡
            if (targetPlayer.IsDead())
            {
                state.phase = GamePhase.GameOver;
                events.Add(new GameOverEvent(
                    attackerPlayerId,
                    $"Player {targetPlayerId} was defeated in combat"
                ));
            }

            return events;
        }

        /// <summary>
        /// 检查攻击者是否可以攻击指定目标
        /// </summary>
        public bool CanAttackTarget(GameState state, RuntimeCard attacker, int targetInstanceId, bool targetIsPlayer)
        {
            // 检查攻击者基本状态
            if (!attacker.canAttack) return false;
            if (attacker.attackedThisTurn) return false;
            if (attacker.currentAttack <= 0) return false;

            var attackerPlayer = state.GetPlayer(attacker.ownerId);
            var opponentPlayer = state.GetPlayer(1 - attacker.ownerId);

            // 检查是否为敌方目标
            if (targetIsPlayer)
            {
                // 攻击敌方玩家
                // 检查是否有守护阻挡
                if (opponentPlayer.HasWardMinion())
                {
                    return false;
                }

                // 检查召唤失调（突进不能攻击玩家，疾驰可以）
                // 只有在入场当回合，且有突进但没有疾驰时，才不能攻击玩家
                if (attacker.summonedThisTurn && attacker.hasRush && !attacker.hasStorm)
                {
                    return false;
                }

                return true;
            }
            else
            {
                // 攻击敌方随从
                var target = state.FindCardByInstanceId(targetInstanceId);
                if (target == null) return false;

                // 确保目标是敌方单位
                if (target.ownerId == attacker.ownerId) return false;

                // 检查守护
                if (opponentPlayer.HasWardMinion())
                {
                    // 如果有守护，必须攻击守护随从
                    if (!target.hasWard)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// 获取有效的攻击目标
        /// </summary>
        public List<AttackTarget> GetValidAttackTargets(GameState state, RuntimeCard attacker)
        {
            var targets = new List<AttackTarget>();

            if (!attacker.canAttack || attacker.attackedThisTurn || attacker.currentAttack <= 0)
            {
                return targets;
            }

            var opponentPlayer = state.GetPlayer(1 - attacker.ownerId);
            bool hasWard = opponentPlayer.HasWardMinion();
            // 只有在入场当回合，且有突进但没有疾驰时，才不能攻击玩家
            bool canAttackPlayer = !(attacker.summonedThisTurn && attacker.hasRush && !attacker.hasStorm);

            if (hasWard)
            {
                // 只能攻击守护随从
                foreach (var tile in opponentPlayer.field)
                {
                    if (tile.occupant != null && tile.occupant.hasWard)
                    {
                        targets.Add(new AttackTarget
                        {
                            instanceId = tile.occupant.instanceId,
                            isPlayer = false,
                            playerId = -1
                        });
                    }
                }
            }
            else
            {
                // 可以攻击任意敌方随从
                foreach (var tile in opponentPlayer.field)
                {
                    if (tile.occupant != null)
                    {
                        // 护符不能被攻击
                        var cardData = _cardDatabase?.GetCardById(tile.occupant.cardId);
                        if (cardData != null && cardData.cardType == CardType.Amulet)
                        {
                            continue;
                        }

                        targets.Add(new AttackTarget
                        {
                            instanceId = tile.occupant.instanceId,
                            isPlayer = false,
                            playerId = -1
                        });
                    }
                }

                // 可以攻击敌方玩家（如果没有守护且不是纯突进）
                if (canAttackPlayer)
                {
                    targets.Add(new AttackTarget
                    {
                        instanceId = -1,
                        isPlayer = true,
                        playerId = 1 - attacker.ownerId
                    });
                }
            }

            return targets;
        }

        /// <summary>
        /// 检查死亡并处理
        /// </summary>
        private List<GameEvent> CheckDeaths(GameState state, RuntimeCard attacker, RuntimeCard defender)
        {
            var events = new List<GameEvent>();

            // 检查防守者死亡
            if (defender.IsDead())
            {
                var tile = state.FindTileByInstanceId(defender.instanceId);
                if (tile != null)
                {
                    // 触发谢幕
                    if (!defender.isSilenced)
                    {
                        var destroyEvents = _effectSystem.TriggerEffects(
                            state, EffectTrigger.OnDestroy, defender, defender.ownerId);
                        events.AddRange(destroyEvents);
                    }

                    // 移除并加入墓地
                    var owner = state.GetPlayer(defender.ownerId);
                    owner.graveyard.Add(defender.cardId);
                    tile.RemoveUnit();

                    events.Add(new UnitDestroyedEvent(
                        attacker.ownerId,
                        defender.instanceId,
                        defender.cardId,
                        tile.tileIndex,
                        defender.ownerId,
                        false
                    ));

                    // 触发敌方/友方单位被破坏效果
                    var allyDestroyEvents = _effectSystem.TriggerEffects(
                        state, EffectTrigger.OnAllyDestroy, defender, defender.ownerId);
                    events.AddRange(allyDestroyEvents);

                    var enemyDestroyEvents = _effectSystem.TriggerEffects(
                        state, EffectTrigger.OnEnemyDestroy, defender, attacker.ownerId);
                    events.AddRange(enemyDestroyEvents);
                }
            }

            // 检查攻击者死亡
            if (attacker.IsDead())
            {
                var tile = state.FindTileByInstanceId(attacker.instanceId);
                if (tile != null)
                {
                    // 触发谢幕
                    if (!attacker.isSilenced)
                    {
                        var destroyEvents = _effectSystem.TriggerEffects(
                            state, EffectTrigger.OnDestroy, attacker, attacker.ownerId);
                        events.AddRange(destroyEvents);
                    }

                    // 移除并加入墓地
                    var owner = state.GetPlayer(attacker.ownerId);
                    owner.graveyard.Add(attacker.cardId);
                    tile.RemoveUnit();

                    events.Add(new UnitDestroyedEvent(
                        defender.ownerId,
                        attacker.instanceId,
                        attacker.cardId,
                        tile.tileIndex,
                        attacker.ownerId,
                        false
                    ));

                    // 触发敌方/友方单位被破坏效果
                    var allyDestroyEvents = _effectSystem.TriggerEffects(
                        state, EffectTrigger.OnAllyDestroy, attacker, attacker.ownerId);
                    events.AddRange(allyDestroyEvents);

                    var enemyDestroyEvents = _effectSystem.TriggerEffects(
                        state, EffectTrigger.OnEnemyDestroy, attacker, defender.ownerId);
                    events.AddRange(enemyDestroyEvents);
                }
            }

            return events;
        }
    }

    /// <summary>
    /// 攻击目标信息
    /// </summary>
    public class AttackTarget
    {
        public int instanceId;
        public bool isPlayer;
        public int playerId;
    }
}
