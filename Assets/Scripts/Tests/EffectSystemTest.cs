using UnityEngine;
using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Effects;
using ShadowCardSmash.Core.Events;

/// <summary>
/// Phase 2 效果系统测试
/// </summary>
public class EffectSystemTest : MonoBehaviour
{
    private EffectSystem _effectSystem;
    private GameState _gameState;
    private int _instanceIdCounter = 1;
    private TestCardDatabase _cardDatabase;

    void Start()
    {
        Debug.Log("=== 效果系统测试开始 ===");

        // 初始化
        Initialize();

        // 测试各种效果
        TestDamageEffect();
        TestHealEffect();
        TestDrawEffect();
        TestBuffEffect();
        TestSilenceEffect();
        TestDestroyEffect();
        TestVanishEffect();
        TestTargetSelector();
        TestConditionChecker();

        Debug.Log("=== 所有效果系统测试完成 ===");
    }

    void Initialize()
    {
        Debug.Log("--- 初始化效果系统 ---");

        // 创建卡牌数据库
        _cardDatabase = new TestCardDatabase();

        // 创建效果系统
        var random = new System.Random(12345);
        _effectSystem = EffectSystemFactory.Create(random);
        _effectSystem.SetCardDatabase(_cardDatabase);
        _effectSystem.SetInstanceIdGenerator(() => _instanceIdCounter++);

        // 创建游戏状态
        CreateTestGameState();

        Debug.Log("初始化完成");
    }

    void CreateTestGameState()
    {
        var deck0 = new List<int> { 1001, 1001, 1002, 1002, 1003 };
        var deck1 = new List<int> { 1001, 1001, 1002, 1002, 1003 };

        var player0 = PlayerState.CreateInitial(0, HeroClass.ClassA, deck0);
        var player1 = PlayerState.CreateInitial(1, HeroClass.ClassB, deck1, 10001);

        _gameState = GameState.CreateInitial(player0, player1, 12345);
        _gameState.turnNumber = 1;
        _gameState.phase = GamePhase.Main;

        // 给玩家0放置一个随从
        var minion0 = RuntimeCard.FromCardData(_cardDatabase.GetCardById(1001), _instanceIdCounter++, 0);
        minion0.canAttack = true;
        player0.field[0].PlaceUnit(minion0);

        // 给玩家1放置一个随从
        var minion1 = RuntimeCard.FromCardData(_cardDatabase.GetCardById(1001), _instanceIdCounter++, 1);
        minion1.canAttack = true;
        player1.field[0].PlaceUnit(minion1);

        Debug.Log($"游戏状态创建完成: 玩家0场上={player0.GetAllFieldUnits().Count}个单位, 玩家1场上={player1.GetAllFieldUnits().Count}个单位");
    }

    void TestDamageEffect()
    {
        Debug.Log("--- 测试伤害效果 ---");

        var target = _gameState.players[1].field[0].occupant;
        int oldHealth = target.currentHealth;

        var effect = new EffectData(EffectTrigger.OnPlay, TargetType.SingleEnemy, EffectType.Damage, 2);
        var events = _effectSystem.ProcessEffect(_gameState, null, 0, effect, new List<RuntimeCard> { target });

        Debug.Log($"伤害效果: 目标生命 {oldHealth} -> {target.currentHealth}");
        Debug.Log($"产生事件数: {events.Count}");
        foreach (var evt in events)
        {
            Debug.Log($"  事件: {evt.GetType().Name}");
        }
    }

    void TestHealEffect()
    {
        Debug.Log("--- 测试治疗效果 ---");

        // 先让玩家0受伤
        _gameState.players[0].TakeDamage(10);
        int oldHealth = _gameState.players[0].health;

        var effect = new EffectData(EffectTrigger.OnPlay, TargetType.AllyPlayer, EffectType.Heal, 5);
        var events = _effectSystem.ProcessEffect(_gameState, null, 0, effect);

        Debug.Log($"治疗效果: 玩家生命 {oldHealth} -> {_gameState.players[0].health}");
        Debug.Log($"产生事件数: {events.Count}");
    }

    void TestDrawEffect()
    {
        Debug.Log("--- 测试抽牌效果 ---");

        int oldHandCount = _gameState.players[0].hand.Count;
        int oldDeckCount = _gameState.players[0].deck.Count;

        var effect = new EffectData(EffectTrigger.OnPlay, TargetType.Self, EffectType.Draw, 2);
        var events = _effectSystem.ProcessEffect(_gameState, null, 0, effect);

        Debug.Log($"抽牌效果: 手牌 {oldHandCount} -> {_gameState.players[0].hand.Count}");
        Debug.Log($"牌库 {oldDeckCount} -> {_gameState.players[0].deck.Count}");
        Debug.Log($"产生事件数: {events.Count}");
    }

    void TestBuffEffect()
    {
        Debug.Log("--- 测试增益效果 ---");

        var target = _gameState.players[0].field[0].occupant;
        int oldAttack = target.currentAttack;
        int oldHealth = target.currentHealth;

        var effect = new EffectData(EffectTrigger.OnPlay, TargetType.SingleAlly, EffectType.Buff, 0);
        effect.parameters = new List<string> { "+2,+3" };

        var events = _effectSystem.ProcessEffect(_gameState, null, 0, effect, new List<RuntimeCard> { target });

        Debug.Log($"增益效果: 攻击 {oldAttack} -> {target.currentAttack}, 生命 {oldHealth} -> {target.currentHealth}");
        Debug.Log($"Buff数量: {target.buffs.Count}");
    }

    void TestSilenceEffect()
    {
        Debug.Log("--- 测试沉默效果 ---");

        var target = _gameState.players[0].field[0].occupant;
        target.hasWard = true;
        target.hasRush = true;

        Debug.Log($"沉默前: 守护={target.hasWard}, 突进={target.hasRush}, 沉默={target.isSilenced}");

        var effect = new EffectData(EffectTrigger.OnPlay, TargetType.SingleAlly, EffectType.Silence, 0);
        var events = _effectSystem.ProcessEffect(_gameState, null, 0, effect, new List<RuntimeCard> { target });

        Debug.Log($"沉默后: 守护={target.hasWard}, 突进={target.hasRush}, 沉默={target.isSilenced}");
        Debug.Log($"产生事件数: {events.Count}");
    }

    void TestDestroyEffect()
    {
        Debug.Log("--- 测试破坏效果 ---");

        // 给玩家1添加一个新随从用于测试
        var newMinion = RuntimeCard.FromCardData(_cardDatabase.GetCardById(1002), _instanceIdCounter++, 1);
        _gameState.players[1].field[1].PlaceUnit(newMinion);

        int oldFieldCount = _gameState.players[1].GetAllFieldUnits().Count;
        int oldGraveyardCount = _gameState.players[1].graveyard.Count;

        var effect = new EffectData(EffectTrigger.OnPlay, TargetType.SingleEnemy, EffectType.Destroy, 0);
        var events = _effectSystem.ProcessEffect(_gameState, null, 0, effect, new List<RuntimeCard> { newMinion });

        Debug.Log($"破坏效果: 场上单位 {oldFieldCount} -> {_gameState.players[1].GetAllFieldUnits().Count}");
        Debug.Log($"墓地 {oldGraveyardCount} -> {_gameState.players[1].graveyard.Count}");
    }

    void TestVanishEffect()
    {
        Debug.Log("--- 测试消失效果 ---");

        // 给玩家1添加一个新随从用于测试
        var newMinion = RuntimeCard.FromCardData(_cardDatabase.GetCardById(1003), _instanceIdCounter++, 1);
        _gameState.players[1].field[2].PlaceUnit(newMinion);

        int oldFieldCount = _gameState.players[1].GetAllFieldUnits().Count;
        int oldGraveyardCount = _gameState.players[1].graveyard.Count;

        var effect = new EffectData(EffectTrigger.OnPlay, TargetType.SingleEnemy, EffectType.Vanish, 0);
        var events = _effectSystem.ProcessEffect(_gameState, null, 0, effect, new List<RuntimeCard> { newMinion });

        Debug.Log($"消失效果: 场上单位 {oldFieldCount} -> {_gameState.players[1].GetAllFieldUnits().Count}");
        Debug.Log($"墓地 {oldGraveyardCount} -> {_gameState.players[1].graveyard.Count} (消失不进墓地)");

        foreach (var evt in events)
        {
            if (evt is UnitDestroyedEvent destroyEvt)
            {
                Debug.Log($"  消失标记: wasVanished={destroyEvt.wasVanished}");
            }
        }
    }

    void TestTargetSelector()
    {
        Debug.Log("--- 测试目标选择器 ---");

        var selector = new DefaultTargetSelector();

        // 测试是否需要玩家选择
        Debug.Log($"SingleEnemy需要选择: {selector.RequiresPlayerChoice(TargetType.SingleEnemy)}");
        Debug.Log($"AllEnemies需要选择: {selector.RequiresPlayerChoice(TargetType.AllEnemies)}");
        Debug.Log($"RandomEnemy需要选择: {selector.RequiresPlayerChoice(TargetType.RandomEnemy)}");

        // 测试自动选择
        var allEnemies = selector.SelectTargets(_gameState, null, 0, TargetType.AllEnemies);
        Debug.Log($"AllEnemies选中: {allEnemies.Count}个目标");

        var allAllies = selector.SelectTargets(_gameState, null, 0, TargetType.AllAllies);
        Debug.Log($"AllAllies选中: {allAllies.Count}个目标");
    }

    void TestConditionChecker()
    {
        Debug.Log("--- 测试条件检查器 ---");

        var checker = new DefaultConditionChecker();

        // 测试各种条件
        bool hasMinions = checker.CheckCondition(_gameState, null, 0, "ally_has_minions", null);
        Debug.Log($"ally_has_minions: {hasMinions}");

        bool healthCheck = checker.CheckCondition(_gameState, null, 0, "health_gte", new List<string> { "20" });
        Debug.Log($"health_gte 20: {healthCheck}");

        bool fieldCheck = checker.CheckCondition(_gameState, null, 0, "field_count_gte", new List<string> { "1" });
        Debug.Log($"field_count_gte 1: {fieldCheck}");

        bool emptyCondition = checker.CheckCondition(_gameState, null, 0, "", null);
        Debug.Log($"空条件: {emptyCondition} (应该为true)");
    }

    /// <summary>
    /// 测试用卡牌数据库
    /// </summary>
    private class TestCardDatabase : ICardDatabase
    {
        private Dictionary<int, CardData> _cards;

        public TestCardDatabase()
        {
            _cards = new Dictionary<int, CardData>
            {
                { 1001, CardData.CreateMinion(1001, "测试士兵", 2, 2, 3) },
                { 1002, CardData.CreateMinion(1002, "测试骑士", 3, 3, 4) },
                { 1003, CardData.CreateMinion(1003, "测试法师", 4, 4, 5) }
            };
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
