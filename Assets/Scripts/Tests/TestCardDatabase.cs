using System.Collections.Generic;
using UnityEngine;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Effects;
using ShadowCardSmash.Core.Cards;

namespace ShadowCardSmash.Tests
{
    /// <summary>
    /// 测试用卡牌数据库 - 使用吸血鬼卡牌
    /// </summary>
    public class TestCardDatabase : ICardDatabase
    {
        private Dictionary<int, CardData> _cards;

        public TestCardDatabase()
        {
            _cards = new Dictionary<int, CardData>();
            InitializeCards();
        }

        private void InitializeCards()
        {
            // 注册吸血鬼卡牌（包括普通卡和衍生卡）
            VampireCards.RegisterToDatabase(_cards);

            // 注册中立卡牌（包括普通卡和衍生卡）
            NeutralCards.RegisterToDatabase(_cards);

            Debug.Log($"TestCardDatabase: 初始化了 {_cards.Count} 张卡牌");
        }

        public CardData GetCardById(int cardId)
        {
            if (_cards.TryGetValue(cardId, out var card))
            {
                return card;
            }

            Debug.LogWarning($"TestCardDatabase: 找不到卡牌ID {cardId}，返回默认卡牌");
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

        /// <summary>
        /// 创建吸血鬼+中立测试牌库（40张牌）
        /// </summary>
        public List<int> CreateTestDeck()
        {
            var deck = new List<int>();

            // === 吸血鬼卡 (20张) ===
            // 铜卡 x2 each = 10张
            for (int i = 0; i < 2; i++)
            {
                deck.Add(2001); // 卖血者
                deck.Add(2002); // 血刺
                deck.Add(2003); // 鲜血献礼
                deck.Add(2004); // 撕咬
                deck.Add(2005); // 饥饿的捕食者
            }

            // 银卡 x2 each = 6张
            for (int i = 0; i < 2; i++)
            {
                deck.Add(2006); // 以伤换命
                deck.Add(2007); // 血扇
                deck.Add(2008); // 鲜血狂信徒
            }

            // 金卡 x2 each = 4张
            for (int i = 0; i < 2; i++)
            {
                deck.Add(2011); // 渴血的狂人
                deck.Add(2013); // 鲜血魔像
            }

            // === 中立卡 (20张) ===
            // 铜卡 x3 each = 12张
            for (int i = 0; i < 3; i++)
            {
                deck.Add(3001); // 胆怯的新兵
                deck.Add(3002); // 防卫队长
                deck.Add(3003); // 新晋骑士
                deck.Add(3004); // 小镇草药师
            }

            // 银卡 x2 each = 6张
            for (int i = 0; i < 2; i++)
            {
                deck.Add(3005); // 调整呼吸
                deck.Add(3006); // 军需官
                deck.Add(3007); // 倾盆大雨
            }

            // 金卡 x1 each = 2张
            deck.Add(3008); // 公会总管
            deck.Add(3009); // 爆破专家

            // 彩卡 x3 = 3张
            for (int i = 0; i < 3; i++)
            {
                deck.Add(3010); // 唐突的集结
            }

            // 总共 43 张
            return deck;
        }

        /// <summary>
        /// 创建简化的测试牌库（用于快速测试）
        /// </summary>
        public List<int> CreateSimpleTestDeck()
        {
            var deck = new List<int>();

            // 20张卖血者 + 20张血刺
            for (int i = 0; i < 20; i++)
            {
                deck.Add(2001); // 卖血者
                deck.Add(2002); // 血刺
            }

            return deck;
        }
    }
}
