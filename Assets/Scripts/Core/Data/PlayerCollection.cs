using System;
using System.Collections.Generic;

namespace ShadowCardSmash.Core.Data
{
    /// <summary>
    /// 玩家收藏 - 记录玩家拥有的卡牌和卡组
    /// </summary>
    [Serializable]
    public class PlayerCollection
    {
        /// <summary>
        /// 玩家ID
        /// </summary>
        public string playerId;

        /// <summary>
        /// 拥有的卡牌（cardId -> 数量）
        /// 注：使用List模拟Dictionary以支持JSON序列化
        /// </summary>
        public List<CardOwnership> ownedCards;

        /// <summary>
        /// 玩家的卡组列表
        /// </summary>
        public List<DeckData> decks;

        /// <summary>
        /// 当前选中的卡组索引
        /// </summary>
        public int selectedDeckIndex;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public PlayerCollection()
        {
            playerId = string.Empty;
            ownedCards = new List<CardOwnership>();
            decks = new List<DeckData>();
            selectedDeckIndex = -1;
        }

        /// <summary>
        /// 创建新的玩家收藏
        /// </summary>
        public static PlayerCollection Create(string playerId)
        {
            return new PlayerCollection
            {
                playerId = playerId,
                ownedCards = new List<CardOwnership>(),
                decks = new List<DeckData>(),
                selectedDeckIndex = -1
            };
        }

        /// <summary>
        /// 获取指定卡牌的拥有数量
        /// </summary>
        public int GetOwnedCount(int cardId)
        {
            foreach (var ownership in ownedCards)
            {
                if (ownership.cardId == cardId)
                {
                    return ownership.count;
                }
            }
            return 0;
        }

        /// <summary>
        /// 添加卡牌到收藏
        /// </summary>
        public void AddCard(int cardId, int count = 1)
        {
            foreach (var ownership in ownedCards)
            {
                if (ownership.cardId == cardId)
                {
                    ownership.count += count;
                    return;
                }
            }
            ownedCards.Add(new CardOwnership(cardId, count));
        }

        /// <summary>
        /// 检查是否拥有足够的卡牌
        /// </summary>
        public bool HasCards(int cardId, int count)
        {
            return GetOwnedCount(cardId) >= count;
        }

        /// <summary>
        /// 获取当前选中的卡组
        /// </summary>
        public DeckData GetSelectedDeck()
        {
            if (selectedDeckIndex < 0 || selectedDeckIndex >= decks.Count)
            {
                return null;
            }
            return decks[selectedDeckIndex];
        }

        /// <summary>
        /// 添加新卡组
        /// </summary>
        public void AddDeck(DeckData deck)
        {
            decks.Add(deck);
            if (selectedDeckIndex < 0)
            {
                selectedDeckIndex = 0;
            }
        }

        /// <summary>
        /// 删除卡组
        /// </summary>
        public bool RemoveDeck(string deckId)
        {
            for (int i = 0; i < decks.Count; i++)
            {
                if (decks[i].deckId == deckId)
                {
                    decks.RemoveAt(i);
                    if (selectedDeckIndex >= decks.Count)
                    {
                        selectedDeckIndex = decks.Count - 1;
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 根据ID查找卡组
        /// </summary>
        public DeckData FindDeck(string deckId)
        {
            foreach (var deck in decks)
            {
                if (deck.deckId == deckId)
                {
                    return deck;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// 卡牌拥有记录（用于序列化Dictionary）
    /// </summary>
    [Serializable]
    public class CardOwnership
    {
        public int cardId;
        public int count;

        public CardOwnership()
        {
        }

        public CardOwnership(int cardId, int count)
        {
            this.cardId = cardId;
            this.count = count;
        }
    }
}
