using UnityEngine;
using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Data.Configs;
using ShadowCardSmash.Core.Effects;
using ShadowCardSmash.Core.Rules;
using ShadowCardSmash.Managers;

/// <summary>
/// Phase 4 存储服务测试
/// </summary>
public class StorageServiceTest : MonoBehaviour
{
    private LocalStorageService _storageService;
    private DeckManager _deckManager;
    private TestCardDatabase _cardDatabase;
    private const string TEST_PLAYER_ID = "test_player_001";

    void Start()
    {
        Debug.Log("=== 存储服务测试开始 ===");

        // 初始化
        Initialize();

        // 测试存储服务
        TestPlayerCollection();
        TestDeckStorage();

        // 测试卡组管理器
        TestDeckManager();

        // 清理测试数据
        CleanupTestData();

        Debug.Log("=== 所有存储服务测试完成 ===");
    }

    void Initialize()
    {
        Debug.Log("--- 初始化存储服务 ---");

        _storageService = new LocalStorageService();
        _cardDatabase = new TestCardDatabase();

        Debug.Log($"存储路径: {_storageService.GetBasePath()}");

        // 清理之前的测试数据
        if (_storageService.HasPlayerData(TEST_PLAYER_ID))
        {
            _storageService.DeleteAllPlayerData(TEST_PLAYER_ID);
            Debug.Log("已清理之前的测试数据");
        }
    }

    void TestPlayerCollection()
    {
        Debug.Log("--- 测试玩家收藏存储 ---");

        // 创建测试收藏
        var collection = PlayerCollection.Create(TEST_PLAYER_ID);
        collection.AddCard(1001, 3);
        collection.AddCard(1002, 2);
        collection.AddCard(2001, 1);

        Debug.Log($"创建收藏: {collection.ownedCards.Count}种卡牌");

        // 保存
        _storageService.SavePlayerCollection(collection);
        Debug.Log("收藏已保存");

        // 加载
        var loadedCollection = _storageService.LoadPlayerCollection(TEST_PLAYER_ID);
        Debug.Log($"加载收藏: {loadedCollection.ownedCards.Count}种卡牌");

        // 验证
        bool match = loadedCollection.GetOwnedCount(1001) == 3
                  && loadedCollection.GetOwnedCount(1002) == 2
                  && loadedCollection.GetOwnedCount(2001) == 1;
        Debug.Log($"数据匹配: {match}");
    }

    void TestDeckStorage()
    {
        Debug.Log("--- 测试卡组存储 ---");

        // 创建测试卡组
        var deck1 = DeckData.Create("测试卡组1", HeroClass.ClassA);
        for (int i = 0; i < 13; i++)
        {
            deck1.AddCard(1001);
            deck1.AddCard(1002);
            deck1.AddCard(2001);
        }
        deck1.AddCard(1001);
        deck1.compensationCardId = 10001;

        var deck2 = DeckData.Create("测试卡组2", HeroClass.ClassB);
        for (int i = 0; i < 20; i++)
        {
            deck2.AddCard(1001);
            deck2.AddCard(1002);
        }

        Debug.Log($"卡组1: {deck1.deckName}, {deck1.GetTotalCardCount()}张");
        Debug.Log($"卡组2: {deck2.deckName}, {deck2.GetTotalCardCount()}张");

        // 保存
        _storageService.SaveDeck(TEST_PLAYER_ID, deck1);
        _storageService.SaveDeck(TEST_PLAYER_ID, deck2);
        Debug.Log("卡组已保存");

        // 加载所有卡组
        var loadedDecks = _storageService.LoadAllDecks(TEST_PLAYER_ID);
        Debug.Log($"加载卡组数: {loadedDecks.Count}");

        foreach (var deck in loadedDecks)
        {
            Debug.Log($"  - {deck.deckName}: {deck.GetTotalCardCount()}张, 补偿卡ID={deck.compensationCardId}");
        }

        // 删除一个卡组
        _storageService.DeleteDeck(TEST_PLAYER_ID, deck2.deckId);
        loadedDecks = _storageService.LoadAllDecks(TEST_PLAYER_ID);
        Debug.Log($"删除后卡组数: {loadedDecks.Count}");
    }

    void TestDeckManager()
    {
        Debug.Log("--- 测试卡组管理器 ---");

        // 创建卡组管理器
        var rulesConfig = ScriptableObject.CreateInstance<DeckRulesConfig>();
        _deckManager = new DeckManager(_storageService, _cardDatabase, rulesConfig);
        _deckManager.Initialize(TEST_PLAYER_ID);

        Debug.Log($"初始卡组数: {_deckManager.Decks.Count}");

        // 创建新卡组
        var createResult = _deckManager.CreateDeck("管理器测试卡组", HeroClass.ClassC);
        Debug.Log($"创建卡组: {createResult.isValid}");
        Debug.Log($"当前卡组数: {_deckManager.Decks.Count}");

        // 获取新创建的卡组
        var newDeck = _deckManager.Decks[_deckManager.Decks.Count - 1];
        Debug.Log($"新卡组ID: {newDeck.deckId}");

        // 添加卡牌
        Debug.Log("添加卡牌...");
        for (int i = 0; i < 13; i++)
        {
            _deckManager.AddCardToDeck(newDeck.deckId, 1001);
            _deckManager.AddCardToDeck(newDeck.deckId, 1002);
            _deckManager.AddCardToDeck(newDeck.deckId, 2001);
        }
        _deckManager.AddCardToDeck(newDeck.deckId, 1001);

        Debug.Log($"添加后卡牌数: {_deckManager.GetDeckCardCount(newDeck.deckId)}");

        // 设置补偿卡
        var compensationCards = _deckManager.GetAllCompensationCards();
        Debug.Log($"可用补偿卡数: {compensationCards.Count}");
        foreach (var card in compensationCards)
        {
            Debug.Log($"  - [{card.cardId}] {card.cardName}: {card.description}");
        }

        _deckManager.SetCompensationCard(newDeck.deckId, 10002);
        Debug.Log($"设置补偿卡: 10002");

        // 验证卡组
        var validationResult = _deckManager.ValidateSelectedDeck();
        Debug.Log($"卡组验证: {validationResult.isValid}");
        if (!validationResult.isValid)
        {
            foreach (var error in validationResult.errors)
            {
                Debug.Log($"  错误: {error}");
            }
        }

        // 复制卡组
        _deckManager.DuplicateDeck(newDeck.deckId, "复制的卡组");
        Debug.Log($"复制后卡组数: {_deckManager.Decks.Count}");

        // 重命名卡组
        _deckManager.RenameDeck(newDeck.deckId, "重命名的卡组");
        Debug.Log($"重命名后: {_deckManager.GetDeck(newDeck.deckId).deckName}");

        // 移除卡牌
        _deckManager.RemoveCardFromDeck(newDeck.deckId, 1001);
        Debug.Log($"移除1张后: {_deckManager.GetDeckCardCount(newDeck.deckId)}张");

        // 选择卡组
        _deckManager.SelectDeckByIndex(0);
        Debug.Log($"选中卡组: {_deckManager.CurrentDeck?.deckName}");

        // 检查卡组完整性
        Debug.Log($"卡组完整: {_deckManager.IsDeckComplete(newDeck.deckId)}");
    }

    void CleanupTestData()
    {
        Debug.Log("--- 清理测试数据 ---");
        _storageService.DeleteAllPlayerData(TEST_PLAYER_ID);
        Debug.Log("测试数据已清理");
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

            // 中立随从
            _cards[1001] = CardData.CreateMinion(1001, "测试士兵", 1, 1, 2, HeroClass.Neutral);
            _cards[1002] = CardData.CreateMinion(1002, "测试骑士", 2, 2, 3, HeroClass.Neutral);
            _cards[1003] = CardData.CreateMinion(1003, "测试法师", 3, 3, 4, HeroClass.Neutral);

            // 中立法术
            var fireball = CardData.CreateSpell(2001, "火球术", 2, "造成3点伤害", HeroClass.Neutral);
            _cards[2001] = fireball;

            // 职业卡（用于测试职业限制）
            _cards[3001] = CardData.CreateMinion(3001, "职业A士兵", 1, 2, 1, HeroClass.ClassA);
            _cards[3002] = CardData.CreateMinion(3002, "职业B士兵", 1, 1, 3, HeroClass.ClassB);
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
