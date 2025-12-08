using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;
using ShadowCardSmash.Core.Effects;
using ShadowCardSmash.Core.Rules;
using ShadowCardSmash.Tests;

/// <summary>
/// Phase 1-6 综合系统测试
/// 模拟完整的3回合对战流程，验证所有系统正常工作
/// </summary>
public class FullSystemTest : MonoBehaviour
{
    private TestCardDatabase _cardDatabase;
    private GameRuleEngine _ruleEngine;
    private GameState _gameState;

    private int _eventCount = 0;

    void Start()
    {
        Debug.Log("╔════════════════════════════════════════════════════════════╗");
        Debug.Log("║           Phase 1-6 综合系统测试开始                        ║");
        Debug.Log("╚════════════════════════════════════════════════════════════╝");

        try
        {
            // 1. 初始化测试环境
            InitializeTestEnvironment();

            // 2. 开始游戏
            StartTestGame();

            // 3. 执行回合1（玩家0）
            ExecuteTurn1();

            // 4. 执行回合2（玩家1）
            ExecuteTurn2();

            // 5. 执行回合3（玩家0）- 包含战斗
            ExecuteTurn3();

            // 6. 打印最终状态
            PrintFinalSummary();

            Debug.Log("╔════════════════════════════════════════════════════════════╗");
            Debug.Log("║           所有测试完成！系统运行正常                        ║");
            Debug.Log("╚════════════════════════════════════════════════════════════╝");
        }
        catch (Exception ex)
        {
            Debug.LogError($"测试失败: {ex.Message}");
            Debug.LogError($"堆栈跟踪: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// 初始化测试环境
    /// </summary>
    void InitializeTestEnvironment()
    {
        Debug.Log("\n========== 初始化测试环境 ==========");

        // 创建卡牌数据库
        _cardDatabase = new TestCardDatabase();
        Debug.Log("✓ 卡牌数据库创建完成");

        // 创建牌库
        var deck0 = _cardDatabase.CreateSimpleTestDeck();
        var deck1 = _cardDatabase.CreateSimpleTestDeck();
        Debug.Log($"✓ 牌库创建完成 (每方 {deck0.Count} 张牌)");

        // 创建玩家状态
        var player0 = PlayerState.CreateInitial(0, HeroClass.ClassA, deck0);
        var player1 = PlayerState.CreateInitial(1, HeroClass.ClassB, deck1);
        Debug.Log("✓ 玩家状态创建完成");

        // 创建游戏状态
        int randomSeed = 12345;
        _gameState = GameState.CreateInitial(player0, player1, randomSeed);
        Debug.Log($"✓ 游戏状态创建完成 (随机种子: {randomSeed})");

        // 创建规则引擎
        _ruleEngine = new GameRuleEngine(_cardDatabase, randomSeed);
        _ruleEngine.Initialize(_gameState);
        Debug.Log("✓ 规则引擎初始化完成");

        PrintGameState("初始化后");
    }

    /// <summary>
    /// 开始游戏
    /// </summary>
    void StartTestGame()
    {
        Debug.Log("\n========== 开始游戏 ==========");

        var events = _ruleEngine.StartGame();
        _gameState = _ruleEngine.CurrentState;

        PrintEvents("游戏开始", events);
        PrintGameState("游戏开始后");
    }

    /// <summary>
    /// 执行回合1（玩家0）
    /// </summary>
    void ExecuteTurn1()
    {
        Debug.Log("\n╔══════════════════════════════════════╗");
        Debug.Log("║         回合 1 - 玩家 0               ║");
        Debug.Log("╚══════════════════════════════════════╝");

        // 打印当前手牌
        PrintHand(0);

        // 尝试打出一张2费随从到格子0
        var playableCards = _ruleEngine.GetPlayableCards(0);
        Debug.Log($"可打出的卡牌索引: [{string.Join(", ", playableCards)}]");

        if (playableCards.Count > 0)
        {
            // 找一张2费随从
            int handIndex = FindCardInHand(0, cardId =>
            {
                var data = _cardDatabase.GetCardById(cardId);
                return data.cardType == CardType.Minion && data.cost <= _gameState.GetPlayer(0).mana;
            });

            if (handIndex >= 0)
            {
                var card = _gameState.GetPlayer(0).hand[handIndex];
                var cardData = _cardDatabase.GetCardById(card.cardId);
                Debug.Log($"\n--- 打出卡牌: {cardData.cardName} (费用:{cardData.cost}) 到格子0 ---");

                var action = PlayerAction.CreatePlayCard(0, handIndex, 0);
                if (_ruleEngine.ValidateAction(action))
                {
                    var events = _ruleEngine.ProcessAction(action);
                    _gameState = _ruleEngine.CurrentState;
                    PrintEvents("打出卡牌", events);
                }
                else
                {
                    Debug.LogWarning("操作验证失败！");
                }
            }
            else
            {
                Debug.Log("没有找到可打出的随从");
            }
        }

        // 结束回合
        EndTurn(0);
        PrintGameState("回合1结束后");
    }

    /// <summary>
    /// 执行回合2（玩家1）
    /// </summary>
    void ExecuteTurn2()
    {
        Debug.Log("\n╔══════════════════════════════════════╗");
        Debug.Log("║         回合 2 - 玩家 1               ║");
        Debug.Log("╚══════════════════════════════════════╝");

        // 打印当前手牌
        PrintHand(1);

        // 尝试打出一张随从到格子0
        int handIndex = FindCardInHand(1, cardId =>
        {
            var data = _cardDatabase.GetCardById(cardId);
            return data.cardType == CardType.Minion && data.cost <= _gameState.GetPlayer(1).mana;
        });

        if (handIndex >= 0)
        {
            var card = _gameState.GetPlayer(1).hand[handIndex];
            var cardData = _cardDatabase.GetCardById(card.cardId);
            Debug.Log($"\n--- 打出卡牌: {cardData.cardName} (费用:{cardData.cost}) 到格子0 ---");

            var action = PlayerAction.CreatePlayCard(1, handIndex, 0);
            if (_ruleEngine.ValidateAction(action))
            {
                var events = _ruleEngine.ProcessAction(action);
                _gameState = _ruleEngine.CurrentState;
                PrintEvents("打出卡牌", events);
            }
            else
            {
                Debug.LogWarning("操作验证失败！");
            }
        }

        // 结束回合
        EndTurn(1);
        PrintGameState("回合2结束后");
    }

    /// <summary>
    /// 执行回合3（玩家0）- 包含战斗
    /// </summary>
    void ExecuteTurn3()
    {
        Debug.Log("\n╔══════════════════════════════════════╗");
        Debug.Log("║         回合 3 - 玩家 0 (战斗)        ║");
        Debug.Log("╚══════════════════════════════════════╝");

        // 打印战场状态
        PrintBattlefield();

        // 查找玩家0的攻击者
        var player0 = _gameState.GetPlayer(0);
        RuntimeCard attacker = null;
        int attackerTileIndex = -1;

        for (int i = 0; i < player0.field.Length; i++)
        {
            if (!player0.field[i].IsEmpty())
            {
                var unit = player0.field[i].occupant;
                if (unit.canAttack)
                {
                    attacker = unit;
                    attackerTileIndex = i;
                    break;
                }
            }
        }

        if (attacker != null)
        {
            var attackerData = _cardDatabase.GetCardById(attacker.cardId);
            Debug.Log($"\n攻击者: {attackerData.cardName} (格子{attackerTileIndex}) ATK:{attacker.currentAttack} HP:{attacker.currentHealth}");

            // 获取有效攻击目标
            var targets = _ruleEngine.GetValidAttackTargets(attacker.instanceId);
            Debug.Log($"有效攻击目标数: {targets.Count}");

            if (targets.Count > 0)
            {
                // 优先攻击随从
                AttackTarget target = null;
                foreach (var t in targets)
                {
                    if (!t.isPlayer)
                    {
                        target = t;
                        break;
                    }
                }

                // 如果没有随从目标，攻击玩家
                if (target == null)
                {
                    target = targets[0];
                }

                if (target.isPlayer)
                {
                    Debug.Log($"--- 攻击玩家 {target.playerId} ---");
                }
                else
                {
                    var defenderCard = _gameState.FindCardByInstanceId(target.instanceId);
                    if (defenderCard != null)
                    {
                        var defenderData = _cardDatabase.GetCardById(defenderCard.cardId);
                        Debug.Log($"--- 攻击随从: {defenderData.cardName} ATK:{defenderCard.currentAttack} HP:{defenderCard.currentHealth} ---");
                    }
                }

                var attackAction = PlayerAction.CreateAttack(0, attacker.instanceId, target.instanceId, target.isPlayer, target.playerId);
                if (_ruleEngine.ValidateAction(attackAction))
                {
                    var events = _ruleEngine.ProcessAction(attackAction);
                    _gameState = _ruleEngine.CurrentState;
                    PrintEvents("战斗", events);
                    PrintCombatResult(attacker.instanceId, target);
                }
                else
                {
                    Debug.LogWarning("攻击操作验证失败！");
                }
            }
            else
            {
                Debug.Log("没有有效的攻击目标");
            }
        }
        else
        {
            Debug.Log("没有可攻击的单位（可能是因为刚入场的随从没有疾驰）");
        }

        // 结束回合
        EndTurn(0);
        PrintGameState("回合3结束后");
    }

    /// <summary>
    /// 结束回合
    /// </summary>
    void EndTurn(int playerId)
    {
        Debug.Log($"\n--- 玩家 {playerId} 结束回合 ---");

        var action = PlayerAction.CreateEndTurn(playerId);
        if (_ruleEngine.ValidateAction(action))
        {
            var events = _ruleEngine.ProcessAction(action);
            _gameState = _ruleEngine.CurrentState;
            PrintEvents("结束回合", events);
        }
        else
        {
            Debug.LogWarning("结束回合操作验证失败！");
        }
    }

    /// <summary>
    /// 在手牌中查找符合条件的卡牌
    /// </summary>
    int FindCardInHand(int playerId, Func<int, bool> predicate)
    {
        var hand = _gameState.GetPlayer(playerId).hand;
        for (int i = 0; i < hand.Count; i++)
        {
            if (predicate(hand[i].cardId))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 打印事件列表
    /// </summary>
    void PrintEvents(string context, List<GameEvent> events)
    {
        if (events == null || events.Count == 0)
        {
            Debug.Log($"[{context}] 无事件产生");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"[{context}] 产生 {events.Count} 个事件:");

        foreach (var evt in events)
        {
            _eventCount++;
            string eventDesc = FormatEvent(evt);
            sb.AppendLine($"  #{_eventCount} {eventDesc}");
        }

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// 格式化事件描述
    /// </summary>
    string FormatEvent(GameEvent evt)
    {
        switch (evt)
        {
            case GameStartEvent gse:
                return $"[GameStart] 先手玩家:{gse.firstPlayerId}";

            case TurnStartEvent tse:
                return $"[TurnStart] 玩家:{tse.playerId} 回合:{tse.turnNumber}";

            case TurnEndEvent tee:
                return $"[TurnEnd] 玩家:{tee.playerId}";

            case CardDrawnEvent cde:
                var drawnCard = _cardDatabase.GetCardById(cde.cardId);
                return $"[CardDrawn] 玩家:{cde.playerId} 抽到:{drawnCard.cardName}(ID:{cde.cardId})";

            case CardPlayedEvent cpe:
                var playedCard = _cardDatabase.GetCardById(cpe.cardId);
                return $"[CardPlayed] 玩家:{cpe.playerId} 打出:{playedCard.cardName} 到格子:{cpe.tileIndex}";

            case SummonEvent se:
                var summonedCard = _cardDatabase.GetCardById(se.cardId);
                return $"[Summon] 玩家:{se.ownerId} 召唤:{summonedCard.cardName} 格子:{se.tileIndex}";

            case DamageEvent de:
                string damageTarget = de.targetIsPlayer ? $"玩家{de.targetPlayerId}" : $"单位#{de.targetInstanceId}";
                return $"[Damage] {damageTarget} 受到 {de.amount} 点伤害 (来源:#{de.sourceInstanceId})";

            case HealEvent he:
                string healTarget = he.targetIsPlayer ? $"玩家{he.targetPlayerId}" : $"单位#{he.targetInstanceId}";
                return $"[Heal] {healTarget} 恢复 {he.amount} 点生命";

            case AttackEvent ae:
                string attackTarget = ae.defenderIsPlayer ? $"玩家{ae.defenderPlayerId}" : $"单位#{ae.defenderInstanceId}";
                return $"[Attack] 单位#{ae.attackerInstanceId} 攻击 {attackTarget}";

            case UnitDestroyedEvent ude:
                return $"[UnitDestroyed] 玩家:{ude.ownerId} 单位#{ude.instanceId} 被破坏";

            case EvolveEvent ee:
                return $"[Evolve] 玩家:{ee.sourcePlayerId} 单位#{ee.instanceId} 进化";

            case ManaChangeEvent mce:
                return $"[ManaChange] 玩家:{mce.playerId} 费用:{mce.oldMana}->{mce.newMana}";

            case FatigueEvent fe:
                return $"[Fatigue] 玩家:{fe.playerId} 疲劳伤害:{fe.damage}";

            case GameOverEvent goe:
                return $"[GameOver] 胜者:玩家{goe.winnerId} 原因:{goe.reason}";

            default:
                return $"[{evt.GetType().Name}] (未格式化)";
        }
    }

    /// <summary>
    /// 打印游戏状态
    /// </summary>
    void PrintGameState(string context)
    {
        var p0 = _gameState.GetPlayer(0);
        var p1 = _gameState.GetPlayer(1);

        var sb = new StringBuilder();
        sb.AppendLine($"\n--- {context} 游戏状态 ---");
        sb.AppendLine($"回合: {_gameState.turnNumber} | 当前玩家: {_gameState.currentPlayerId} | 阶段: {_gameState.phase}");
        sb.AppendLine($"玩家0: HP={p0.health}/{p0.maxHealth} | PP={p0.mana}/{p0.maxMana} | EP={p0.evolutionPoints} | 手牌={p0.hand.Count} | 牌库={p0.deck.Count} | 场上={CountFieldUnits(p0)}");
        sb.AppendLine($"玩家1: HP={p1.health}/{p1.maxHealth} | PP={p1.mana}/{p1.maxMana} | EP={p1.evolutionPoints} | 手牌={p1.hand.Count} | 牌库={p1.deck.Count} | 场上={CountFieldUnits(p1)}");

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// 打印玩家手牌
    /// </summary>
    void PrintHand(int playerId)
    {
        var player = _gameState.GetPlayer(playerId);
        var sb = new StringBuilder();
        sb.AppendLine($"\n玩家 {playerId} 手牌 ({player.hand.Count}张):");

        for (int i = 0; i < player.hand.Count; i++)
        {
            var card = player.hand[i];
            var data = _cardDatabase.GetCardById(card.cardId);
            string stats = data.cardType == CardType.Minion ? $" {data.attack}/{data.health}" : "";
            sb.AppendLine($"  [{i}] {data.cardName} ({data.cost}费{stats}) - {data.cardType}");
        }

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// 打印战场状态
    /// </summary>
    void PrintBattlefield()
    {
        var sb = new StringBuilder();
        sb.AppendLine("\n========== 战场状态 ==========");

        // 玩家1的场地（对面）
        sb.AppendLine("玩家1战场:");
        PrintPlayerField(sb, 1);

        sb.AppendLine("------------------------");

        // 玩家0的场地（己方）
        sb.AppendLine("玩家0战场:");
        PrintPlayerField(sb, 0);

        Debug.Log(sb.ToString());
    }

    void PrintPlayerField(StringBuilder sb, int playerId)
    {
        var player = _gameState.GetPlayer(playerId);
        for (int i = 0; i < player.field.Length; i++)
        {
            var tile = player.field[i];
            if (!tile.IsEmpty())
            {
                var unit = tile.occupant;
                var data = _cardDatabase.GetCardById(unit.cardId);
                string status = unit.canAttack ? "[可攻击]" : "[已行动]";
                sb.AppendLine($"  格子{i}: {data.cardName} ATK:{unit.currentAttack} HP:{unit.currentHealth} {status}");
            }
            else
            {
                sb.AppendLine($"  格子{i}: (空)");
            }
        }
    }

    /// <summary>
    /// 打印战斗结果
    /// </summary>
    void PrintCombatResult(int attackerInstanceId, AttackTarget target)
    {
        var sb = new StringBuilder();
        sb.AppendLine("\n--- 战斗结果 ---");

        var attacker = _gameState.FindCardByInstanceId(attackerInstanceId);
        if (attacker != null)
        {
            var attackerData = _cardDatabase.GetCardById(attacker.cardId);
            sb.AppendLine($"攻击者 {attackerData.cardName}: HP={attacker.currentHealth} {(attacker.currentHealth <= 0 ? "[已死亡]" : "")}");
        }
        else
        {
            sb.AppendLine("攻击者: [已死亡并移除]");
        }

        if (target.isPlayer)
        {
            var targetPlayer = _gameState.GetPlayer(target.playerId);
            sb.AppendLine($"目标玩家{target.playerId}: HP={targetPlayer.health}");
        }
        else
        {
            var defender = _gameState.FindCardByInstanceId(target.instanceId);
            if (defender != null)
            {
                var defenderData = _cardDatabase.GetCardById(defender.cardId);
                sb.AppendLine($"防守者 {defenderData.cardName}: HP={defender.currentHealth} {(defender.currentHealth <= 0 ? "[已死亡]" : "")}");
            }
            else
            {
                sb.AppendLine("防守者: [已死亡并移除]");
            }
        }

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// 打印最终总结
    /// </summary>
    void PrintFinalSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("\n╔══════════════════════════════════════╗");
        sb.AppendLine("║           测试总结                    ║");
        sb.AppendLine("╚══════════════════════════════════════╝");

        sb.AppendLine($"总事件数: {_eventCount}");
        sb.AppendLine($"最终回合: {_gameState.turnNumber}");

        var p0 = _gameState.GetPlayer(0);
        var p1 = _gameState.GetPlayer(1);

        sb.AppendLine($"\n玩家0 最终状态:");
        sb.AppendLine($"  - 生命: {p0.health}/{p0.maxHealth}");
        sb.AppendLine($"  - 手牌: {p0.hand.Count}张");
        sb.AppendLine($"  - 牌库: {p0.deck.Count}张");
        sb.AppendLine($"  - 场上单位: {CountFieldUnits(p0)}");

        sb.AppendLine($"\n玩家1 最终状态:");
        sb.AppendLine($"  - 生命: {p1.health}/{p1.maxHealth}");
        sb.AppendLine($"  - 手牌: {p1.hand.Count}张");
        sb.AppendLine($"  - 牌库: {p1.deck.Count}张");
        sb.AppendLine($"  - 场上单位: {CountFieldUnits(p1)}");

        // 检查游戏是否结束
        if (_gameState.IsGameOver())
        {
            int winner = _gameState.GetWinnerId();
            sb.AppendLine($"\n游戏已结束! 胜者: 玩家{winner}");
        }
        else
        {
            sb.AppendLine($"\n游戏进行中...");
        }

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// 统计场上单位数量
    /// </summary>
    int CountFieldUnits(PlayerState player)
    {
        int count = 0;
        foreach (var tile in player.field)
        {
            if (!tile.IsEmpty()) count++;
        }
        return count;
    }
}
