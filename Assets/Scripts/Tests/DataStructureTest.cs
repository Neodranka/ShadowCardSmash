using UnityEngine;
using System.Collections.Generic;
using ShadowCardSmash.Core.Data;

/// <summary>
/// Phase 1 数据结构测试
/// </summary>
public class DataStructureTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== 数据结构测试开始 ===");

        // 测试1: 创建一张随从卡
        TestCreateMinionCard();

        // 测试2: 创建一张法术卡
        TestCreateSpellCard();

        // 测试3: 创建一张护符卡
        TestCreateAmuletCard();

        // 测试4: 创建玩家状态
        TestCreatePlayerState();

        // 测试5: 创建游戏状态
        TestCreateGameState();

        // 测试6: 创建卡组
        TestCreateDeck();

        // 测试7: 测试RuntimeCard
        TestRuntimeCard();

        // 测试8: 测试TileState
        TestTileState();

        Debug.Log("=== 所有测试完成 ===");
    }

    void TestCreateMinionCard()
    {
        Debug.Log("--- 测试1: 创建随从卡 ---");

        var card = new CardData
        {
            cardId = 1001,
            cardName = "测试士兵",
            description = "一个普通的士兵",
            cardType = CardType.Minion,
            rarity = Rarity.Bronze,
            cost = 2,
            heroClass = HeroClass.Neutral,
            tags = new List<string> { "士兵" },
            attack = 2,
            health = 3,
            evolvedAttack = 4,
            evolvedHealth = 5,
            effects = new List<EffectData>()
        };

        Debug.Log($"[随从卡] {card.cardName}: {card.cost}费 {card.attack}/{card.health}");
        Debug.Log($"  进化后: {card.evolvedAttack}/{card.evolvedHealth}");
        Debug.Log($"  标签: {string.Join(", ", card.tags)}");

        // 测试静态工厂方法
        var card2 = CardData.CreateMinion(1002, "工厂创建的随从", 3, 3, 4, HeroClass.ClassA, Rarity.Silver);
        Debug.Log($"[工厂方法] {card2.cardName}: {card2.cost}费 {card2.attack}/{card2.health}");
    }

    void TestCreateSpellCard()
    {
        Debug.Log("--- 测试2: 创建法术卡 ---");

        var effect = new EffectData
        {
            trigger = EffectTrigger.OnPlay,
            targetType = TargetType.SingleEnemy,
            effectType = EffectType.Damage,
            value = 3,
            conditionType = "",
            conditionParams = new List<string>(),
            parameters = new List<string>()
        };

        var card = new CardData
        {
            cardId = 2001,
            cardName = "火球术",
            description = "对一个敌方随从造成3点伤害",
            cardType = CardType.Spell,
            rarity = Rarity.Silver,
            cost = 3,
            heroClass = HeroClass.ClassA,
            tags = new List<string>(),
            effects = new List<EffectData> { effect }
        };

        Debug.Log($"[法术卡] {card.cardName}: {card.cost}费");
        Debug.Log($"  效果数量: {card.effects.Count}");
        Debug.Log($"  效果类型: {card.effects[0].effectType}, 数值: {card.effects[0].value}");

        // 测试静态工厂方法
        var card2 = CardData.CreateSpell(2002, "治疗术", 2, "恢复5点生命", HeroClass.ClassB);
        Debug.Log($"[工厂方法] {card2.cardName}: {card2.cost}费 - {card2.description}");
    }

    void TestCreateAmuletCard()
    {
        Debug.Log("--- 测试3: 创建护符卡 ---");

        var onDestroyEffect = new EffectData
        {
            trigger = EffectTrigger.OnDestroy,
            targetType = TargetType.Self,
            effectType = EffectType.Draw,
            value = 2,
            conditionType = "",
            conditionParams = new List<string>(),
            parameters = new List<string>()
        };

        var card = new CardData
        {
            cardId = 3001,
            cardName = "倒计时护符",
            description = "倒计时3：谢幕-抽2张牌",
            cardType = CardType.Amulet,
            rarity = Rarity.Gold,
            cost = 2,
            heroClass = HeroClass.Neutral,
            countdown = 3,
            canActivate = false,
            activateCost = 0,
            tags = new List<string>(),
            effects = new List<EffectData> { onDestroyEffect }
        };

        Debug.Log($"[护符卡] {card.cardName}: {card.cost}费, 倒计时={card.countdown}");
        Debug.Log($"  可启动: {card.canActivate}");

        // 测试静态工厂方法
        var card2 = CardData.CreateAmulet(3002, "永久护符", 4, -1, HeroClass.ClassC, Rarity.Legendary);
        Debug.Log($"[工厂方法] {card2.cardName}: {card2.cost}费, 倒计时={card2.countdown}(-1表示无)");
    }

    void TestCreatePlayerState()
    {
        Debug.Log("--- 测试4: 创建玩家状态 ---");

        // 使用手动创建方式
        var player = new PlayerState
        {
            playerId = 0,
            heroClass = HeroClass.ClassA,
            health = 40,
            maxHealth = 40,
            mana = 1,
            maxMana = 1,
            evolutionPoints = 0,
            hasEvolvedThisTurn = false,
            fatigueCounter = 0,
            deck = new List<int> { 1001, 1001, 1001, 2001, 2001 },
            hand = new List<RuntimeCard>(),
            field = new TileState[6],
            graveyard = new List<int>(),
            compensationCardId = -1
        };

        // 初始化6个格子
        for (int i = 0; i < 6; i++)
        {
            player.field[i] = new TileState(i);
        }

        Debug.Log($"[玩家状态] ID={player.playerId}, 职业={player.heroClass}");
        Debug.Log($"  生命: {player.health}/{player.maxHealth}");
        Debug.Log($"  费用: {player.mana}/{player.maxMana}");
        Debug.Log($"  牌库: {player.deck.Count}张");
        Debug.Log($"  EP: {player.evolutionPoints}");

        // 测试静态工厂方法
        var deckIds = new List<int> { 1001, 1002, 1003 };
        var player2 = PlayerState.CreateInitial(1, HeroClass.ClassB, deckIds, 10001);
        Debug.Log($"[工厂方法] 后手玩家 EP={player2.evolutionPoints}, 补偿卡ID={player2.compensationCardId}");
    }

    void TestCreateGameState()
    {
        Debug.Log("--- 测试5: 创建游戏状态 ---");

        // 创建两个玩家
        var deck0 = new List<int> { 1001, 1002, 1003 };
        var deck1 = new List<int> { 1001, 1002, 1003 };
        var player0 = PlayerState.CreateInitial(0, HeroClass.ClassA, deck0);
        var player1 = PlayerState.CreateInitial(1, HeroClass.ClassB, deck1, 10001);

        // 创建游戏状态
        var state = GameState.CreateInitial(player0, player1, 12345);
        state.turnNumber = 1;
        state.phase = GamePhase.Main;

        Debug.Log($"[游戏状态] 回合={state.turnNumber}, 当前玩家={state.currentPlayerId}, 阶段={state.phase}");
        Debug.Log($"  随机种子: {state.randomSeed}");
        Debug.Log($"  玩家0: {state.players[0].heroClass}, EP={state.players[0].evolutionPoints}");
        Debug.Log($"  玩家1: {state.players[1].heroClass}, EP={state.players[1].evolutionPoints}");

        // 测试辅助方法
        var currentPlayer = state.GetCurrentPlayer();
        var opponent = state.GetOpponentPlayer();
        Debug.Log($"  当前玩家职业: {currentPlayer.heroClass}");
        Debug.Log($"  对手职业: {opponent.heroClass}");
    }

    void TestCreateDeck()
    {
        Debug.Log("--- 测试6: 创建卡组 ---");

        var deck = new DeckData
        {
            deckId = System.Guid.NewGuid().ToString(),
            deckName = "测试卡组",
            heroClass = HeroClass.ClassA,
            cards = new List<DeckEntry>
            {
                new DeckEntry { cardId = 1001, count = 3 },
                new DeckEntry { cardId = 1002, count = 3 },
                new DeckEntry { cardId = 2001, count = 2 }
            },
            compensationCardId = 10001,
            lastModified = System.DateTimeOffset.Now.ToUnixTimeSeconds()
        };

        Debug.Log($"[卡组] {deck.deckName}");
        Debug.Log($"  职业: {deck.heroClass}");
        Debug.Log($"  卡牌数: {deck.GetTotalCardCount()}");
        Debug.Log($"  补偿卡ID: {deck.compensationCardId}");

        // 测试添加/移除卡牌
        deck.AddCard(1003);
        Debug.Log($"  添加卡牌后: {deck.GetTotalCardCount()}张");

        deck.RemoveCard(1001);
        Debug.Log($"  移除卡牌后: {deck.GetTotalCardCount()}张, 1001数量={deck.GetCardCount(1001)}");

        // 测试展开为列表
        var cardList = deck.ToCardIdList();
        Debug.Log($"  展开为列表: {cardList.Count}张");

        // 测试静态工厂方法
        var deck2 = DeckData.Create("工厂卡组", HeroClass.ClassC);
        Debug.Log($"[工厂方法] {deck2.deckName}, ID长度={deck2.deckId.Length}");
    }

    void TestRuntimeCard()
    {
        Debug.Log("--- 测试7: RuntimeCard ---");

        // 创建一张测试卡
        var cardData = CardData.CreateMinion(1001, "测试随从", 2, 3, 4);

        // 创建运行时实例
        var runtimeCard = RuntimeCard.FromCardData(cardData, 1, 0);

        Debug.Log($"[RuntimeCard] instanceId={runtimeCard.instanceId}, cardId={runtimeCard.cardId}");
        Debug.Log($"  攻击/生命: {runtimeCard.currentAttack}/{runtimeCard.currentHealth}");
        Debug.Log($"  可攻击: {runtimeCard.canAttack}, 已进化: {runtimeCard.isEvolved}");

        // 测试受伤
        runtimeCard.TakeDamage(2);
        Debug.Log($"  受伤后: {runtimeCard.currentHealth}, 是否死亡: {runtimeCard.IsDead()}");

        // 测试治疗
        runtimeCard.Heal(10);
        Debug.Log($"  治疗后: {runtimeCard.currentHealth} (上限{runtimeCard.maxHealth})");

        // 测试进化
        runtimeCard.Evolve(cardData.evolvedAttack, cardData.evolvedHealth);
        Debug.Log($"  进化后: {runtimeCard.currentAttack}/{runtimeCard.currentHealth}, 有突进: {runtimeCard.hasRush}");

        // 测试沉默
        runtimeCard.hasWard = true;
        runtimeCard.Silence();
        Debug.Log($"  沉默后: 守护={runtimeCard.hasWard}, 突进={runtimeCard.hasRush}");
    }

    void TestTileState()
    {
        Debug.Log("--- 测试8: TileState ---");

        var tile = new TileState(0);
        Debug.Log($"[TileState] 索引={tile.tileIndex}, 是否为空={tile.IsEmpty()}");

        // 放置单位
        var card = RuntimeCard.FromCardData(CardData.CreateMinion(1001, "测试", 1, 1, 1), 1, 0);
        bool placed = tile.PlaceUnit(card);
        Debug.Log($"  放置单位: {placed}, 是否为空={tile.IsEmpty()}");

        // 尝试再次放置
        var card2 = RuntimeCard.FromCardData(CardData.CreateMinion(1002, "测试2", 1, 1, 1), 2, 0);
        bool placed2 = tile.PlaceUnit(card2);
        Debug.Log($"  再次放置: {placed2} (应该失败)");

        // 添加格子效果
        var tileEffect = new TileEffect(1, 2001, EffectType.Damage, 1, 2, EffectTrigger.OnTurnEnd);
        tile.AddEffect(tileEffect);
        Debug.Log($"  添加效果后: 效果数量={tile.effects.Count}");

        // 减少效果持续时间
        tile.TickEffects();
        Debug.Log($"  Tick后: 剩余回合={tile.effects[0].remainingTurns}");

        // 移除单位
        var removed = tile.RemoveUnit();
        Debug.Log($"  移除单位: {removed != null}, 是否为空={tile.IsEmpty()}");
    }
}
