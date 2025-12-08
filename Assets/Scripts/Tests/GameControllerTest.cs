using UnityEngine;
using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;
using ShadowCardSmash.Core.Effects;
using ShadowCardSmash.Core.Rules;
using ShadowCardSmash.Managers;
using ShadowCardSmash.Network;

/// <summary>
/// Phase 6 游戏控制器测试
/// </summary>
public class GameControllerTest : MonoBehaviour
{
    private GameController _gameController;
    private TestCardDatabase _cardDatabase;

    void Start()
    {
        Debug.Log("=== 游戏控制器测试开始 ===");

        SetupTest();

        // 测试本地游戏初始化
        TestLocalGameInit();

        // 测试游戏开始
        TestGameStart();

        // 测试查询方法
        TestQueryMethods();

        // 测试玩家操作
        TestPlayerActions();

        // 测试回合流程
        TestTurnFlow();

        // 测试网络游戏初始化
        TestNetworkGameInit();

        Debug.Log("=== 所有游戏控制器测试完成 ===");
    }

    void SetupTest()
    {
        Debug.Log("--- 设置测试环境 ---");

        // 创建GameController
        var go = new GameObject("GameController");
        _gameController = go.AddComponent<GameController>();

        // 创建测试卡牌数据库
        _cardDatabase = new TestCardDatabase();

        // 订阅事件
        _gameController.OnGameStarted += () => Debug.Log("事件: 游戏开始");
        _gameController.OnStateChanged += () => Debug.Log("事件: 状态更新");
        _gameController.OnTurnChanged += (playerId) => Debug.Log($"事件: 回合切换到玩家 {playerId}");
        _gameController.OnGameOver += (winnerId, reason) => Debug.Log($"事件: 游戏结束, 胜者={winnerId}, 原因={reason}");
        _gameController.OnGameEvent += (evt) => Debug.Log($"游戏事件: {evt.GetType().Name}");

        Debug.Log("测试环境设置完成");
    }

    void TestLocalGameInit()
    {
        Debug.Log("--- 测试本地游戏初始化 ---");

        // 初始化控制器
        _gameController.Initialize(_cardDatabase);

        // 创建测试卡组
        var deck0 = CreateTestDeck("玩家0卡组", HeroClass.ClassA);
        var deck1 = CreateTestDeck("玩家1卡组", HeroClass.ClassB);

        // 初始化本地游戏
        _gameController.InitializeLocalGame(deck0, deck1, 12345);

        Debug.Log($"本地游戏初始化: LocalPlayerId={_gameController.LocalPlayerId}");
        Debug.Log($"游戏状态: IsGameStarted={_gameController.IsGameStarted}, IsGameOver={_gameController.IsGameOver}");
        Debug.Log($"回合状态: IsMyTurn={_gameController.IsMyTurn}");
    }

    void TestGameStart()
    {
        Debug.Log("--- 测试游戏开始 ---");

        // 开始游戏
        _gameController.StartGame();

        var state = _gameController.CurrentState;
        Debug.Log($"游戏开始后状态:");
        Debug.Log($"  - 回合数: {state.turnNumber}");
        Debug.Log($"  - 当前玩家: {state.currentPlayerId}");
        Debug.Log($"  - IsGameStarted: {_gameController.IsGameStarted}");
        Debug.Log($"  - IsMyTurn: {_gameController.IsMyTurn}");

        // 检查玩家状态
        var localPlayer = _gameController.GetLocalPlayerState();
        var opponent = _gameController.GetOpponentPlayerState();

        Debug.Log($"本地玩家: HP={localPlayer.health}, 手牌={localPlayer.hand.Count}, PP={localPlayer.mana}/{localPlayer.maxMana}");
        Debug.Log($"对手玩家: HP={opponent.health}, 手牌={opponent.hand.Count}, PP={opponent.mana}/{opponent.maxMana}");
    }

    void TestQueryMethods()
    {
        Debug.Log("--- 测试查询方法 ---");

        // 获取可打出的卡牌
        var playableCards = _gameController.GetPlayableCardIndices();
        Debug.Log($"可打出的卡牌索引: [{string.Join(", ", playableCards)}]");

        // 获取本地战场
        var localField = _gameController.GetLocalField();
        int localFieldCount = 0;
        for (int i = 0; i < localField.Length; i++)
        {
            if (!localField[i].IsEmpty())
            {
                localFieldCount++;
                Debug.Log($"本地战场[{i}]: {localField[i].occupant.cardId}");
            }
        }
        Debug.Log($"本地战场单位数: {localFieldCount}");

        // 获取对手战场
        var opponentField = _gameController.GetOpponentField();
        int opponentFieldCount = 0;
        for (int i = 0; i < opponentField.Length; i++)
        {
            if (!opponentField[i].IsEmpty())
            {
                opponentFieldCount++;
            }
        }
        Debug.Log($"对手战场单位数: {opponentFieldCount}");

        // 获取可进化的随从
        var evolvableMinions = _gameController.GetEvolvableMinions();
        Debug.Log($"可进化随从数: {evolvableMinions.Count}");
    }

    void TestPlayerActions()
    {
        Debug.Log("--- 测试玩家操作 ---");

        // 确保是我的回合
        if (!_gameController.IsMyTurn)
        {
            Debug.Log("不是我的回合，切换控制玩家");
            _gameController.SwitchControlledPlayer();
        }

        // 尝试打出卡牌
        var playableCards = _gameController.GetPlayableCardIndices();
        if (playableCards.Count > 0)
        {
            int handIndex = playableCards[0];
            Debug.Log($"尝试打出手牌[{handIndex}]到位置0");

            bool result = _gameController.TryPlayCard(handIndex, 0);
            Debug.Log($"打出卡牌结果: {result}");

            // 再次检查战场
            var localField = _gameController.GetLocalField();
            Debug.Log($"战场位置0: IsEmpty={localField[0].IsEmpty()}");
        }
        else
        {
            Debug.Log("没有可打出的卡牌");
        }

        // 尝试攻击（如果有可攻击单位）
        var localPlayer = _gameController.GetLocalPlayerState();
        bool hasAttacker = false;
        int attackerInstanceId = -1;

        for (int i = 0; i < localPlayer.field.Length; i++)
        {
            if (!localPlayer.field[i].IsEmpty() && localPlayer.field[i].occupant.canAttack)
            {
                hasAttacker = true;
                attackerInstanceId = localPlayer.field[i].occupant.instanceId;
                break;
            }
        }

        if (hasAttacker)
        {
            var targets = _gameController.GetValidAttackTargets(attackerInstanceId);
            Debug.Log($"攻击者 {attackerInstanceId} 的有效目标数: {targets.Count}");

            if (targets.Count > 0)
            {
                var target = targets[0];
                bool attackResult = _gameController.TryAttack(
                    attackerInstanceId,
                    target.instanceId,
                    target.isPlayer,
                    target.playerId
                );
                Debug.Log($"攻击结果: {attackResult}");
            }
        }
        else
        {
            Debug.Log("没有可攻击的单位");
        }

        // 尝试进化
        var evolvable = _gameController.GetEvolvableMinions();
        if (evolvable.Count > 0)
        {
            int evolveInstanceId = evolvable[0].instanceId;
            bool canEvolve = _gameController.CanEvolve(evolveInstanceId);
            Debug.Log($"随从 {evolveInstanceId} 能否进化: {canEvolve}");

            if (canEvolve)
            {
                bool evolveResult = _gameController.TryEvolve(evolveInstanceId);
                Debug.Log($"进化结果: {evolveResult}");
            }
        }
        else
        {
            Debug.Log("没有可进化的随从");
        }
    }

    void TestTurnFlow()
    {
        Debug.Log("--- 测试回合流程 ---");

        var stateBefore = _gameController.CurrentState;
        Debug.Log($"结束回合前: 回合={stateBefore.turnNumber}, 当前玩家={stateBefore.currentPlayerId}");

        // 结束回合
        bool endTurnResult = _gameController.TryEndTurn();
        Debug.Log($"结束回合结果: {endTurnResult}");

        var stateAfter = _gameController.CurrentState;
        Debug.Log($"结束回合后: 回合={stateAfter.turnNumber}, 当前玩家={stateAfter.currentPlayerId}");

        // 切换控制以便继续测试
        _gameController.SwitchControlledPlayer();
        Debug.Log($"切换控制后: LocalPlayerId={_gameController.LocalPlayerId}, IsMyTurn={_gameController.IsMyTurn}");

        // 再结束一个回合
        if (_gameController.IsMyTurn)
        {
            _gameController.TryEndTurn();
            Debug.Log($"再次结束回合后: 回合={_gameController.CurrentState.turnNumber}");
        }
    }

    void TestNetworkGameInit()
    {
        Debug.Log("--- 测试网络游戏初始化 ---");

        // 创建新的GameController用于网络测试
        var networkGO = new GameObject("NetworkGameController");
        var networkController = networkGO.AddComponent<GameController>();

        // 创建网络管理器
        var networkManagerGO = new GameObject("TestNetworkManager");
        var networkManager = networkManagerGO.AddComponent<NetworkManager>();

        // 创建本地网络服务
        var (hostService, clientService) = LocalNetworkTestHelper.CreatePair(0f);
        networkManager.SetNetworkService(hostService);

        // 初始化控制器
        networkController.Initialize(_cardDatabase, networkManager);

        // 模拟网络游戏开始
        var deck0 = CreateTestDeck("网络玩家0", HeroClass.ClassA);
        var deck1 = CreateTestDeck("网络玩家1", HeroClass.ClassB);

        networkController.InitializeLocalGame(deck0, deck1, 54321);
        networkController.StartGame();

        Debug.Log($"网络游戏状态: IsGameStarted={networkController.IsGameStarted}");
        Debug.Log($"网络游戏回合: {networkController.CurrentState.turnNumber}");

        // 清理
        Destroy(networkGO, 1f);
        Destroy(networkManagerGO, 1f);
    }

    DeckData CreateTestDeck(string name, HeroClass heroClass)
    {
        var deck = DeckData.Create(name, heroClass);

        // 添加测试卡牌 (使用测试卡牌ID)
        deck.cards.Add(new DeckEntry(1001, 3)); // 3张1费随从
        deck.cards.Add(new DeckEntry(1002, 3)); // 3张2费随从
        deck.cards.Add(new DeckEntry(1003, 3)); // 3张3费随从
        deck.cards.Add(new DeckEntry(1004, 3)); // 3张2费法术
        deck.cards.Add(new DeckEntry(1005, 2)); // 2张1费护符
        deck.cards.Add(new DeckEntry(1006, 3)); // 3张4费随从
        deck.cards.Add(new DeckEntry(1007, 3)); // 3张5费随从

        // 填充到40张
        deck.cards.Add(new DeckEntry(1001, 7));
        deck.cards.Add(new DeckEntry(1002, 7));
        deck.cards.Add(new DeckEntry(1003, 6));

        return deck;
    }

    void OnDestroy()
    {
        if (_gameController != null)
        {
            Destroy(_gameController.gameObject);
        }
    }
}

/// <summary>
/// 测试用卡牌数据库
/// </summary>
public class TestCardDatabase : ICardDatabase
{
    private Dictionary<int, CardData> _cards;

    public TestCardDatabase()
    {
        _cards = new Dictionary<int, CardData>();
        InitializeTestCards();
    }

    private void InitializeTestCards()
    {
        // 1费随从 - 小型士兵
        _cards[1001] = CardData.CreateMinion(1001, "小型士兵", 1, 1, 2);
        _cards[1001].description = "一个普通的小型士兵";

        // 2费随从 - 森林精灵
        _cards[1002] = CardData.CreateMinion(1002, "森林精灵", 2, 2, 2);
        _cards[1002].description = "一个来自森林的精灵";

        // 3费随从 - 石像鬼 (带守护)
        _cards[1003] = CardData.CreateMinion(1003, "石像鬼", 3, 2, 4, HeroClass.Neutral, Rarity.Silver);
        _cards[1003].description = "坚固的石像鬼，具有守护能力";
        _cards[1003].tags.Add("Ward"); // 使用标签标记关键词

        // 2费法术 - 魔法飞弹
        _cards[1004] = CardData.CreateSpell(1004, "魔法飞弹", 2, "对一个敌方单位造成3点伤害");
        _cards[1004].effects.Add(new EffectData
        {
            trigger = EffectTrigger.OnPlay,
            effectType = EffectType.Damage,
            targetType = TargetType.SingleEnemy,
            value = 3
        });

        // 1费护符 - 神秘祭坛
        _cards[1005] = CardData.CreateAmulet(1005, "神秘祭坛", 1, 3);
        _cards[1005].description = "倒计时结束时，抽一张牌";
        _cards[1005].effects.Add(new EffectData
        {
            trigger = EffectTrigger.OnDestroy, // 倒计时结束触发OnDestroy
            effectType = EffectType.Draw,
            targetType = TargetType.Self,
            value = 1
        });

        // 4费随从 - 龙骑士 (带疾驰)
        _cards[1006] = CardData.CreateMinion(1006, "龙骑士", 4, 4, 3, HeroClass.Neutral, Rarity.Gold);
        _cards[1006].description = "冲锋战士";
        _cards[1006].tags.Add("Storm"); // 使用标签标记关键词

        // 5费随从 - 守护天使
        _cards[1007] = CardData.CreateMinion(1007, "守护天使", 5, 4, 5, HeroClass.Neutral, Rarity.Gold);
        _cards[1007].description = "入场时恢复2点生命";
        _cards[1007].effects.Add(new EffectData
        {
            trigger = EffectTrigger.OnPlay,
            effectType = EffectType.Heal,
            targetType = TargetType.AllyPlayer, // 治疗友方玩家
            value = 2
        });

        // 补偿卡
        _cards[9001] = CardData.CreateSpell(9001, "临时水晶", 0, "本回合获得1点PP");
        _cards[9001].effects.Add(new EffectData
        {
            trigger = EffectTrigger.OnPlay,
            effectType = EffectType.GainCost, // 使用GainCost代替GainPP
            targetType = TargetType.Self,
            value = 1
        });

        Debug.Log($"TestCardDatabase: 初始化了 {_cards.Count} 张测试卡牌");
    }

    public CardData GetCardById(int cardId)
    {
        if (_cards.TryGetValue(cardId, out var card))
        {
            return card;
        }

        Debug.LogWarning($"TestCardDatabase: 找不到卡牌ID {cardId}");
        // 返回一个默认卡牌 (id, name, cost, attack, health)
        return CardData.CreateMinion(cardId, $"未知卡牌{cardId}", 1, 1, 1);
    }

    public bool HasCard(int cardId)
    {
        return _cards.ContainsKey(cardId);
    }

    public List<CardData> GetAllCards()
    {
        return new List<CardData>(_cards.Values);
    }

    public List<CardData> GetCardsByClass(HeroClass heroClass)
    {
        var result = new List<CardData>();
        foreach (var card in _cards.Values)
        {
            if (card.heroClass == heroClass || card.heroClass == HeroClass.Neutral)
            {
                result.Add(card);
            }
        }
        return result;
    }
}
