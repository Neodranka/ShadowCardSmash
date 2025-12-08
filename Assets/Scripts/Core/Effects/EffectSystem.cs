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

            // 创建上下文
            var context = new EffectContext
            {
                GameState = state,
                Source = source,
                SourcePlayerId = sourcePlayerId,
                Targets = targets,
                Value = effect.value,
                Parameters = effect.parameters ?? new List<string>(),
                ResultEvents = events,
                CardDatabase = _cardDatabase,
                GenerateInstanceId = _instanceIdGenerator
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

                    if (cardData?.effects == null)
                        continue;

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
    }
}
