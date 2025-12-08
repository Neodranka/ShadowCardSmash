using System.Collections.Generic;
using UnityEngine;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Effects;

namespace ShadowCardSmash.Tests
{
    /// <summary>
    /// 测试用卡牌数据库 - 包含用于系统测试的基础卡牌
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
            // ID 1: "测试士兵" 2费 2/3 白板随从
            _cards[1] = CardData.CreateMinion(1, "测试士兵", 2, 2, 3);
            _cards[1].description = "一个普通的测试士兵";

            // ID 2: "精英战士" 3费 3/4 守护
            _cards[2] = CardData.CreateMinion(2, "精英战士", 3, 3, 4, HeroClass.Neutral, Rarity.Silver);
            _cards[2].description = "具有守护能力的精英战士";
            _cards[2].tags.Add("Ward"); // 标记守护关键词

            // ID 3: "火球术" 2费 法术 对一个目标造成3点伤害
            _cards[3] = CardData.CreateSpell(3, "火球术", 2, "对一个目标造成3点伤害");
            _cards[3].effects.Add(new EffectData
            {
                trigger = EffectTrigger.OnPlay,
                effectType = EffectType.Damage,
                targetType = TargetType.SingleEnemy,
                value = 3
            });

            // ID 4: "治疗师" 2费 1/3 开幕：恢复友方英雄2点生命
            _cards[4] = CardData.CreateMinion(4, "治疗师", 2, 1, 3);
            _cards[4].description = "开幕：恢复友方英雄2点生命";
            _cards[4].effects.Add(new EffectData
            {
                trigger = EffectTrigger.OnPlay,
                effectType = EffectType.Heal,
                targetType = TargetType.AllyPlayer,
                value = 2
            });

            // ID 5: "倒计时神龛" 2费 护符 倒计时2 谢幕：抽1张牌
            _cards[5] = CardData.CreateAmulet(5, "倒计时神龛", 2, 2);
            _cards[5].description = "倒计时2，谢幕：抽1张牌";
            _cards[5].effects.Add(new EffectData
            {
                trigger = EffectTrigger.OnDestroy,
                effectType = EffectType.Draw,
                targetType = TargetType.Self,
                value = 1
            });

            // ID 6: "疾驰骑士" 4费 3/2 疾驰
            _cards[6] = CardData.CreateMinion(6, "疾驰骑士", 4, 3, 2, HeroClass.Neutral, Rarity.Gold);
            _cards[6].description = "疾驰：可以在入场回合立即攻击";
            _cards[6].tags.Add("Storm");

            // ID 7: "突进猎手" 3费 2/2 突进
            _cards[7] = CardData.CreateMinion(7, "突进猎手", 3, 2, 2);
            _cards[7].description = "突进：可以在入场回合攻击随从";
            _cards[7].tags.Add("Rush");

            // ID 8: "小鬼" 1费 1/1 白板
            _cards[8] = CardData.CreateMinion(8, "小鬼", 1, 1, 1);
            _cards[8].description = "最基础的随从";

            // ID 9: "重甲卫士" 5费 4/6 守护
            _cards[9] = CardData.CreateMinion(9, "重甲卫士", 5, 4, 6, HeroClass.Neutral, Rarity.Gold);
            _cards[9].description = "强力的守护随从";
            _cards[9].tags.Add("Ward");

            // ID 10: "抽牌大师" 3费 2/2 开幕：抽1张牌
            _cards[10] = CardData.CreateMinion(10, "抽牌大师", 3, 2, 2);
            _cards[10].description = "开幕：抽1张牌";
            _cards[10].effects.Add(new EffectData
            {
                trigger = EffectTrigger.OnPlay,
                effectType = EffectType.Draw,
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
        /// 创建测试用牌库（40张牌）
        /// </summary>
        public List<int> CreateTestDeck()
        {
            var deck = new List<int>();

            // 每种卡各放4张，共40张
            for (int i = 0; i < 4; i++)
            {
                deck.Add(1);  // 测试士兵 x4
                deck.Add(2);  // 精英战士 x4
                deck.Add(3);  // 火球术 x4
                deck.Add(4);  // 治疗师 x4
                deck.Add(5);  // 倒计时神龛 x4
                deck.Add(6);  // 疾驰骑士 x4
                deck.Add(7);  // 突进猎手 x4
                deck.Add(8);  // 小鬼 x4
                deck.Add(9);  // 重甲卫士 x4
                deck.Add(10); // 抽牌大师 x4
            }

            return deck;
        }

        /// <summary>
        /// 创建简化的测试牌库（用于快速测试）
        /// </summary>
        public List<int> CreateSimpleTestDeck()
        {
            var deck = new List<int>();

            // 20张士兵 + 20张精英战士
            for (int i = 0; i < 20; i++)
            {
                deck.Add(1);  // 测试士兵
                deck.Add(2);  // 精英战士
            }

            return deck;
        }
    }
}
