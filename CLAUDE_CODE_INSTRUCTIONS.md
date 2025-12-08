# Claude Code 开发指令

本文档包含分阶段的开发任务，配合 `GAME_DESIGN_DOCUMENT.md` 使用。

---

## 开发指令使用说明

将以下指令分阶段复制给 Claude Code 执行。每完成一个阶段，测试确认无误后再进行下一阶段。

---

## Phase 1: 基础数据结构

### 指令 1.1 - 创建枚举和基础数据类

```
请在 Unity 项目中创建以下文件结构和代码：

目录：Assets/Scripts/Core/Data/

1. 创建 Enums.cs，包含以下枚举：
   - CardType: Minion, Spell, Amulet
   - Rarity: Bronze, Silver, Gold, Legendary
   - HeroClass: Neutral=0, ClassA=1, ClassB=2, ClassC=3
   - GamePhase: NotStarted, Mulligan, TurnStart, Draw, Main, TurnEnd, GameOver
   - Keyword: Ward, Rush, Storm
   - EffectTrigger: OnPlay, OnDestroy, OnAttack, OnDamaged, OnTurnStart, OnTurnEnd, OnEvolve, OnActivate, OnAllyPlay, OnEnemyPlay, OnAllyDestroy, OnEnemyDestroy
   - EffectType: Damage, Heal, Draw, Discard, Summon, Buff, Debuff, Destroy, Vanish, Silence, GainKeyword, AddToHand, Transform, Evolve, GainCost, TileEffect
   - TargetType: Self, SingleEnemy, SingleAlly, AllEnemies, AllAllies, AllMinions, RandomEnemy, RandomAlly, PlayerChoice, EnemyPlayer, AllyPlayer, AdjacentTiles
   - ActionType: PlayCard, Attack, Evolve, ActivateAmulet, EndTurn, Surrender
   - NetworkMessageType: Connect, Disconnect, DeckSubmit, DeckAccepted, DeckRejected, Ready, GameStart, PlayerAction, ActionResult, StateSync, GameEvent, Ping, Pong, Surrender

2. 创建 CardData.cs（使用[System.Serializable]）：
   - 基础属性：cardId(int), cardName(string), description(string), cardType(CardType), rarity(Rarity), cost(int)
   - 归属：heroClass(HeroClass), tags(List<string>)
   - 随从属性：attack(int), health(int)
   - 护符属性：countdown(int，-1表示无), canActivate(bool), activateCost(int)
   - 进化属性：evolvedAttack(int), evolvedHealth(int)
   - 效果：effects(List<EffectData>)
   - 资源：artworkPath(string), evolvedArtworkPath(string)

3. 创建 EffectData.cs（使用[System.Serializable]）：
   - trigger(EffectTrigger)
   - conditionType(string) - 条件类型标识
   - conditionParams(List<string>) - 条件参数
   - targetType(TargetType)
   - effectType(EffectType)
   - value(int)
   - parameters(List<string>)

请确保所有类都有合适的默认构造函数，并添加必要的using语句。
```

### 指令 1.2 - 创建游戏状态数据类

```
继续在 Assets/Scripts/Core/Data/ 目录创建：

1. 创建 GameState.cs：
   - turnNumber(int)
   - currentPlayerId(int)
   - phase(GamePhase)
   - players(PlayerState[]) - 数组长度2，[0]=先手，[1]=后手
   - randomSeed(int)

2. 创建 PlayerState.cs：
   - playerId(int)
   - heroClass(HeroClass)
   - health(int), maxHealth(int) - 初始都是40
   - mana(int), maxMana(int)
   - evolutionPoints(int) - 后手初始3，先手0
   - hasEvolvedThisTurn(bool)
   - fatigueCounter(int) - 疲劳计数器
   - deck(List<int>) - 牌库中的cardId
   - hand(List<RuntimeCard>) - 手牌
   - field(TileState[]) - 6个格子
   - graveyard(List<int>) - 墓地
   - compensationCardId(int) - 后手补偿卡ID，-1表示无/先手

3. 创建 TileState.cs：
   - tileIndex(int) - 0到5
   - occupant(RuntimeCard) - 可为null
   - effects(List<TileEffect>)

4. 创建 TileEffect.cs：
   - effectId(int)
   - sourceCardId(int)
   - effectType(EffectType)
   - value(int)
   - remainingTurns(int) - -1表示永久
   - triggerTiming(EffectTrigger) - 触发时机

5. 创建 RuntimeCard.cs - 运行时卡牌实例：
   - instanceId(int) - 运行时唯一ID
   - cardId(int) - 对应CardData
   - ownerId(int) - 所属玩家
   - currentAttack(int), currentHealth(int), maxHealth(int)
   - isEvolved(bool)
   - canAttack(bool)
   - attackedThisTurn(bool)
   - isSilenced(bool)
   - currentCountdown(int) - 护符倒计时
   - hasWard(bool), hasRush(bool), hasStorm(bool)
   - buffs(List<BuffData>)

6. 创建 BuffData.cs：
   - buffId(int)
   - sourceCardId(int)
   - attackModifier(int)
   - healthModifier(int)
   - duration(int) - -1表示永久
   - grantedKeywords(List<Keyword>)

所有类使用[System.Serializable]标记。
```

### 指令 1.3 - 创建卡组数据类

```
继续在 Assets/Scripts/Core/Data/ 目录创建：

1. 创建 DeckData.cs：
   - deckId(string) - 使用GUID
   - deckName(string)
   - heroClass(HeroClass)
   - cards(List<DeckEntry>) - 卡组内容
   - compensationCardId(int) - 后手补偿卡选择
   - lastModified(long) - Unix时间戳

2. 创建 DeckEntry.cs：
   - cardId(int)
   - count(int) - 1到3

3. 创建 PlayerCollection.cs：
   - playerId(string)
   - ownedCards(Dictionary<int, int>) - cardId -> 拥有数量
   - decks(List<DeckData>)
   - selectedDeckIndex(int)

4. 创建 DeckRulesConfig.cs 作为ScriptableObject：
   - 放在 Assets/Scripts/Core/Data/Configs/ 目录
   - 使用 [CreateAssetMenu(fileName = "DeckRules", menuName = "CardGame/DeckRules")]
   - minDeckSize(int) = 40
   - maxDeckSize(int) = 40
   - maxCopiesPerCard(int) = 3
   - maxCopiesLegendary(int) = 3 (此游戏传说也是3张上限)
   - enforceClassRestriction(bool) = true
   - allowNeutralCards(bool) = true

使用[System.Serializable]标记普通类，ScriptableObject单独处理。
```

---

## Phase 2: 效果系统

### 指令 2.1 - 创建效果系统框架

```
在 Assets/Scripts/Core/Effects/ 目录创建效果系统：

1. 创建接口 IEffectExecutor.cs：
   - void Execute(EffectContext context)
   
2. 创建 EffectContext.cs - 效果执行上下文：
   - GameState gameState
   - RuntimeCard source - 效果来源卡牌
   - List<RuntimeCard> targets - 目标列表
   - int value - 效果数值
   - List<string> parameters - 额外参数
   - List<GameEvent> resultEvents - 执行产生的事件

3. 创建接口 ITargetSelector.cs：
   - List<RuntimeCard> SelectTargets(GameState state, RuntimeCard source, TargetType targetType)
   - bool RequiresPlayerChoice(TargetType targetType) - 是否需要玩家选择

4. 创建接口 IConditionChecker.cs：
   - bool CheckCondition(GameState state, RuntimeCard source, string conditionType, List<string> conditionParams)

5. 创建 EffectSystem.cs - 效果系统主类：
   - Dictionary<EffectType, IEffectExecutor> executors
   - ITargetSelector targetSelector
   - IConditionChecker conditionChecker
   - 方法：RegisterExecutor(EffectType type, IEffectExecutor executor)
   - 方法：ProcessEffect(GameState state, RuntimeCard source, EffectData effect) -> List<GameEvent>
   - 方法：TriggerEffects(GameState state, EffectTrigger trigger, RuntimeCard triggerSource) -> List<GameEvent>

效果系统应该是纯逻辑，不依赖Unity组件，方便单元测试。
```

### 指令 2.2 - 实现基础效果执行器

```
在 Assets/Scripts/Core/Effects/Executors/ 目录创建：

1. DamageExecutor.cs - 造成伤害：
   - 对targets中的每个目标造成value点伤害
   - 生成 DamageEvent
   - 检查目标是否死亡，生成 UnitDestroyedEvent

2. HealExecutor.cs - 治疗：
   - 恢复生命值，不超过maxHealth
   - 生成 HealEvent

3. DrawExecutor.cs - 抽牌：
   - 从牌库抽value张牌
   - 处理手牌上限（满了进墓地）
   - 处理牌库空（疲劳伤害）
   - 生成 DrawEvent, FatigueEvent

4. SummonExecutor.cs - 召唤：
   - parameters[0]存储要召唤的cardId
   - 在指定格子召唤随从
   - 生成 SummonEvent

5. BuffExecutor.cs - 增益：
   - parameters格式："攻击力,生命值" 如 "+2,+2"
   - 添加BuffData到目标
   - 生成 BuffEvent

6. DestroyExecutor.cs - 破坏：
   - 将目标移到墓地
   - 触发谢幕效果
   - 生成 UnitDestroyedEvent

7. VanishExecutor.cs - 消失：
   - 将目标从游戏中移除
   - 不触发谢幕
   - 不进入墓地
   - 生成 UnitVanishedEvent

8. SilenceExecutor.cs - 沉默：
   - 设置isSilenced = true
   - 移除特殊关键词（ward, rush, storm）
   - 保留buff数值
   - 生成 SilenceEvent

每个执行器实现 IEffectExecutor 接口。
```

### 指令 2.3 - 创建游戏事件类

```
在 Assets/Scripts/Core/Events/ 目录创建游戏事件：

1. 创建基类 GameEvent.cs：
   - eventId(int) - 自增ID
   - timestamp(long)
   - sourcePlayerId(int)

2. 创建以下事件类（都继承GameEvent）：

   CardDrawnEvent:
   - playerId(int)
   - cardId(int)
   - fromFatigue(bool)
   - discardedDueToFull(bool)

   CardPlayedEvent:
   - playerId(int)
   - cardId(int)
   - instanceId(int)
   - tileIndex(int) - 放置位置，法术为-1
   - manaCost(int)

   DamageEvent:
   - sourceInstanceId(int)
   - targetInstanceId(int)
   - amount(int)
   - targetIsPlayer(bool)
   - targetPlayerId(int)

   HealEvent:
   - targetInstanceId(int)
   - amount(int)
   - actualHealed(int) - 实际恢复量
   - targetIsPlayer(bool)

   UnitDestroyedEvent:
   - instanceId(int)
   - cardId(int)
   - tileIndex(int)
   - wasVanished(bool) - true表示消失而非破坏

   SummonEvent:
   - instanceId(int)
   - cardId(int)
   - tileIndex(int)
   - ownerId(int)

   AttackEvent:
   - attackerInstanceId(int)
   - defenderInstanceId(int)
   - defenderIsPlayer(bool)

   EvolveEvent:
   - instanceId(int)
   - wasManual(bool) - 是否手动进化（消耗EP）

   BuffEvent:
   - targetInstanceId(int)
   - attackChange(int)
   - healthChange(int)
   - keywordsGranted(List<Keyword>)

   SilenceEvent:
   - targetInstanceId(int)

   TurnStartEvent:
   - playerId(int)
   - turnNumber(int)
   - newMaxMana(int)

   TurnEndEvent:
   - playerId(int)

   GameStartEvent:
   - firstPlayerId(int)
   - randomSeed(int)

   GameOverEvent:
   - winnerId(int)
   - reason(string)

   FatigueEvent:
   - playerId(int)
   - damage(int)

   AmuletActivatedEvent:
   - instanceId(int)
   - wasDestroyed(bool)

   CountdownTickEvent:
   - instanceId(int)
   - newCountdown(int)
```

---

## Phase 3: 游戏规则引擎

### 指令 3.1 - 创建游戏规则引擎核心

```
在 Assets/Scripts/Core/Rules/ 目录创建：

1. 创建 GameRuleEngine.cs - 核心规则引擎：
   
   属性：
   - GameState currentState
   - EffectSystem effectSystem
   - System.Random rng (使用种子初始化)
   - int instanceIdCounter - 用于生成唯一instanceId

   公开方法：
   - void Initialize(GameState initialState)
   - List<GameEvent> ProcessAction(PlayerAction action) - 处理玩家操作
   - bool ValidateAction(PlayerAction action) - 验证操作合法性
   - List<RuntimeCard> GetValidTargets(int playerId, ActionType actionType, int sourceInstanceId) - 获取有效目标
   - List<int> GetPlayableCards(int playerId) - 获取可使用的手牌
   - bool CanEvolve(int playerId, int instanceId) - 能否进化
   - GameState GetStateCopy() - 获取状态副本

   私有方法：
   - List<GameEvent> ProcessPlayCard(int playerId, int handIndex, int tileIndex, int targetId)
   - List<GameEvent> ProcessAttack(int attackerInstanceId, int targetInstanceId)
   - List<GameEvent> ProcessEvolve(int playerId, int instanceId)
   - List<GameEvent> ProcessActivateAmulet(int instanceId)
   - List<GameEvent> ProcessEndTurn(int playerId)
   - void ApplyEvents(List<GameEvent> events) - 将事件应用到状态
   - int GenerateInstanceId()

2. 创建 PlayerAction.cs：
   - playerId(int)
   - actionType(ActionType)
   - handIndex(int) - PlayCard时使用
   - tileIndex(int) - 放置位置
   - sourceInstanceId(int) - Attack/Evolve/Activate时使用
   - targetInstanceId(int) - 目标
   - targetIsPlayer(bool)
```

### 指令 3.2 - 实现回合流程管理

```
在 Assets/Scripts/Core/Rules/ 目录创建：

1. 创建 TurnManager.cs：

   方法：
   - List<GameEvent> StartTurn(GameState state, int playerId)
     * 设置 currentPlayerId
     * 增加 maxMana（不超过10）
     * 恢复 mana = maxMana
     * 重置 hasEvolvedThisTurn = false
     * 重置所有随从的 canAttack = true, attackedThisTurn = false
     * 处理护符倒计时（-1，归零则破坏）
     * 处理格子效果
     * 生成 TurnStartEvent
     * 触发 OnTurnStart 效果

   - List<GameEvent> DrawPhase(GameState state, int playerId)
     * 抽1张牌
     * 处理牌库空（疲劳）
     * 处理手牌满（进墓地）
     * 返回 DrawEvent 或 FatigueEvent

   - List<GameEvent> EndTurn(GameState state, int playerId)
     * 触发 OnTurnEnd 效果
     * 生成 TurnEndEvent
     * 切换到对方玩家

   - int GetNextPlayerId(int currentPlayerId) - 返回对手ID

2. 创建 CombatResolver.cs - 战斗结算：

   方法：
   - List<GameEvent> ResolveAttack(GameState state, RuntimeCard attacker, RuntimeCard defender)
     * 双方互相造成伤害（攻击力）
     * 生成 AttackEvent, DamageEvent
     * 检查死亡，生成 UnitDestroyedEvent
     * 设置 attacker.attackedThisTurn = true, canAttack = false

   - List<GameEvent> ResolveAttackPlayer(GameState state, RuntimeCard attacker, int targetPlayerId)
     * 对玩家造成伤害
     * 检查游戏结束
     * 设置攻击状态

   - bool CanAttackTarget(GameState state, RuntimeCard attacker, int targetInstanceId, bool targetIsPlayer)
     * 检查是否有守护阻挡
     * 检查attacker是否能攻击（canAttack, 召唤失调等）

   - List<RuntimeCard> GetValidAttackTargets(GameState state, RuntimeCard attacker)
     * 如果有守护，只返回守护随从
     * 否则返回所有敌方单位 + 敌方玩家标记

3. 创建 EvolutionSystem.cs - 进化系统：

   方法：
   - bool CanUseEvolution(GameState state, int playerId)
     * 后手第4回合后才能用
     * 检查EP > 0
     * 检查本回合未手动进化

   - bool CanEvolveMinion(RuntimeCard minion)
     * 必须是随从
     * 未被进化过

   - List<GameEvent> Evolve(GameState state, int playerId, RuntimeCard minion, bool consumeEP)
     * 应用 +2/+2
     * 设置 isEvolved = true
     * 给予突进（如果本回合入场）
     * 如果 consumeEP: EP-1, hasEvolvedThisTurn = true
     * 触发 OnEvolve 效果
     * 生成 EvolveEvent
```

### 指令 3.3 - 实现卡组验证器

```
在 Assets/Scripts/Core/Rules/ 目录创建：

1. 创建 DeckValidator.cs：

   依赖：
   - DeckRulesConfig rulesConfig
   - Dictionary<int, CardData> cardDatabase - 所有卡牌数据

   方法：
   - ValidationResult ValidateDeck(DeckData deck)
   
   - ValidationResult 类包含：
     * bool isValid
     * List<string> errors

   验证规则：
   - 卡组大小必须等于 rulesConfig.minDeckSize (40)
   - 每张卡数量不超过 rulesConfig.maxCopiesPerCard (3)
   - 所有卡牌ID必须存在于cardDatabase
   - 职业卡必须与卡组职业匹配
   - 中立卡(HeroClass.Neutral)可在任何卡组使用
   - 补偿卡ID必须是有效的补偿卡

2. 创建 CompensationCardDatabase.cs - 后手补偿卡数据：
   
   使用静态数据或ScriptableObject存储：
   - cardId: 10001, name: "临时水晶", effect: 本回合+1费用
   - cardId: 10002, name: "微型打击", effect: 对一个随从造成2点伤害
   - cardId: 10003, name: "小鬼召唤", effect: 召唤一个1/1的小鬼
   - cardId: 10004, name: "贪婪抽取", effect: 弃1抽2
   - cardId: 10005, name: "紧急治疗", effect: 恢复自己3点生命

   方法：
   - List<CardData> GetAllCompensationCards()
   - CardData GetCompensationCard(int cardId)
   - bool IsCompensationCard(int cardId)
```

---

## Phase 4: 存储服务与管理器

### 指令 4.1 - 创建存储服务

```
在 Assets/Scripts/Managers/ 目录创建：

1. 创建接口 IStorageService.cs：
   - void SavePlayerCollection(PlayerCollection collection)
   - PlayerCollection LoadPlayerCollection(string playerId)
   - void SaveDeck(DeckData deck)
   - void DeleteDeck(string deckId)
   - List<DeckData> LoadAllDecks(string playerId)

2. 创建 LocalStorageService.cs 实现 IStorageService：
   
   存储位置：Application.persistentDataPath + "/SaveData/"
   
   文件结构：
   - /SaveData/collection_{playerId}.json
   - /SaveData/decks/{playerId}/{deckId}.json

   实现细节：
   - 使用 JsonUtility 进行序列化
   - 创建目录如果不存在
   - 加载时处理文件不存在的情况
   - 考虑异常处理和日志

3. 创建 DeckManager.cs：
   
   依赖：
   - IStorageService storageService
   - DeckValidator validator
   - CompensationCardDatabase compensationCards

   属性：
   - List<DeckData> decks
   - DeckData currentDeck

   方法：
   - void Initialize(string playerId)
   - List<DeckData> GetAllDecks()
   - DeckData GetDeck(string deckId)
   - ValidationResult CreateDeck(string name, HeroClass heroClass)
   - ValidationResult SaveDeck(DeckData deck)
   - void DeleteDeck(string deckId)
   - ValidationResult AddCardToDeck(string deckId, int cardId)
   - ValidationResult RemoveCardFromDeck(string deckId, int cardId)
   - void SetCompensationCard(string deckId, int compensationCardId)
   - void SelectDeck(string deckId)
   - DeckData GetSelectedDeck()
```

---

## Phase 5: 网络层基础

### 指令 5.1 - 创建网络消息结构

```
在 Assets/Scripts/Network/Messages/ 目录创建：

1. 创建 NetworkMessage.cs - 基础消息类：
   - messageType(NetworkMessageType)
   - sequence(int) - 序列号
   - timestamp(long)
   - payload(string) - JSON序列化的数据

2. 创建各种消息载荷类（都使用[System.Serializable]）：

   ConnectPayload:
   - playerName(string)
   - version(string) - 游戏版本

   DeckSubmitPayload:
   - deck(DeckData)

   GameStartPayload:
   - randomSeed(int)
   - firstPlayerId(int)
   - player0State(PlayerState)
   - player1State(PlayerState)

   PlayerActionPayload:
   - action(PlayerAction)

   ActionResultPayload:
   - success(bool)
   - errorMessage(string)
   - events(List<GameEvent>) - 注意：需要支持多态序列化

   StateSyncPayload:
   - gameState(GameState)
   - stateHash(string) - 用于校验

3. 创建 MessageSerializer.cs：
   - string Serialize<T>(T obj)
   - T Deserialize<T>(string json)
   - NetworkMessage CreateMessage(NetworkMessageType type, object payload)
   - 处理GameEvent的多态序列化（可能需要自定义处理）
```

### 指令 5.2 - 创建网络管理器框架

```
在 Assets/Scripts/Network/ 目录创建：

1. 创建接口 INetworkService.cs：
   - event Action<NetworkMessage> OnMessageReceived
   - event Action OnConnected
   - event Action<string> OnDisconnected
   - event Action<string> OnError
   - bool IsHost { get; }
   - bool IsConnected { get; }
   - void StartHost(int port)
   - void Connect(string ip, int port)
   - void Send(NetworkMessage message)
   - void Disconnect()

2. 创建 NetworkManager.cs（继承MonoBehaviour，单例模式）：
   
   属性：
   - INetworkService networkService
   - bool isHost
   - bool isConnected
   - int localPlayerId
   - Queue<NetworkMessage> messageQueue - 接收到的消息队列
   
   公开方法：
   - void HostGame(int port = 7777)
   - void JoinGame(string ip, int port = 7777)
   - void SubmitDeck(DeckData deck)
   - void SendAction(PlayerAction action)
   - void SendReady()
   - void Disconnect()
   
   事件：
   - Action<int> OnPlayerConnected
   - Action<int> OnPlayerDisconnected
   - Action<GameStartPayload> OnGameStart
   - Action<ActionResultPayload> OnActionResult
   - Action<StateSyncPayload> OnStateSync
   - Action<string> OnError

3. 创建 LocalNetworkService.cs - 本地测试用实现：
   - 模拟网络延迟
   - 用于单机双端测试
   - 实现 INetworkService 接口

注意：实际的TCP网络实现在后续阶段完成，先用LocalNetworkService测试逻辑。
```

---

## Phase 6: 游戏控制器

### 指令 6.1 - 创建游戏主控制器

```
在 Assets/Scripts/Managers/ 目录创建：

1. 创建 GameController.cs（继承MonoBehaviour）：

   依赖：
   - GameRuleEngine ruleEngine
   - NetworkManager networkManager
   - EffectSystem effectSystem
   - TurnManager turnManager

   状态：
   - GameState currentState
   - int localPlayerId
   - bool isMyTurn
   - bool waitingForResponse - 等待服务器响应

   初始化方法：
   - void InitializeGame(GameStartPayload payload)
     * 创建 GameState
     * 初始化 ruleEngine
     * 设置 localPlayerId
     * 注册网络事件监听

   玩家操作方法（供UI调用）：
   - bool TryPlayCard(int handIndex, int tileIndex, int targetId = -1)
   - bool TryAttack(int attackerInstanceId, int targetInstanceId, bool targetIsPlayer)
   - bool TryEvolve(int instanceId)
   - bool TryActivateAmulet(int instanceId)
   - bool TryEndTurn()
   
   查询方法：
   - List<int> GetPlayableCardIndices()
   - List<int> GetValidTargetsForCard(int handIndex)
   - List<int> GetValidAttackTargets(int attackerInstanceId)
   - bool CanEvolve(int instanceId)
   - bool CanActivateAmulet(int instanceId)
   - PlayerState GetLocalPlayerState()
   - PlayerState GetOpponentPlayerState()
   - TileState[] GetLocalField()
   - TileState[] GetOpponentField()

   网络响应处理：
   - void OnActionResultReceived(ActionResultPayload result)
   - void OnStateSyncReceived(StateSyncPayload sync)

   事件（供View订阅）：
   - Action<GameEvent> OnGameEvent - 每个事件触发，用于播放动画
   - Action OnStateChanged - 状态变化，用于刷新UI
   - Action<int> OnTurnChanged - 回合切换
   - Action<int, string> OnGameOver - 游戏结束
```

---

## 完成 Phase 1-6 后的测试指令

```
请创建一个简单的测试场景来验证核心逻辑：

1. 创建 Assets/Scripts/Tests/CoreLogicTest.cs（继承MonoBehaviour）：
   
   在Start()中：
   - 创建两个测试CardData（一个随从、一个法术）
   - 创建两个PlayerState
   - 创建GameState
   - 初始化GameRuleEngine
   - 模拟一个完整回合：
     * StartTurn
     * DrawPhase
     * PlayCard（放置随从）
     * EndTurn
   - 打印所有生成的GameEvent
   - 打印最终GameState

2. 创建测试场景 Assets/Scenes/Test.unity：
   - 添加一个空物体挂载 CoreLogicTest
   - 运行并在Console查看输出

这将验证数据结构和规则引擎是否正常工作。
```

---

## 后续阶段预告

**Phase 7**: 实际TCP网络实现  
**Phase 8**: UI框架搭建  
**Phase 9**: 卡牌视图与动画  
**Phase 10**: 完整游戏流程整合  
**Phase 11**: 卡牌内容制作  
**Phase 12**: 测试与平衡  

---

*指令文档版本: 1.0*
