using System;
using System.Collections.Generic;

namespace ShadowCardSmash.Core.Data
{
    /// <summary>
    /// 卡组数据 - 存储完整的卡组信息
    /// </summary>
    [Serializable]
    public class DeckData
    {
        /// <summary>
        /// 卡组唯一ID（GUID）
        /// </summary>
        public string deckId;

        /// <summary>
        /// 卡组名称
        /// </summary>
        public string deckName;

        /// <summary>
        /// 卡组职业
        /// </summary>
        public HeroClass heroClass;

        /// <summary>
        /// 卡组内容（卡牌ID和数量）
        /// </summary>
        public List<DeckEntry> cards;

        /// <summary>
        /// 后手补偿卡选择
        /// </summary>
        public int compensationCardId;

        /// <summary>
        /// 最后修改时间（Unix时间戳）
        /// </summary>
        public long lastModified;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public DeckData()
        {
            deckId = Guid.NewGuid().ToString();
            deckName = "新卡组";
            cards = new List<DeckEntry>();
            compensationCardId = -1;
            lastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// 创建新卡组
        /// </summary>
        public static DeckData Create(string name, HeroClass heroClass)
        {
            return new DeckData
            {
                deckId = Guid.NewGuid().ToString(),
                deckName = name,
                heroClass = heroClass,
                cards = new List<DeckEntry>(),
                compensationCardId = -1,
                lastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        /// <summary>
        /// 获取卡组总卡牌数
        /// </summary>
        public int GetTotalCardCount()
        {
            int total = 0;
            foreach (var entry in cards)
            {
                total += entry.count;
            }
            return total;
        }

        /// <summary>
        /// 获取指定卡牌的数量
        /// </summary>
        public int GetCardCount(int cardId)
        {
            foreach (var entry in cards)
            {
                if (entry.cardId == cardId)
                {
                    return entry.count;
                }
            }
            return 0;
        }

        /// <summary>
        /// 添加卡牌
        /// </summary>
        public bool AddCard(int cardId, int maxCopies = 3)
        {
            foreach (var entry in cards)
            {
                if (entry.cardId == cardId)
                {
                    if (entry.count >= maxCopies)
                    {
                        return false;
                    }
                    entry.count++;
                    UpdateTimestamp();
                    return true;
                }
            }

            cards.Add(new DeckEntry(cardId, 1));
            UpdateTimestamp();
            return true;
        }

        /// <summary>
        /// 移除卡牌
        /// </summary>
        public bool RemoveCard(int cardId)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].cardId == cardId)
                {
                    cards[i].count--;
                    if (cards[i].count <= 0)
                    {
                        cards.RemoveAt(i);
                    }
                    UpdateTimestamp();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 展开为卡牌ID列表（用于游戏初始化）
        /// </summary>
        public List<int> ToCardIdList()
        {
            var result = new List<int>();
            foreach (var entry in cards)
            {
                for (int i = 0; i < entry.count; i++)
                {
                    result.Add(entry.cardId);
                }
            }
            return result;
        }

        /// <summary>
        /// 更新时间戳
        /// </summary>
        private void UpdateTimestamp()
        {
            lastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
