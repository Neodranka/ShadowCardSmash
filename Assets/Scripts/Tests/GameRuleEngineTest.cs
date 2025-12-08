using UnityEngine;
using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Effects;
using ShadowCardSmash.Core.Events;
using ShadowCardSmash.Core.Rules;

/// <summary>
/// Phase 3 游戏规则引擎测试
/// </summary>
public class GameRuleEngineTest : MonoBehaviour
{
    private GameRuleEngine _engine;
    private TestCardDatabase _cardDatabase;

    void Start()
    {
        Debug.Log("=== 游戏规则引擎测试开始 ===");

        // 初始化
        Initialize();

        // 测试游戏流程
        TestGameStart();
        TestPlayCard();
        TestAttack();
        TestEndTurn();
        TestEvolution();

        Debug.Log("=== 所有规则引擎测试完成 ===");
    }

    void Initialize()
    {
        Debug.Log("--- 初始化规则引擎 ---");

        _cardDatabase = new TestCardDatabase();
        _engine = new GameRuleEngine(_cardDatabase, 12345);

        // 创建初始游戏状态
        var deck0 = CreateTestDeck();
        var deck1 = CreateTestDeck();

        var player0 = PlayerState.CreateInitial(0, HeroClass.ClassA, deck0);
        var player1 = PlayerState.CreateInitial(1, HeroClass.ClassB, deck1, 10001);

        var gameState = GameState.CreateInitial(player0, player1, 12345);
        _engine.Initialize(gameState);

        Debug.Log("规则引擎初始化完成");
    }

    List<int> CreateTestDeck()
    {
        var deck = new List<int>();
        // 添加40张测试卡
        for (int i = 0; i < 10; i++)
        {
            deck.Add(1001); // 测试士兵
            deck.Add(1002); // 测试骑士
            deck.Add(1003); // 测试法师
            deck.Add(2001); // 火球术
        }
        return deck;
    }

    void TestGameStart()
    {
        Debug.Log("--- 测试游戏开始 ---");

        var events = _engine.StartGame();

        Debug.Log($"游戏开始产生 {events.Count} 个事件");

        var state = _engine.CurrentState;
        Debug.Log($"当前回合: {state.turnNumber}, 当前玩家: {state.currentPlayerId}");
        Debug.Log($"玩家0手牌: {state.players[0].hand.Count}张, 费用: {state.players[0].mana}/{state.players[0].maxMana}");
        Debug.Log($"玩家1手牌: {state.players[1].hand.Count}张, EP: {state.players[1].evolutionPoints}");

        // 打印部分事件
        int eventCount = 0;
        foreach (var evt in events)
        {
            if (eventCount < 5)
            {
                Debug.Log($"  事件: {evt.GetType().Name}");
            }
            eventCount++;
        }
        if (eventCount > 5)
        {
            Debug.Log($"  ... 还有 {eventCount - 5} 个事件");
        }
    }

    void TestPlayCard()
    {
        Debug.Log("--- 测试使用卡牌 ---");

        var state = _engine.CurrentState;
        var player = state.players[0];

        // 获取可使用的卡牌
        var playableCards = _engine.GetPlayableCards(0);
        Debug.Log($"可使用的手牌索引: [{string.Join(", ", playableCards)}]");

        if (playableCards.Count > 0)
        {
            // 找一张随从卡使用
            int handIndex = -1;
            for (int i = 0; i < player.hand.Count; i++)
            {
                var cardData = _cardDatabase.GetCardById(player.hand[i].cardId);
                if (cardData != null && cardData.cardType == CardType.Minion && cardData.cost <= player.mana)
                {
                    handIndex = i;
                    break;
                }
            }

            if (handIndex >= 0)
            {
                var card = player.hand[handIndex];
                var cardData = _cardDatabase.GetCardById(card.cardId);
                Debug.Log($"使用卡牌: {cardData.cardName} (费用:{cardData.cost})");

                var action = PlayerAction.CreatePlayCard(0, handIndex, 0);
                var events = _engine.ProcessAction(action);

                Debug.Log($"产生 {events.Count} 个事件");
                Debug.Log($"场上单位: {state.players[0].GetAllFieldUnits().Count}");
                Debug.Log($"剩余费用: {player.mana}");
            }
        }
    }

    void TestAttack()
    {
        Debug.Log("--- 测试攻击 ---");

        var state = _engine.CurrentState;

        // 先给敌方放一个随从用于测试
        var enemyMinion = RuntimeCard.FromCardData(_cardDatabase.GetCardById(1001), _engine.GenerateInstanceId(), 1);
        enemyMinion.canAttack = true;
        state.players[1].field[0].PlaceUnit(enemyMinion);

        // 获取我方场上的随从
        var myUnits = state.players[0].GetAllFieldUnits();
        if (myUnits.Count > 0)
        {
            var attacker = myUnits[0];
            // 让攻击者可以攻击（模拟非召唤失调）
            attacker.canAttack = true;
            attacker.hasStorm = true; // 给予疾驰以便测试

            var targets = _engine.GetValidAttackTargets(attacker.instanceId);
            Debug.Log($"有效攻击目标数: {targets.Count}");

            if (targets.Count > 0)
            {
                var target = targets[0];
                Debug.Log($"攻击目标: isPlayer={target.isPlayer}, instanceId={target.instanceId}");

                var action = PlayerAction.CreateAttack(
                    0,
                    attacker.instanceId,
                    target.instanceId,
                    target.isPlayer,
                    target.playerId
                );

                int attackerHealthBefore = attacker.currentHealth;
                var events = _engine.ProcessAction(action);

                Debug.Log($"攻击产生 {events.Count} 个事件");
                Debug.Log($"攻击者生命: {attackerHealthBefore} -> {attacker.currentHealth}");

                foreach (var evt in events)
                {
                    Debug.Log($"  事件: {evt.GetType().Name}");
                }
            }
        }
        else
        {
            Debug.Log("场上没有随从可以攻击");
        }
    }

    void TestEndTurn()
    {
        Debug.Log("--- 测试结束回合 ---");

        var state = _engine.CurrentState;
        int currentPlayer = state.currentPlayerId;
        int currentTurn = state.turnNumber;

        Debug.Log($"结束回合前: 回合{currentTurn}, 玩家{currentPlayer}");

        var action = PlayerAction.CreateEndTurn(currentPlayer);
        var events = _engine.ProcessAction(action);

        Debug.Log($"结束回合产生 {events.Count} 个事件");
        Debug.Log($"结束回合后: 回合{state.turnNumber}, 玩家{state.currentPlayerId}");
        Debug.Log($"新玩家费用: {state.GetCurrentPlayer().mana}/{state.GetCurrentPlayer().maxMana}");
        Debug.Log($"新玩家手牌: {state.GetCurrentPlayer().hand.Count}张");

        // 打印事件
        foreach (var evt in events)
        {
            if (evt is TurnStartEvent ts)
            {
                Debug.Log($"  TurnStartEvent: 玩家{ts.playerId}, 回合{ts.turnNumber}");
            }
            else if (evt is TurnEndEvent te)
            {
                Debug.Log($"  TurnEndEvent: 玩家{te.playerId}");
            }
            else if (evt is CardDrawnEvent cd)
            {
                Debug.Log($"  CardDrawnEvent: 玩家{cd.playerId}, 卡牌ID{cd.cardId}");
            }
        }
    }

    void TestEvolution()
    {
        Debug.Log("--- 测试进化系统 ---");

        var state = _engine.CurrentState;

        // 切换到玩家1（后手）并模拟到第4回合
        state.currentPlayerId = 1;
        state.turnNumber = 4;
        state.players[1].hasEvolvedThisTurn = false;

        Debug.Log($"玩家1 EP: {state.players[1].evolutionPoints}");
        Debug.Log($"当前回合: {state.turnNumber}");

        // 给玩家1放一个随从
        var minion = RuntimeCard.FromCardData(_cardDatabase.GetCardById(1001), _engine.GenerateInstanceId(), 1);
        state.players[1].field[1].PlaceUnit(minion);

        Debug.Log($"进化前: {minion.currentAttack}/{minion.currentHealth}, 已进化={minion.isEvolved}");

        // 检查是否可以进化
        bool canEvolve = _engine.CanEvolve(1, minion.instanceId);
        Debug.Log($"可以进化: {canEvolve}");

        if (canEvolve)
        {
            var evolvableMinions = _engine.GetEvolvableMinions(1);
            Debug.Log($"可进化随从数: {evolvableMinions.Count}");

            var action = PlayerAction.CreateEvolve(1, minion.instanceId);
            var events = _engine.ProcessAction(action);

            Debug.Log($"进化产生 {events.Count} 个事件");
            Debug.Log($"进化后: {minion.currentAttack}/{minion.currentHealth}, 已进化={minion.isEvolved}");
            Debug.Log($"剩余EP: {state.players[1].evolutionPoints}");
            Debug.Log($"本回合已进化: {state.players[1].hasEvolvedThisTurn}");
            Debug.Log($"获得突进: {minion.hasRush}");
        }
    }

    /// <summary>
    /// 测试用卡牌数据库
    /// </summary>
    private class TestCardDatabase : ICardDatabase
    {
        private Dictionary<int, CardData> _cards;

        public TestCardDatabase()
        {
            _cards = new Dictionary<int, CardData>();

            // 随从卡
            var soldier = CardData.CreateMinion(1001, "测试士兵", 1, 1, 2);
            soldier.evolvedAttack = 3;
            soldier.evolvedHealth = 4;
            _cards[1001] = soldier;

            var knight = CardData.CreateMinion(1002, "测试骑士", 2, 2, 3);
            knight.evolvedAttack = 4;
            knight.evolvedHealth = 5;
            _cards[1002] = knight;

            var mage = CardData.CreateMinion(1003, "测试法师", 3, 3, 4);
            mage.evolvedAttack = 5;
            mage.evolvedHealth = 6;
            _cards[1003] = mage;

            // 法术卡
            var fireball = CardData.CreateSpell(2001, "火球术", 2, "造成3点伤害");
            fireball.effects = new List<EffectData>
            {
                new EffectData(EffectTrigger.OnPlay, TargetType.SingleEnemy, EffectType.Damage, 3)
            };
            _cards[2001] = fireball;
        }

        public CardData GetCardById(int cardId)
        {
            _cards.TryGetValue(cardId, out var card);
            return card;
        }

        public bool HasCard(int cardId)
        {
            return _cards.ContainsKey(cardId);
        }
    }
}
