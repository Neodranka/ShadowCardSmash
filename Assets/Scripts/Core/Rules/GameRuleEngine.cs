using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;
using ShadowCardSmash.Core.Effects;

namespace ShadowCardSmash.Core.Rules
{
    /// <summary>
    /// 游戏规则引擎 - 核心游戏逻辑处理
    /// </summary>
    public class GameRuleEngine
    {
        private GameState _currentState;
        private EffectSystem _effectSystem;
        private TurnManager _turnManager;
        private CombatResolver _combatResolver;
        private EvolutionSystem _evolutionSystem;
        private ICardDatabase _cardDatabase;
        private System.Random _rng;
        private int _instanceIdCounter;

        public GameState CurrentState => _currentState;

        public GameRuleEngine(ICardDatabase cardDatabase, int? randomSeed = null)
        {
            _cardDatabase = cardDatabase;
            _rng = randomSeed.HasValue ? new System.Random(randomSeed.Value) : new System.Random();
            _instanceIdCounter = 1;

            // 创建效果系统
            _effectSystem = EffectSystemFactory.Create(_rng);
            _effectSystem.SetCardDatabase(cardDatabase);
            _effectSystem.SetInstanceIdGenerator(GenerateInstanceId);

            // 创建子系统
            _turnManager = new TurnManager(_effectSystem, cardDatabase, GenerateInstanceId);
            _combatResolver = new CombatResolver(_effectSystem, cardDatabase);
            _evolutionSystem = new EvolutionSystem(_effectSystem, cardDatabase);
        }

        /// <summary>
        /// 生成唯一实例ID
        /// </summary>
        public int GenerateInstanceId()
        {
            return _instanceIdCounter++;
        }

        /// <summary>
        /// 初始化游戏
        /// </summary>
        public void Initialize(GameState initialState)
        {
            _currentState = initialState;
            _rng = new System.Random(initialState.randomSeed);
        }

        /// <summary>
        /// 开始游戏（执行开局流程）
        /// </summary>
        public List<GameEvent> StartGame()
        {
            var events = new List<GameEvent>();

            // 洗牌
            ShuffleDeck(_currentState.players[0]);
            ShuffleDeck(_currentState.players[1]);

            // 发起始手牌
            // 先手4张，后手5张+补偿卡
            var drawEvents0 = DrawInitialHand(_currentState.players[0], 4);
            var drawEvents1 = DrawInitialHand(_currentState.players[1], 5);
            events.AddRange(drawEvents0);
            events.AddRange(drawEvents1);

            // 后手获得补偿卡
            if (_currentState.players[1].compensationCardId > 0)
            {
                var compensationDb = new CompensationCardDatabase();
                var compensationCard = compensationDb.GetCompensationCard(_currentState.players[1].compensationCardId);
                if (compensationCard != null)
                {
                    int instanceId = GenerateInstanceId();
                    var runtimeCard = RuntimeCard.FromCardData(compensationCard, instanceId, 1);
                    _currentState.players[1].hand.Add(runtimeCard);
                    events.Add(new CardDrawnEvent(1, compensationCard.cardId, instanceId, false, false));
                }
            }

            // 生成游戏开始事件
            events.Add(new GameStartEvent(0, _currentState.randomSeed));

            // 开始先手的第一回合
            var turnStartEvents = _turnManager.StartTurn(_currentState, 0);
            events.AddRange(turnStartEvents);

            // 抽牌阶段
            var drawPhaseEvents = _turnManager.DrawPhase(_currentState, 0);
            events.AddRange(drawPhaseEvents);

            return events;
        }

        /// <summary>
        /// 处理玩家操作
        /// </summary>
        public List<GameEvent> ProcessAction(PlayerAction action)
        {
            var events = new List<GameEvent>();

            // 验证操作
            if (!ValidateAction(action))
            {
                return events;
            }

            switch (action.actionType)
            {
                case ActionType.PlayCard:
                    events = ProcessPlayCard(action);
                    break;

                case ActionType.Attack:
                    events = ProcessAttack(action);
                    break;

                case ActionType.Evolve:
                    events = ProcessEvolve(action);
                    break;

                case ActionType.ActivateAmulet:
                    events = ProcessActivateAmulet(action);
                    break;

                case ActionType.EndTurn:
                    events = ProcessEndTurn(action);
                    break;

                case ActionType.Surrender:
                    events = ProcessSurrender(action);
                    break;
            }

            return events;
        }

        /// <summary>
        /// 验证操作合法性
        /// </summary>
        public bool ValidateAction(PlayerAction action)
        {
            // 检查游戏是否结束
            if (_currentState.IsGameOver())
                return false;

            // 检查是否轮到该玩家
            if (action.playerId != _currentState.currentPlayerId)
                return false;

            // 检查阶段
            if (_currentState.phase != GamePhase.Main && action.actionType != ActionType.Surrender)
                return false;

            var player = _currentState.GetPlayer(action.playerId);

            switch (action.actionType)
            {
                case ActionType.PlayCard:
                    // 检查手牌索引
                    if (action.handIndex < 0 || action.handIndex >= player.hand.Count)
                        return false;

                    var cardToPlay = player.hand[action.handIndex];
                    var cardData = _cardDatabase?.GetCardById(cardToPlay.cardId);

                    // 检查费用
                    if (cardData != null && cardData.cost > player.mana)
                        return false;

                    // 检查格子（随从/护符需要）
                    if (cardData != null && (cardData.cardType == CardType.Minion || cardData.cardType == CardType.Amulet))
                    {
                        if (action.tileIndex < 0 || action.tileIndex >= PlayerState.FIELD_SIZE)
                            return false;
                        if (!player.field[action.tileIndex].IsEmpty())
                            return false;
                    }
                    break;

                case ActionType.Attack:
                    var attacker = _currentState.FindCardByInstanceId(action.sourceInstanceId);
                    if (attacker == null || attacker.ownerId != action.playerId)
                        return false;

                    if (!_combatResolver.CanAttackTarget(_currentState, attacker,
                        action.targetInstanceId, action.targetIsPlayer))
                        return false;
                    break;

                case ActionType.Evolve:
                    if (!_evolutionSystem.CanUseEvolution(_currentState, action.playerId))
                        return false;

                    var minionToEvolve = _currentState.FindCardByInstanceId(action.sourceInstanceId);
                    if (!_evolutionSystem.CanEvolveMinion(_currentState, minionToEvolve, action.playerId))
                        return false;
                    break;

                case ActionType.ActivateAmulet:
                    var amulet = _currentState.FindCardByInstanceId(action.sourceInstanceId);
                    if (amulet == null || amulet.ownerId != action.playerId)
                        return false;

                    var amuletData = _cardDatabase?.GetCardById(amulet.cardId);
                    if (amuletData == null || amuletData.cardType != CardType.Amulet)
                        return false;
                    if (!amuletData.canActivate)
                        return false;
                    if (amulet.isSilenced)
                        return false;
                    if (amuletData.activateCost > player.mana)
                        return false;
                    break;
            }

            return true;
        }

        /// <summary>
        /// 处理使用卡牌
        /// </summary>
        private List<GameEvent> ProcessPlayCard(PlayerAction action)
        {
            var events = new List<GameEvent>();
            var player = _currentState.GetPlayer(action.playerId);
            var cardInHand = player.hand[action.handIndex];
            var cardData = _cardDatabase?.GetCardById(cardInHand.cardId);

            if (cardData == null) return events;

            // 扣除费用
            player.mana -= cardData.cost;

            // 从手牌移除
            player.hand.RemoveAt(action.handIndex);

            // 生成使用卡牌事件
            events.Add(new CardPlayedEvent(
                action.playerId,
                cardData.cardId,
                cardInHand.instanceId,
                action.tileIndex,
                cardData.cost
            ));

            // 根据卡牌类型处理
            switch (cardData.cardType)
            {
                case CardType.Minion:
                    events.AddRange(ProcessPlayMinion(action, cardInHand, cardData));
                    break;

                case CardType.Spell:
                    events.AddRange(ProcessPlaySpell(action, cardInHand, cardData));
                    break;

                case CardType.Amulet:
                    events.AddRange(ProcessPlayAmulet(action, cardInHand, cardData));
                    break;
            }

            return events;
        }

        /// <summary>
        /// 处理使用随从
        /// </summary>
        private List<GameEvent> ProcessPlayMinion(PlayerAction action, RuntimeCard cardInHand, CardData cardData)
        {
            var events = new List<GameEvent>();
            var player = _currentState.GetPlayer(action.playerId);

            // 创建运行时卡牌并放置
            var runtimeCard = RuntimeCard.FromCardData(cardData, cardInHand.instanceId, action.playerId);

            // 设置召唤失调
            runtimeCard.canAttack = runtimeCard.hasStorm;

            // 检查卡牌是否有初始关键词
            foreach (var effect in cardData.effects)
            {
                if (effect.effectType == EffectType.GainKeyword && effect.trigger == EffectTrigger.OnPlay)
                {
                    // 初始关键词在开幕效果中处理
                }
            }

            // 放置到格子
            player.field[action.tileIndex].PlaceUnit(runtimeCard);

            events.Add(new SummonEvent(
                action.playerId,
                runtimeCard.instanceId,
                cardData.cardId,
                action.tileIndex,
                action.playerId
            ));

            // 触发开幕效果
            var targets = action.targetInstanceId >= 0
                ? new List<RuntimeCard> { _currentState.FindCardByInstanceId(action.targetInstanceId) }
                : null;

            foreach (var effect in cardData.effects)
            {
                if (effect.trigger == EffectTrigger.OnPlay)
                {
                    var effectEvents = _effectSystem.ProcessEffect(
                        _currentState, runtimeCard, action.playerId, effect, targets);
                    events.AddRange(effectEvents);
                }
            }

            // 触发友方/敌方单位入场效果
            var allyPlayEvents = _effectSystem.TriggerEffects(
                _currentState, EffectTrigger.OnAllyPlay, runtimeCard, action.playerId);
            events.AddRange(allyPlayEvents);

            var enemyPlayEvents = _effectSystem.TriggerEffects(
                _currentState, EffectTrigger.OnEnemyPlay, runtimeCard, 1 - action.playerId);
            events.AddRange(enemyPlayEvents);

            return events;
        }

        /// <summary>
        /// 处理使用法术
        /// </summary>
        private List<GameEvent> ProcessPlaySpell(PlayerAction action, RuntimeCard cardInHand, CardData cardData)
        {
            var events = new List<GameEvent>();
            var player = _currentState.GetPlayer(action.playerId);

            // 获取目标
            List<RuntimeCard> targets = null;
            if (action.targetInstanceId >= 0)
            {
                var target = _currentState.FindCardByInstanceId(action.targetInstanceId);
                if (target != null)
                {
                    targets = new List<RuntimeCard> { target };
                }
            }

            // 执行所有效果
            foreach (var effect in cardData.effects)
            {
                if (effect.trigger == EffectTrigger.OnPlay)
                {
                    // 创建临时上下文处理玩家目标
                    var effectEvents = _effectSystem.ProcessEffect(
                        _currentState, null, action.playerId, effect, targets);
                    events.AddRange(effectEvents);
                }
            }

            // 法术进入墓地
            player.graveyard.Add(cardData.cardId);

            return events;
        }

        /// <summary>
        /// 处理使用护符
        /// </summary>
        private List<GameEvent> ProcessPlayAmulet(PlayerAction action, RuntimeCard cardInHand, CardData cardData)
        {
            var events = new List<GameEvent>();
            var player = _currentState.GetPlayer(action.playerId);

            // 创建运行时卡牌
            var runtimeCard = RuntimeCard.FromCardData(cardData, cardInHand.instanceId, action.playerId);
            runtimeCard.currentCountdown = cardData.countdown;

            // 放置到格子
            player.field[action.tileIndex].PlaceUnit(runtimeCard);

            events.Add(new SummonEvent(
                action.playerId,
                runtimeCard.instanceId,
                cardData.cardId,
                action.tileIndex,
                action.playerId
            ));

            // 触发开幕效果
            foreach (var effect in cardData.effects)
            {
                if (effect.trigger == EffectTrigger.OnPlay)
                {
                    var effectEvents = _effectSystem.ProcessEffect(
                        _currentState, runtimeCard, action.playerId, effect);
                    events.AddRange(effectEvents);
                }
            }

            return events;
        }

        /// <summary>
        /// 处理攻击
        /// </summary>
        private List<GameEvent> ProcessAttack(PlayerAction action)
        {
            var attacker = _currentState.FindCardByInstanceId(action.sourceInstanceId);

            if (action.targetIsPlayer)
            {
                return _combatResolver.ResolveAttackPlayer(_currentState, attacker, action.targetPlayerId);
            }
            else
            {
                var defender = _currentState.FindCardByInstanceId(action.targetInstanceId);
                return _combatResolver.ResolveAttack(_currentState, attacker, defender);
            }
        }

        /// <summary>
        /// 处理进化
        /// </summary>
        private List<GameEvent> ProcessEvolve(PlayerAction action)
        {
            var minion = _currentState.FindCardByInstanceId(action.sourceInstanceId);
            return _evolutionSystem.Evolve(_currentState, action.playerId, minion, true);
        }

        /// <summary>
        /// 处理启动护符
        /// </summary>
        private List<GameEvent> ProcessActivateAmulet(PlayerAction action)
        {
            var events = new List<GameEvent>();
            var player = _currentState.GetPlayer(action.playerId);
            var amulet = _currentState.FindCardByInstanceId(action.sourceInstanceId);
            var cardData = _cardDatabase?.GetCardById(amulet.cardId);

            if (cardData == null) return events;

            // 扣除启动费用
            player.mana -= cardData.activateCost;

            // 执行启动效果
            foreach (var effect in cardData.effects)
            {
                if (effect.trigger == EffectTrigger.OnActivate)
                {
                    List<RuntimeCard> targets = null;
                    if (action.targetInstanceId >= 0)
                    {
                        var target = _currentState.FindCardByInstanceId(action.targetInstanceId);
                        if (target != null) targets = new List<RuntimeCard> { target };
                    }

                    var effectEvents = _effectSystem.ProcessEffect(
                        _currentState, amulet, action.playerId, effect, targets);
                    events.AddRange(effectEvents);
                }
            }

            // 检查启动是否破坏护符
            // 规则：如果效果的parameters中包含"destroy_after"则破坏
            bool destroyAfterActivate = false;
            foreach (var effect in cardData.effects)
            {
                if (effect.trigger == EffectTrigger.OnActivate &&
                    effect.parameters != null &&
                    effect.parameters.Contains("destroy_after"))
                {
                    destroyAfterActivate = true;
                    break;
                }
            }

            events.Add(new AmuletActivatedEvent(
                action.playerId,
                amulet.instanceId,
                amulet.cardId,
                destroyAfterActivate
            ));

            // 如果需要破坏护符
            if (destroyAfterActivate)
            {
                // 从战场移除
                var ownerField = _currentState.players[action.playerId].field;
                for (int i = 0; i < ownerField.Length; i++)
                {
                    if (ownerField[i].occupant == amulet)
                    {
                        ownerField[i].RemoveUnit();

                        // 触发谢幕效果（如果未被沉默）
                        if (!amulet.isSilenced)
                        {
                            foreach (var effect in cardData.effects)
                            {
                                if (effect.trigger == EffectTrigger.OnDestroy)
                                {
                                    var destroyEvents = _effectSystem.ProcessEffect(
                                        _currentState, amulet, action.playerId, effect, null);
                                    events.AddRange(destroyEvents);
                                }
                            }
                        }

                        // 加入墓地
                        _currentState.players[action.playerId].graveyard.Add(amulet.cardId);

                        events.Add(new UnitDestroyedEvent(
                            action.playerId,
                            amulet.instanceId,
                            amulet.cardId,
                            i,
                            action.playerId,
                            false
                        ));

                        break;
                    }
                }
            }

            return events;
        }

        /// <summary>
        /// 处理结束回合
        /// </summary>
        private List<GameEvent> ProcessEndTurn(PlayerAction action)
        {
            var events = new List<GameEvent>();

            // 结束当前回合
            var endEvents = _turnManager.EndTurn(_currentState, action.playerId);
            events.AddRange(endEvents);

            // 切换到对方
            int nextPlayerId = _turnManager.GetNextPlayerId(action.playerId);

            // 开始对方回合
            var startEvents = _turnManager.StartTurn(_currentState, nextPlayerId);
            events.AddRange(startEvents);

            // 抽牌阶段
            var drawEvents = _turnManager.DrawPhase(_currentState, nextPlayerId);
            events.AddRange(drawEvents);

            return events;
        }

        /// <summary>
        /// 处理投降
        /// </summary>
        private List<GameEvent> ProcessSurrender(PlayerAction action)
        {
            var events = new List<GameEvent>();

            _currentState.phase = GamePhase.GameOver;
            events.Add(new GameOverEvent(
                1 - action.playerId,
                $"Player {action.playerId} surrendered"
            ));

            return events;
        }

        /// <summary>
        /// 获取可使用的手牌索引列表
        /// </summary>
        public List<int> GetPlayableCards(int playerId)
        {
            var result = new List<int>();
            var player = _currentState.GetPlayer(playerId);

            for (int i = 0; i < player.hand.Count; i++)
            {
                var card = player.hand[i];
                var cardData = _cardDatabase?.GetCardById(card.cardId);

                if (cardData == null) continue;

                // 检查费用
                if (cardData.cost > player.mana) continue;

                // 检查格子（随从/护符需要空格子）
                if (cardData.cardType == CardType.Minion || cardData.cardType == CardType.Amulet)
                {
                    if (player.GetEmptyTileCount() == 0) continue;
                }

                result.Add(i);
            }

            return result;
        }

        /// <summary>
        /// 获取卡牌的有效目标
        /// </summary>
        public List<RuntimeCard> GetValidTargets(int playerId, int handIndex)
        {
            var player = _currentState.GetPlayer(playerId);
            if (handIndex < 0 || handIndex >= player.hand.Count)
                return new List<RuntimeCard>();

            var card = player.hand[handIndex];
            var cardData = _cardDatabase?.GetCardById(card.cardId);

            if (cardData == null || cardData.effects.Count == 0)
                return new List<RuntimeCard>();

            // 检查第一个需要目标的效果
            foreach (var effect in cardData.effects)
            {
                if (effect.trigger == EffectTrigger.OnPlay &&
                    _effectSystem.RequiresTargetChoice(effect))
                {
                    return _effectSystem.GetValidTargets(_currentState, null, playerId, effect);
                }
            }

            return new List<RuntimeCard>();
        }

        /// <summary>
        /// 获取有效攻击目标
        /// </summary>
        public List<AttackTarget> GetValidAttackTargets(int attackerInstanceId)
        {
            var attacker = _currentState.FindCardByInstanceId(attackerInstanceId);
            if (attacker == null) return new List<AttackTarget>();

            return _combatResolver.GetValidAttackTargets(_currentState, attacker);
        }

        /// <summary>
        /// 检查是否可以进化
        /// </summary>
        public bool CanEvolve(int playerId, int instanceId)
        {
            if (!_evolutionSystem.CanUseEvolution(_currentState, playerId))
                return false;

            var minion = _currentState.FindCardByInstanceId(instanceId);
            return _evolutionSystem.CanEvolveMinion(_currentState, minion, playerId);
        }

        /// <summary>
        /// 获取可进化的随从
        /// </summary>
        public List<RuntimeCard> GetEvolvableMinions(int playerId)
        {
            return _evolutionSystem.GetEvolvableMinions(_currentState, playerId);
        }

        /// <summary>
        /// 获取状态副本
        /// </summary>
        public GameState GetStateCopy()
        {
            return _currentState.DeepCopy();
        }

        /// <summary>
        /// 洗牌
        /// </summary>
        private void ShuffleDeck(PlayerState player)
        {
            var deck = player.deck;
            int n = deck.Count;
            while (n > 1)
            {
                n--;
                int k = _rng.Next(n + 1);
                int temp = deck[k];
                deck[k] = deck[n];
                deck[n] = temp;
            }
        }

        /// <summary>
        /// 发起始手牌
        /// </summary>
        private List<GameEvent> DrawInitialHand(PlayerState player, int count)
        {
            var events = new List<GameEvent>();

            for (int i = 0; i < count && player.deck.Count > 0; i++)
            {
                int cardId = player.deck[0];
                player.deck.RemoveAt(0);

                int instanceId = GenerateInstanceId();
                var cardData = _cardDatabase?.GetCardById(cardId);

                RuntimeCard runtimeCard;
                if (cardData != null)
                {
                    runtimeCard = RuntimeCard.FromCardData(cardData, instanceId, player.playerId);
                }
                else
                {
                    runtimeCard = new RuntimeCard
                    {
                        instanceId = instanceId,
                        cardId = cardId,
                        ownerId = player.playerId
                    };
                }

                player.hand.Add(runtimeCard);
                events.Add(new CardDrawnEvent(player.playerId, cardId, instanceId, false, false));
            }

            return events;
        }
    }
}
