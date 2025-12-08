using System;

namespace ShadowCardSmash.Core.Data
{
    /// <summary>
    /// 卡组条目 - 记录卡组中一种卡牌的数量
    /// </summary>
    [Serializable]
    public class DeckEntry
    {
        /// <summary>
        /// 卡牌ID
        /// </summary>
        public int cardId;

        /// <summary>
        /// 数量（1-3）
        /// </summary>
        public int count;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public DeckEntry()
        {
            count = 1;
        }

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        public DeckEntry(int cardId, int count)
        {
            this.cardId = cardId;
            this.count = count;
        }
    }
}
