using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects
{
    /// <summary>
    /// 效果系统 - 管理和执行所有卡牌效果
    /// </summary>
    public class EffectSystem
    {
        private Dictionary<EffectType, IEffectExecutor> _executors;
        private ITargetSelector _targetSelector;
        private IConditionChecker _conditionChecker;
        private ICardDatabase _cardDatabase;
        private System.Func<int> _instanceIdGenerator;

        public EffectSystem()
        {
            _executors = new Dictionary<EffectType, IEffectExecutor>();
            _targetSelector = new DefaultTargetSelector();
            _conditionChecker = new DefaultConditionChecker();
        }

        /// <summary>
        /// 设置目标选择器
        /// </summary>
        public void SetTargetSelector(ITargetSelector selector)
        {
            _targetSelector = selector;
        }

        /// <summary>
        /// 设置条件检查器
        /// </summary>
        public void SetConditionChecker(IConditionChecker checker)
        {
            _conditionChecker = checker;
        }

        /// <summary>
        /// 设置卡牌数据库
        /// </summary>
        public void SetCardDatabase(ICardDatabase database)
        {
            _cardDatabase = database;
        }

        /// <summary>
        /// 设置实例ID生成器
        /// </summary>
        public void SetInstanceIdGenerator(System.Func<int> generator)
        {
            _instanceIdGenerator = generator;
        }

        /// <summary>
        /// 注册效果执行器
        /// </summary>
        public void RegisterExecutor(EffectType type, IEffectExecutor executor)
        {
            _executors[type] = executor;
        }

        /// <summary>
        /// 获取效果执行器
        /// </summary>
        public IEffectExecutor GetExecutor(EffectType type)
        {
            _executors.TryGetValue(type, out var executor);
            return executor;
        }

        /// <summary>
        /// 处理单个效果
        /// </summary>
        /// <param name="state">游戏状态</param>
        /// <param name="source">效果来源</param>
        /// <param name="sourcePlayerId">来源玩家ID</param>
        /// <param name="effect">效果数据</param>
        /// <param name="chosenTargets">玩家选择的目标（如果需要）</param>
        /// <returns>产生的游戏事件</returns>
        public List<GameEvent> ProcessEffect(GameState state, RuntimeCard source, int sourcePlayerId,
            EffectData effect, List<RuntimeCard> chosenTargets = null)
        {
            return ProcessEffectWithPlayerTarget(state, source, sourcePlayerId, effect, chosenTargets, false, -1);
        }

        /// <summary>
        /// 处理单个效果（支持玩家作为目标）
        /// </summary>
        public List<GameEvent> ProcessEffectWithPlayerTarget(GameState state, RuntimeCard source, int sourcePlayerId,
            EffectData effect, List<RuntimeCard> chosenTargets, bool actionTargetIsPlayer, int actionTargetPlayerId,
            List<int> selectedTileIndices = null)
        {
            var events = new List<GameEvent>();

            // 检查条件
            if (!_conditionChecker.CheckCondition(state, source, sourcePlayerId,
                effect.conditionType, effect.conditionParams))
            {
                return events;
            }

            // 获取目标
            List<RuntimeCard> targets;
            if (chosenTargets != null && chosenTargets.Count > 0)
            {
                targets = chosenTargets;
            }
            else
            {
                targets = _targetSelector.SelectTargets(state, source, sourcePlayerId, effect.targetType);
            }

            // 检查条件表达式（新增的condition字段）
            if (!string.IsNullOrEmpty(effect.condition))
            {
                if (!CheckConditionExpression(state, source, sourcePlayerId, effect.condition))
                {
                    return events;
                }
            }

            // 创建上下文
            var context = new EffectContext
            {
                GameState = state,
                Source = source,
                SourcePlayerId = sourcePlayerId,
                Targets = targets,
                Value = effect.value,
                SecondaryValue = effect.secondaryValue,
                Condition = effect.condition ?? string.Empty,
                Parameters = effect.parameters ?? new List<string>(),
                ResultEvents = events,
                CardDatabase = _cardDatabase,
                GenerateInstanceId = _instanceIdGenerator,
                EffectSystem = this,
                SelectedTileIndices = selectedTileIndices
            };

            // 处理玩家目标
            if (effect.targetType == TargetType.EnemyPlayer)
            {
                context.TargetIsPlayer = true;
                context.TargetPlayerId = 1 - sourcePlayerId;
            }
            else if (effect.targetType == TargetType.AllyPlayer)
            {
                context.TargetIsPlayer = true;
                context.TargetPlayerId = sourcePlayerId;
            }
            else if (effect.targetType == TargetType.All)
            {
                // 全体目标（包括双方玩家）
                context.TargetAll = true;
            }
            else if (effect.targetType == TargetType.PlayerChoice && actionTargetIsPlayer)
            {
                // 玩家选择了敌方玩家作为目标
                context.TargetIsPlayer = true;
                context.TargetPlayerId = actionTargetPlayerId;
            }

            // 执行效果
            if (_executors.TryGetValue(effect.effectType, out var executor))
            {
                executor.Execute(context);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"No executor registered for effect type: {effect.effectType}");
            }

            return events;
        }

        /// <summary>
        /// 触发指定时机的所有效果
        /// </summary>
        /// <param name="state">游戏状态</param>
        /// <param name="trigger">触发时机</param>
        /// <param name="triggerSource">触发源（可能为null）</param>
        /// <param name="triggerPlayerId">触发者玩家ID</param>
        /// <returns>产生的游戏事件</returns>
        public List<GameEvent> TriggerEffects(GameState state, EffectTrigger trigger,
            RuntimeCard triggerSource, int triggerPlayerId)
        {
            var allEvents = new List<GameEvent>();

            // 遍历所有场上单位，触发对应效果
            for (int playerId = 0; playerId < 2; playerId++)
            {
                var player = state.GetPlayer(playerId);

                foreach (var tile in player.field)
                {
                    if (tile.occupant == null || tile.occupant.isSilenced)
                        continue;

                    var unit = tile.occupant;
                    var cardData = _cardDatabase?.GetCardById(unit.cardId);

                    // 处理卡牌自带的效果
                    if (cardData?.effects != null)
                    {
                        foreach (var effect in cardData.effects)
                        {
                            if (effect.trigger != trigger)
                                continue;

                            // 某些触发需要特殊处理
                            bool shouldTrigger = ShouldTriggerEffect(trigger, unit, triggerSource, playerId, triggerPlayerId);

                            if (shouldTrigger)
                            {
                                var events = ProcessEffect(state, unit, playerId, effect);
                                allEvents.AddRange(events);
                            }
                        }
                    }

                    // 处理被添加的效果（如渴血符文添加的效果）
                    if (unit.addedEffects != null)
                    {
                        foreach (var addedEffect in unit.addedEffects)
                        {
                            if (addedEffect.trigger != trigger)
                                continue;

                            bool shouldTrigger = ShouldTriggerEffect(trigger, unit, triggerSource, playerId, triggerPlayerId);

                            if (shouldTrigger)
                            {
                                // 创建临时 EffectData 来处理
                                var effect = new EffectData
                                {
                                    trigger = addedEffect.trigger,
                                    effectType = addedEffect.effectType,
                                    targetType = addedEffect.targetType,
                                    value = addedEffect.value
                                };

                                var events = ProcessEffect(state, unit, playerId, effect);
                                allEvents.AddRange(events);

                                UnityEngine.Debug.Log($"EffectSystem: 触发添加的效果 - {unit.instanceId} {addedEffect.effectType}");
                            }
                        }
                    }
                }
            }

            return allEvents;
        }

        /// <summary>
        /// 判断效果是否应该触发
        /// </summary>
        private bool ShouldTriggerEffect(EffectTrigger trigger, RuntimeCard listener,
            RuntimeCard triggerSource, int listenerPlayerId, int triggerPlayerId)
        {
            switch (trigger)
            {
                case EffectTrigger.OnTurnStart:
                case EffectTrigger.OnTurnEnd:
                case EffectTrigger.OnOwnerTurnEnd:
                    // 只在自己的回合触发
                    return listenerPlayerId == triggerPlayerId;

                case EffectTrigger.OnAllyPlay:
                    // 友方单位入场时触发
                    return listenerPlayerId == triggerPlayerId &&
                           (triggerSource == null || triggerSource.instanceId != listener.instanceId);

                case EffectTrigger.OnEnemyPlay:
                    // 敌方单位入场时触发
                    return listenerPlayerId != triggerPlayerId;

                case EffectTrigger.OnAllyDestroy:
                    // 友方单位被破坏时触发
                    return listenerPlayerId == triggerPlayerId &&
                           (triggerSource == null || triggerSource.instanceId != listener.instanceId);

                case EffectTrigger.OnEnemyDestroy:
                    // 敌方单位被破坏时触发
                    return listenerPlayerId != triggerPlayerId;

                case EffectTrigger.OnDraw:
                    // 抽牌时触发（只对抽牌玩家的己方单位触发）
                    return listenerPlayerId == triggerPlayerId;

                default:
                    return true;
            }
        }

        /// <summary>
        /// 检查效果是否需要玩家选择目标
        /// </summary>
        public bool RequiresTargetChoice(EffectData effect)
        {
            return _targetSelector.RequiresPlayerChoice(effect.targetType);
        }

        /// <summary>
        /// 获取效果的有效目标列表
        /// </summary>
        public List<RuntimeCard> GetValidTargets(GameState state, RuntimeCard source,
            int sourcePlayerId, EffectData effect)
        {
            return _targetSelector.GetValidTargets(state, source, sourcePlayerId, effect.targetType);
        }

        /// <summary>
        /// 根据目标类型获取有效目标列表
        /// </summary>
        public List<RuntimeCard> GetValidTargetsByType(GameState state, RuntimeCard source,
            int sourcePlayerId, TargetType targetType)
        {
            return _targetSelector.GetValidTargets(state, source, sourcePlayerId, targetType);
        }

        /// <summary>
        /// 检查条件表达式
        /// </summary>
        private bool CheckConditionExpression(GameState state, RuntimeCard source, int sourcePlayerId, string condition)
        {
            if (string.IsNullOrEmpty(condition)) return true;

            var player = state.GetPlayer(sourcePlayerId);

            // 支持 && 连接的复合条件
            if (condition.Contains("&&"))
            {
                var parts = condition.Split(new string[] { "&&" }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (!CheckSingleCondition(state, source, sourcePlayerId, player, part.Trim()))
                    {
                        return false;
                    }
                }
                return true;
            }

            return CheckSingleCondition(state, source, sourcePlayerId, player, condition);
        }

        /// <summary>
        /// 检查单个条件表达式
        /// </summary>
        private bool CheckSingleCondition(GameState state, RuntimeCard source, int sourcePlayerId,
            PlayerState player, string condition)
        {
            // 布尔条件检查
            switch (condition)
            {
                case "is_evolved":
                    return source != null && source.isEvolved;
                case "minion_destroyed_this_turn":
                    return player.minionDestroyedThisTurn;
                case "is_enemy_turn":
                    return state.currentPlayerId != sourcePlayerId;
                case "is_my_turn":
                case "is_own_turn":
                    return state.currentPlayerId == sourcePlayerId;
                case "has_other_minions":
                    // 检查场上是否有其他随从（排除自己）
                    foreach (var tile in player.field)
                    {
                        if (tile.occupant != null && (source == null || tile.occupant.instanceId != source.instanceId))
                        {
                            return true;
                        }
                    }
                    return false;
            }

            // 解析简单条件表达式: "variable>=value" 或 "variable<=value"
            // 支持: total_self_damage>=10, self_damage_this_turn>=5 等

            if (condition.Contains(">="))
            {
                var parts = condition.Split(new string[] { ">=" }, System.StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string varName = parts[0].Trim();
                    if (int.TryParse(parts[1].Trim(), out int threshold))
                    {
                        int varValue = GetConditionVariable(player, source, varName);
                        return varValue >= threshold;
                    }
                }
            }
            else if (condition.Contains("<="))
            {
                var parts = condition.Split(new string[] { "<=" }, System.StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string varName = parts[0].Trim();
                    if (int.TryParse(parts[1].Trim(), out int threshold))
                    {
                        int varValue = GetConditionVariable(player, source, varName);
                        return varValue <= threshold;
                    }
                }
            }
            else if (condition.Contains(">"))
            {
                var parts = condition.Split('>');
                if (parts.Length == 2)
                {
                    string varName = parts[0].Trim();
                    if (int.TryParse(parts[1].Trim(), out int threshold))
                    {
                        int varValue = GetConditionVariable(player, source, varName);
                        return varValue > threshold;
                    }
                }
            }
            else if (condition.Contains("<"))
            {
                var parts = condition.Split('<');
                if (parts.Length == 2)
                {
                    string varName = parts[0].Trim();
                    if (int.TryParse(parts[1].Trim(), out int threshold))
                    {
                        int varValue = GetConditionVariable(player, source, varName);
                        return varValue < threshold;
                    }
                }
            }

            UnityEngine.Debug.LogWarning($"EffectSystem: 无法解析条件表达式: {condition}");
            return true; // 默认通过
        }

        /// <summary>
        /// 获取条件变量的值
        /// </summary>
        private int GetConditionVariable(PlayerState player, RuntimeCard source, string varName)
        {
            switch (varName)
            {
                case "total_self_damage":
                    return player.totalSelfDamage;
                case "self_damage_this_turn":
                    return player.selfDamageThisTurn;
                case "self_damage_count":
                    // 本局游戏中玩家在自己回合受到伤害的次数
                    return player.selfDamageCount;
                case "health":
                    return player.health;
                case "max_health":
                    return player.maxHealth;
                case "mana":
                    return player.mana;
                case "hand_count":
                    return player.hand.Count;
                case "deck_count":
                    return player.deck.Count;
                case "field_count":
                    return player.GetEmptyTileCount() == 0 ? PlayerState.FIELD_SIZE : PlayerState.FIELD_SIZE - player.GetEmptyTileCount();
                case "source_attack":
                    return source?.currentAttack ?? 0;
                case "source_health":
                    return source?.currentHealth ?? 0;
                default:
                    UnityEngine.Debug.LogWarning($"EffectSystem: 未知条件变量: {varName}");
                    return 0;
            }
        }
    }
}
