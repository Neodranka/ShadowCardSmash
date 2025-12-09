# Phase 8-Logic: 游戏逻辑完善

## 目标

让热座模式能正常游玩，所有卡牌效果正确生效。

---

## 当前状态检查

在开始之前，先检查现有系统的完成度。请运行热座模式并报告以下功能是否正常：

```
检查清单：
[ ] 游戏能正常启动，显示双方手牌
[ ] 回合能正常切换
[ ] 能使用随从卡放到格子上
[ ] 随从能进行攻击
[ ] 随从死亡会被移除
[ ] 费用系统正常（每回合+1，使用卡牌扣费）
[ ] 抽牌系统正常
[ ] 玩家生命值变化正常
[ ] 游戏结束判定正常
```

根据检查结果，可能需要修复的部分会有所不同。

---

## 指令 8.1 - 完善效果执行器

```
请检查并完善 Assets/Scripts/Core/Effects/Executors/ 目录下的效果执行器：

需要确保以下执行器都已实现且功能正确：

1. DamageExecutor.cs - 造成伤害
   - 对随从造成伤害：减少 currentHealth
   - 对玩家造成伤害：减少 player.health
   - 检查随从死亡（currentHealth <= 0）
   - 死亡时触发谢幕效果
   - 生成 DamageEvent 和 UnitDestroyedEvent（如果死亡）

2. HealExecutor.cs - 治疗
   - 对随从治疗：增加 currentHealth，不超过 maxHealth
   - 对玩家治疗：增加 player.health，不超过 player.maxHealth
   - 生成 HealEvent

3. DrawExecutor.cs - 抽牌
   - 从牌库顶抽取 value 张牌
   - 处理手牌上限（10张）：超出的牌直接进墓地
   - 处理牌库空：造成疲劳伤害（递增：1, 2, 3...）
   - 生成 CardDrawnEvent 或 FatigueEvent

4. SummonExecutor.cs - 召唤
   - parameters[0] 是要召唤的 cardId
   - 找到空格子放置
   - 创建 RuntimeCard 实例
   - 新召唤的随从有召唤失调（canAttack = false）
   - 生成 SummonEvent

5. BuffExecutor.cs - 增益/减益
   - parameters 格式: "attack,health" 如 "2,2" 或 "-1,0"
   - 修改 currentAttack 和 currentHealth/maxHealth
   - 如果是正面buff，增加 maxHealth 并同时增加 currentHealth
   - 如果是减益，只减少当前值
   - 生成 BuffEvent

6. DestroyExecutor.cs - 破坏
   - 将目标从战场移除
   - 将 cardId 加入墓地
   - 触发目标的谢幕效果（如果有且未被沉默）
   - 生成 UnitDestroyedEvent

7. VanishExecutor.cs - 消失
   - 将目标从战场移除
   - 不进入墓地
   - 不触发谢幕效果
   - 生成 UnitDestroyedEvent（设置 wasVanished = true）

8. SilenceExecutor.cs - 沉默
   - 设置 target.isSilenced = true
   - 移除关键词：hasWard = false, hasRush = false, hasStorm = false
   - 保留数值buff（currentAttack, currentHealth 不变）
   - 生成 SilenceEvent

9. GainKeywordExecutor.cs - 获得关键词（如果没有则创建）
   - parameters[0] 是关键词名称: "Ward", "Rush", "Storm"
   - 设置对应的 bool 属性为 true
   - 生成 KeywordGainedEvent（如果没有这个事件类，创建一个）

10. GainManaExecutor.cs - 获得费用（如果没有则创建）
    - 增加当前回合的可用费用
    - value 是增加的费用数量
    - 不超过 maxMana
    - 生成 ManaGainedEvent

确保每个执行器都：
- 正确实现 IEffectExecutor 接口
- 在 EffectSystemFactory 中注册
- 处理边界情况（空目标、目标已死亡等）
```

---

## 指令 8.2 - 完善关键词系统

```
请确保关键词系统正确实现：

1. 守护 (Ward)
   位置: CombatResolver.cs 的 GetValidAttackTargets 方法

   逻辑：
   - 检查对方场上是否有 hasWard = true 的随从
   - 如果有，只返回带守护的随从作为有效目标
   - 如果没有，返回所有敌方随从 + 敌方玩家

2. 突进 (Rush)
   位置: GameRuleEngine.cs 的 ProcessPlayCard 方法

   逻辑：
   - 随从入场时，如果 hasRush = true
   - 设置 canAttack = true
   - 但只能攻击随从，不能攻击玩家
   
   位置: CombatResolver.cs 的 CanAttackTarget 方法
   - 如果攻击者本回合入场且有 Rush（没有 Storm）
   - 不能攻击玩家

3. 疾驰 (Storm)
   位置: GameRuleEngine.cs 的 ProcessPlayCard 方法

   逻辑：
   - 随从入场时，如果 hasStorm = true
   - 设置 canAttack = true
   - 可以攻击任意目标（包括玩家）

4. 召唤失调处理
   位置: GameRuleEngine.cs

   逻辑：
   - 默认情况：随从入场设置 canAttack = false
   - 有 Rush：canAttack = true，但 CombatResolver 限制不能打玩家
   - 有 Storm：canAttack = true，无限制
   - 回合开始时：重置所有随从 canAttack = true

请创建或更新相关代码，确保这些关键词正确生效。
```

---

## 指令 8.3 - 完善护符系统

```
请确保护符系统正确实现：

1. 护符放置
   位置: GameRuleEngine.cs 的 ProcessPlayCard 方法

   逻辑：
   - 护符放置到格子上，占据一个位置
   - 护符有 countdown 属性（-1 表示无倒计时）
   - 护符入场触发开幕效果

2. 倒计时处理
   位置: TurnManager.cs 的 StartTurn 方法

   逻辑：
   - 回合开始时，遍历当前玩家的所有护符
   - 如果护符有倒计时（countdown > 0）：
     * countdown -= 1
     * 生成 CountdownTickEvent
     * 如果 countdown == 0：
       - 破坏护符（触发谢幕）
       - 生成 UnitDestroyedEvent

3. 护符启动
   位置: GameRuleEngine.cs 的 ProcessActivateAmulet 方法

   逻辑：
   - 检查护符是否可启动（canActivate = true）
   - 检查是否被沉默（isSilenced = true 则不能启动）
   - 扣除启动费用（activateCost）
   - 执行 OnActivate 效果
   - 根据效果决定是否破坏护符

4. 护符不可攻击
   位置: CombatResolver.cs

   逻辑：
   - GetValidAttackTargets 不应该返回护符
   - 护符的 CardType == CardType.Amulet

请更新相关代码。
```

---

## 指令 8.4 - 完善进化系统

```
请确保进化系统正确实现：

位置: Assets/Scripts/Core/Rules/EvolutionSystem.cs

1. 进化条件检查 CanEvolve(GameState state, int playerId, RuntimeCard minion)
   - 必须是后手玩家（playerId == 1）或回合数 >= 4
   - 玩家还有进化点（evolutionPoints > 0）
   - 本回合还没有手动进化（hasEvolvedThisTurn == false）
   - 目标是随从（cardType == Minion）
   - 目标未进化过（isEvolved == false）
   - 目标不是护符

2. 执行进化 Evolve(GameState state, int playerId, RuntimeCard minion, bool consumeEP)
   - 增加属性：currentAttack += 2, currentHealth += 2, maxHealth += 2
   - 设置 isEvolved = true
   - 给予突进（如果本回合入场，设置 canAttack = true）
   - 如果 consumeEP == true：
     * evolutionPoints -= 1
     * hasEvolvedThisTurn = true
   - 触发 OnEvolve 效果
   - 生成 EvolveEvent

3. 后手进化点初始化
   位置: GameRuleEngine.cs 的 StartGame 或 Initialize

   - 后手玩家（playerId == 1）初始 evolutionPoints = 3
   - 先手玩家 evolutionPoints = 0（或者也可以给，但延后开放）

4. 进化点可用时机
   - 后手从第4回合开始可以使用进化点
   - 检查: turnNumber >= 4 或者使用专门的标记

5. UI 联动
   位置: BattleUIController.cs

   - 进化按钮应该在条件满足时可用
   - 点击进化按钮后，高亮可进化的随从
   - 点击随从执行进化
```

---

## 指令 8.5 - 完善开幕/谢幕效果触发

```
请确保开幕和谢幕效果正确触发：

1. 开幕效果 (OnPlay)
   位置: GameRuleEngine.cs 的 ProcessPlayCard 方法

   在放置随从/护符后：
   ```csharp
   // 触发开幕效果
   var cardData = _cardDatabase.GetCardById(card.cardId);
   if (cardData != null && cardData.effects != null)
   {
       foreach (var effect in cardData.effects)
       {
           if (effect.trigger == EffectTrigger.OnPlay)
           {
               var effectEvents = _effectSystem.ProcessEffect(
                   _currentState, card, playerId, effect, chosenTargets);
               events.AddRange(effectEvents);
           }
       }
   }
   ```

2. 谢幕效果 (OnDestroy)
   位置: 任何导致单位死亡/破坏的地方

   在单位被破坏时（不是消失）：
   ```csharp
   // 检查是否被沉默
   if (!card.isSilenced)
   {
       var cardData = _cardDatabase.GetCardById(card.cardId);
       if (cardData != null && cardData.effects != null)
       {
           foreach (var effect in cardData.effects)
           {
               if (effect.trigger == EffectTrigger.OnDestroy)
               {
                   var effectEvents = _effectSystem.ProcessEffect(
                       _currentState, card, card.ownerId, effect, null);
                   events.AddRange(effectEvents);
               }
           }
       }
   }
   ```

3. 确保谢幕在以下情况触发：
   - 战斗死亡
   - 被效果破坏（DestroyExecutor）
   - 护符倒计时归零
   - 伤害致死（DamageExecutor）

4. 确保谢幕在以下情况不触发：
   - 被沉默的单位
   - 被消失（VanishExecutor）
```

---

## 指令 8.6 - 完善目标选择系统

```
请确保目标选择器正确实现：

位置: Assets/Scripts/Core/Effects/TargetSelector.cs

1. 实现所有目标类型的选择逻辑：

```csharp
public List<RuntimeCard> SelectTargets(GameState state, RuntimeCard source, 
    int sourcePlayerId, TargetType targetType)
{
    var targets = new List<RuntimeCard>();
    int opponentId = 1 - sourcePlayerId;
    
    switch (targetType)
    {
        case TargetType.Self:
            if (source != null) targets.Add(source);
            break;
            
        case TargetType.AllEnemies:
            // 所有敌方随从
            foreach (var tile in state.players[opponentId].field)
            {
                if (tile.occupant != null && 
                    tile.occupant.GetCardType() == CardType.Minion)
                {
                    targets.Add(tile.occupant);
                }
            }
            break;
            
        case TargetType.AllAllies:
            // 所有友方随从
            foreach (var tile in state.players[sourcePlayerId].field)
            {
                if (tile.occupant != null && 
                    tile.occupant.GetCardType() == CardType.Minion)
                {
                    targets.Add(tile.occupant);
                }
            }
            break;
            
        case TargetType.AllMinions:
            // 所有随从（双方）
            for (int p = 0; p < 2; p++)
            {
                foreach (var tile in state.players[p].field)
                {
                    if (tile.occupant != null && 
                        tile.occupant.GetCardType() == CardType.Minion)
                    {
                        targets.Add(tile.occupant);
                    }
                }
            }
            break;
            
        case TargetType.RandomEnemy:
            var enemies = SelectTargets(state, source, sourcePlayerId, TargetType.AllEnemies);
            if (enemies.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, enemies.Count);
                targets.Add(enemies[index]);
            }
            break;
            
        case TargetType.RandomAlly:
            var allies = SelectTargets(state, source, sourcePlayerId, TargetType.AllAllies);
            if (allies.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, allies.Count);
                targets.Add(allies[index]);
            }
            break;
            
        case TargetType.AdjacentTiles:
            // 相邻格子的单位
            if (source != null)
            {
                int tileIndex = GetTileIndex(state, source);
                if (tileIndex >= 0)
                {
                    // 左边
                    if (tileIndex > 0)
                    {
                        var leftOccupant = state.players[source.ownerId].field[tileIndex - 1].occupant;
                        if (leftOccupant != null) targets.Add(leftOccupant);
                    }
                    // 右边
                    if (tileIndex < 5)
                    {
                        var rightOccupant = state.players[source.ownerId].field[tileIndex + 1].occupant;
                        if (rightOccupant != null) targets.Add(rightOccupant);
                    }
                }
            }
            break;
            
        case TargetType.SingleEnemy:
        case TargetType.SingleAlly:
        case TargetType.PlayerChoice:
            // 这些需要玩家选择，返回空列表，由 UI 处理
            break;
            
        case TargetType.EnemyPlayer:
        case TargetType.AllyPlayer:
            // 玩家目标在 EffectSystem 中单独处理
            break;
    }
    
    return targets;
}

public bool RequiresPlayerChoice(TargetType targetType)
{
    return targetType == TargetType.SingleEnemy ||
           targetType == TargetType.SingleAlly ||
           targetType == TargetType.PlayerChoice;
}
```

2. 在 UI 中处理需要选择目标的效果：
   - BattleUIController 检测到需要选择目标时
   - 进入 SelectingTarget 状态
   - 高亮有效目标
   - 玩家点击后，将选择的目标传递给 GameRuleEngine
```

---

## 指令 8.7 - 连接 UI 与逻辑层

```
请确保 BattleUIController 正确连接 HotSeatGameManager 和 GameRuleEngine：

1. HotSeatGameManager.cs 应该：
   - 创建并持有 GameRuleEngine 实例
   - 创建并持有 GameState 实例
   - 初始化测试卡组和玩家状态
   - 响应 UI 的操作请求

2. BattleUIController.cs 应该：
   - 获取 HotSeatGameManager 的引用
   - 操作时调用 gameManager 的方法
   - 刷新显示时读取 gameManager 的状态

3. 检查以下交互流程：

   出牌流程：
   a. 玩家点击手牌 -> OnHandCardClicked
   b. UI 高亮可放置的格子
   c. 玩家点击格子 -> OnTileClicked
   d. 如果卡牌需要目标，进入目标选择
   e. 调用 gameManager.PlayCard(handIndex, tileIndex, targetId)
   f. gameManager 调用 ruleEngine.ProcessAction
   g. 返回 GameEvents 列表
   h. UI 播放动画并刷新显示

   攻击流程：
   a. 玩家点击我方随从 -> 选中攻击者
   b. UI 高亮可攻击的目标
   c. 玩家点击目标
   d. 调用 gameManager.Attack(attackerId, targetId)
   e. gameManager 调用 ruleEngine.ProcessAction
   f. 返回 GameEvents
   g. UI 播放攻击动画

   结束回合：
   a. 玩家点击结束回合按钮
   b. 调用 gameManager.EndTurn()
   c. 处理回合结束和下一回合开始
   d. 显示玩家切换提示（热座模式）
   e. 切换视角，刷新 UI

4. 确保 RefreshAllUI 方法正确刷新所有组件：
   - 双方生命值
   - 双方费用
   - 双方手牌数
   - 场上所有单位
   - 牌库/墓地数量
   - 进化点显示
   - 回合指示器
```

---

## 指令 8.8 - 创建完整测试流程

```
请创建一个测试脚本验证所有逻辑：

创建 Assets/Scripts/Tests/GameLogicIntegrationTest.cs：

public class GameLogicIntegrationTest : MonoBehaviour
{
    [ContextMenu("Run Full Game Test")]
    public void RunFullGameTest()
    {
        Debug.Log("=== 游戏逻辑集成测试 ===");
        
        // 1. 初始化
        var cardDb = new TestCardDatabase();
        var engine = new GameRuleEngine(cardDb, 12345);
        var state = CreateTestGameState();
        engine.Initialize(state);
        
        // 2. 开始游戏
        var startEvents = engine.StartGame();
        LogEvents("游戏开始", startEvents);
        
        // 3. 测试出牌
        TestPlayCard(engine);
        
        // 4. 测试攻击
        TestAttack(engine);
        
        // 5. 测试进化
        TestEvolution(engine);
        
        // 6. 测试效果
        TestEffects(engine);
        
        // 7. 测试护符
        TestAmulet(engine);
        
        Debug.Log("=== 测试完成 ===");
    }
    
    void TestPlayCard(GameRuleEngine engine)
    {
        Debug.Log("--- 测试出牌 ---");
        
        var action = new PlayerAction
        {
            playerId = engine.CurrentState.currentPlayerId,
            actionType = ActionType.PlayCard,
            handIndex = 0,
            tileIndex = 0,
            targetInstanceId = -1
        };
        
        if (engine.ValidateAction(action))
        {
            var events = engine.ProcessAction(action);
            LogEvents("出牌", events);
            
            // 验证
            var player = engine.CurrentState.players[action.playerId];
            Debug.Log($"  手牌数: {player.hand.Count}");
            Debug.Log($"  格子0占用: {player.field[0].occupant != null}");
        }
        else
        {
            Debug.LogWarning("  出牌验证失败");
        }
    }
    
    void TestAttack(GameRuleEngine engine)
    {
        Debug.Log("--- 测试攻击 ---");
        
        // 先结束回合让对手出牌
        EndTurn(engine);
        
        // 对手出一个随从
        var action1 = new PlayerAction
        {
            playerId = engine.CurrentState.currentPlayerId,
            actionType = ActionType.PlayCard,
            handIndex = 0,
            tileIndex = 0
        };
        engine.ProcessAction(action1);
        
        // 结束回合
        EndTurn(engine);
        
        // 现在我方随从应该可以攻击了
        var myPlayer = engine.CurrentState.players[engine.CurrentState.currentPlayerId];
        var myMinion = myPlayer.field[0].occupant;
        
        if (myMinion != null && myMinion.canAttack)
        {
            var opponentId = 1 - engine.CurrentState.currentPlayerId;
            var enemyMinion = engine.CurrentState.players[opponentId].field[0].occupant;
            
            if (enemyMinion != null)
            {
                var attackAction = new PlayerAction
                {
                    playerId = engine.CurrentState.currentPlayerId,
                    actionType = ActionType.Attack,
                    sourceInstanceId = myMinion.instanceId,
                    targetInstanceId = enemyMinion.instanceId
                };
                
                var events = engine.ProcessAction(attackAction);
                LogEvents("攻击", events);
            }
        }
    }
    
    void TestEvolution(GameRuleEngine engine)
    {
        Debug.Log("--- 测试进化 ---");
        // 切换到后手玩家测试进化
        // ...
    }
    
    void TestEffects(GameRuleEngine engine)
    {
        Debug.Log("--- 测试效果 ---");
        // 测试开幕、谢幕效果
        // ...
    }
    
    void TestAmulet(GameRuleEngine engine)
    {
        Debug.Log("--- 测试护符 ---");
        // 测试护符倒计时、启动
        // ...
    }
    
    void EndTurn(GameRuleEngine engine)
    {
        var action = new PlayerAction
        {
            playerId = engine.CurrentState.currentPlayerId,
            actionType = ActionType.EndTurn
        };
        engine.ProcessAction(action);
    }
    
    void LogEvents(string phase, List<GameEvent> events)
    {
        Debug.Log($"  {phase} 产生 {events.Count} 个事件:");
        foreach (var e in events)
        {
            Debug.Log($"    - {e.GetType().Name}");
        }
    }
    
    GameState CreateTestGameState()
    {
        // 创建测试用游戏状态
        // ...
    }
}
```

然后在 Unity 中：
1. 创建空物体挂载这个脚本
2. 右键脚本 -> Run Full Game Test
3. 查看 Console 输出，检查是否有错误
```

---

## 常见问题排查

如果测试中遇到问题，检查以下几点：

1. **NullReferenceException**
   - 检查 CardDatabase 是否正确初始化
   - 检查 RuntimeCard 的 cardId 是否能找到对应 CardData
   - 检查 field 数组是否正确初始化（6个 TileState）

2. **效果不触发**
   - 检查 CardData.effects 是否正确配置
   - 检查 EffectTrigger 是否匹配
   - 检查执行器是否在 EffectSystemFactory 中注册

3. **关键词不生效**
   - 检查 RuntimeCard 的 hasWard/hasRush/hasStorm 是否正确设置
   - 检查 CombatResolver 的逻辑

4. **进化不可用**
   - 检查 evolutionPoints 初始化
   - 检查回合数条件
   - 检查 hasEvolvedThisTurn 重置

5. **UI 不刷新**
   - 检查事件回调是否正确连接
   - 检查 RefreshAllUI 是否被调用

---

*Phase 8-Logic 指令版本: 1.0*
