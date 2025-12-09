using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;
using ShadowCardSmash.Core.Effects;

namespace ShadowCardSmash.Core.Rules
{
    /// <summary>
    /// 回合管理器 - 处理回合流程
    /// </summary>
    public class TurnManager
    {
        private EffectSystem _effectSystem;
        private ICardDatabase _cardDatabase;
        private System.Func<int> _instanceIdGenerator;

        public TurnManager(EffectSystem effectSystem, ICardDatabase cardDatabase,
            System.Func<int> instanceIdGenerator)
        {
            _effectSystem = effectSystem;
            _cardDatabase = cardDatabase;
            _instanceIdGenerator = instanceIdGenerator;
        }

        /// <summary>
        /// 开始回合
        /// </summary>
        public List<GameEvent> StartTurn(GameState state, int playerId)
        {
            var events = new List<GameEvent>();
            var player = state.GetPlayer(playerId);

            // 设置当前玩家
            state.currentPlayerId = playerId;
            state.phase = GamePhase.TurnStart;

            // 增加回合数（只在先手回合开始时增加）
            if (playerId == 0)
            {
                state.turnNumber++;
            }

            // 增加费用上限（不超过10）
            int oldMaxMana = player.maxMana;
            if (player.maxMana < PlayerState.MAX_MANA)
            {
                player.maxMana++;
            }

            // 恢复费用
            player.mana = player.maxMana;

            // 重置进化标记
            player.hasEvolvedThisTurn = false;

            // 重置回合自伤计数
            player.ResetTurnSelfDamage();

            // 重置所有随从的攻击状态
            foreach (var tile in player.field)
            {
                if (tile.occupant != null)
                {
                    tile.occupant.canAttack = true;
                    tile.occupant.attackedThisTurn = false;
                    tile.occupant.summonedThisTurn = false; // 不再是入场当回合
                }
            }

            // 处理护符倒计时
            var countdownEvents = ProcessCountdowns(state, playerId);
            events.AddRange(countdownEvents);

            // 处理格子效果
            var tileEffectEvents = ProcessTileEffects(state, playerId);
            events.AddRange(tileEffectEvents);

            // 生成回合开始事件
            events.Add(new TurnStartEvent(playerId, state.turnNumber, player.maxMana));

            // 触发回合开始效果
            var triggerEvents = _effectSystem.TriggerEffects(state, EffectTrigger.OnTurnStart, null, playerId);
            events.AddRange(triggerEvents);

            return events;
        }

        /// <summary>
        /// 抽牌阶段
        /// </summary>
        public List<GameEvent> DrawPhase(GameState state, int playerId)
        {
            var events = new List<GameEvent>();
            var player = state.GetPlayer(playerId);

            state.phase = GamePhase.Draw;

            // 抽1张牌
            if (player.IsDeckEmpty())
            {
                // 疲劳伤害
                player.fatigueCounter++;
                int fatigueDamage = player.fatigueCounter;
                player.TakeDamage(fatigueDamage);

                events.Add(new FatigueEvent(playerId, fatigueDamage, player.fatigueCounter));

                // 检查死亡
                if (player.IsDead())
                {
                    state.phase = GamePhase.GameOver;
                    events.Add(new GameOverEvent(1 - playerId, $"Player {playerId} died from fatigue"));
                }
            }
            else
            {
                // 从牌库抽牌
                int cardId = player.deck[0];
                player.deck.RemoveAt(0);

                if (player.IsHandFull())
                {
                    // 手牌满，进墓地
                    player.graveyard.Add(cardId);
                    events.Add(new CardDrawnEvent(playerId, cardId, -1, false, true));
                }
                else
                {
                    // 加入手牌
                    int instanceId = _instanceIdGenerator();
                    var cardData = _cardDatabase?.GetCardById(cardId);

                    RuntimeCard runtimeCard;
                    if (cardData != null)
                    {
                        runtimeCard = RuntimeCard.FromCardData(cardData, instanceId, playerId);
                    }
                    else
                    {
                        runtimeCard = new RuntimeCard
                        {
                            instanceId = instanceId,
                            cardId = cardId,
                            ownerId = playerId
                        };
                    }

                    player.hand.Add(runtimeCard);
                    events.Add(new CardDrawnEvent(playerId, cardId, instanceId, false, false));
                }
            }

            // 切换到主要阶段
            state.phase = GamePhase.Main;

            return events;
        }

        /// <summary>
        /// 结束回合
        /// </summary>
        public List<GameEvent> EndTurn(GameState state, int playerId)
        {
            var events = new List<GameEvent>();

            state.phase = GamePhase.TurnEnd;

            // 触发回合结束效果
            var triggerEvents = _effectSystem.TriggerEffects(state, EffectTrigger.OnTurnEnd, null, playerId);
            events.AddRange(triggerEvents);

            // 生成回合结束事件
            events.Add(new TurnEndEvent(playerId));

            return events;
        }

        /// <summary>
        /// 获取下一个玩家ID
        /// </summary>
        public int GetNextPlayerId(int currentPlayerId)
        {
            return 1 - currentPlayerId;
        }

        /// <summary>
        /// 处理护符倒计时
        /// </summary>
        private List<GameEvent> ProcessCountdowns(GameState state, int playerId)
        {
            var events = new List<GameEvent>();
            var player = state.GetPlayer(playerId);

            for (int i = 0; i < player.field.Length; i++)
            {
                var tile = player.field[i];
                if (tile.occupant == null) continue;

                var unit = tile.occupant;

                // 检查是否是护符且有倒计时
                var cardData = _cardDatabase?.GetCardById(unit.cardId);
                if (cardData == null || cardData.cardType != CardType.Amulet) continue;
                if (unit.currentCountdown <= 0) continue;

                // 减少倒计时
                unit.currentCountdown--;

                events.Add(new CountdownTickEvent(playerId, unit.instanceId, unit.cardId, unit.currentCountdown));

                // 倒计时归零，破坏护符
                if (unit.currentCountdown == 0)
                {
                    // 触发谢幕效果（如果未被沉默）
                    if (!unit.isSilenced)
                    {
                        var destroyEvents = _effectSystem.TriggerEffects(state, EffectTrigger.OnDestroy, unit, playerId);
                        events.AddRange(destroyEvents);
                    }

                    // 移除护符
                    tile.RemoveUnit();
                    player.graveyard.Add(unit.cardId);

                    events.Add(new UnitDestroyedEvent(playerId, unit.instanceId, unit.cardId, i, playerId, false));
                }
            }

            return events;
        }

        /// <summary>
        /// 处理格子效果
        /// </summary>
        private List<GameEvent> ProcessTileEffects(GameState state, int playerId)
        {
            var events = new List<GameEvent>();
            var player = state.GetPlayer(playerId);

            foreach (var tile in player.field)
            {
                // 减少格子效果持续时间
                tile.TickEffects();
                tile.RemoveExpiredEffects();

                // 处理回合开始触发的格子效果
                if (tile.occupant != null)
                {
                    foreach (var effect in tile.effects)
                    {
                        if (effect.triggerTiming != EffectTrigger.OnTurnStart) continue;

                        // 根据效果类型处理
                        switch (effect.effectType)
                        {
                            case EffectType.Damage:
                                tile.occupant.TakeDamage(effect.value);
                                events.Add(new DamageEvent(
                                    -1, // 格子效果无来源
                                    -1,
                                    tile.occupant.instanceId,
                                    effect.value,
                                    false,
                                    -1
                                ));

                                // 检查死亡
                                if (tile.occupant.IsDead())
                                {
                                    var unit = tile.occupant;
                                    tile.RemoveUnit();
                                    player.graveyard.Add(unit.cardId);
                                    events.Add(new UnitDestroyedEvent(
                                        -1, unit.instanceId, unit.cardId, tile.tileIndex, playerId, false
                                    ));
                                }
                                break;

                            case EffectType.Heal:
                                int oldHealth = tile.occupant.currentHealth;
                                tile.occupant.Heal(effect.value);
                                events.Add(new HealEvent(
                                    -1,
                                    tile.occupant.instanceId,
                                    effect.value,
                                    tile.occupant.currentHealth - oldHealth,
                                    false,
                                    -1
                                ));
                                break;

                            case EffectType.Buff:
                                tile.occupant.currentAttack += effect.value;
                                events.Add(new BuffEvent(-1, tile.occupant.instanceId, effect.value, 0, null));
                                break;
                        }
                    }
                }
            }

            return events;
        }
    }
}
