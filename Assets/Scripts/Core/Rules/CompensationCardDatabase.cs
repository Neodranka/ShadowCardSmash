using System.Collections.Generic;
using ShadowCardSmash.Core.Data;

namespace ShadowCardSmash.Core.Rules
{
    /// <summary>
    /// 后手补偿卡数据库
    /// </summary>
    public class CompensationCardDatabase
    {
        private Dictionary<int, CardData> _compensationCards;

        // 补偿卡ID范围
        public const int COMPENSATION_CARD_ID_START = 10001;
        public const int COMPENSATION_CARD_ID_END = 10099;

        public CompensationCardDatabase()
        {
            _compensationCards = new Dictionary<int, CardData>();
            InitializeDefaultCards();
        }

        /// <summary>
        /// 初始化默认补偿卡
        /// </summary>
        private void InitializeDefaultCards()
        {
            // 临时水晶 - 本回合+1费用
            var tempCrystal = new CardData
            {
                cardId = 10001,
                cardName = "临时水晶",
                description = "本回合+1费用上限，并获得1点费用",
                cardType = CardType.Spell,
                rarity = Rarity.Bronze,
                cost = 0,
                heroClass = HeroClass.Neutral,
                tags = new List<string> { "补偿" },
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        targetType = TargetType.AllyPlayer,
                        effectType = EffectType.GainCost,
                        value = 1
                    }
                }
            };
            _compensationCards[10001] = tempCrystal;

            // 微型打击 - 对一个随从造成2点伤害
            var microStrike = new CardData
            {
                cardId = 10002,
                cardName = "微型打击",
                description = "对一个敌方随从造成2点伤害",
                cardType = CardType.Spell,
                rarity = Rarity.Bronze,
                cost = 0,
                heroClass = HeroClass.Neutral,
                tags = new List<string> { "补偿" },
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        targetType = TargetType.SingleEnemy,
                        effectType = EffectType.Damage,
                        value = 2
                    }
                }
            };
            _compensationCards[10002] = microStrike;

            // 小鬼召唤 - 召唤一个1/1的小鬼
            var impSummon = new CardData
            {
                cardId = 10003,
                cardName = "小鬼召唤",
                description = "召唤一个1/1的小鬼",
                cardType = CardType.Spell,
                rarity = Rarity.Bronze,
                cost = 0,
                heroClass = HeroClass.Neutral,
                tags = new List<string> { "补偿" },
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        targetType = TargetType.Self,
                        effectType = EffectType.Summon,
                        value = 1,
                        parameters = new List<string> { "99001" } // 小鬼的cardId
                    }
                }
            };
            _compensationCards[10003] = impSummon;

            // 贪婪抽取 - 弃1抽2
            var greedyDraw = new CardData
            {
                cardId = 10004,
                cardName = "贪婪抽取",
                description = "弃掉1张手牌，抽2张牌",
                cardType = CardType.Spell,
                rarity = Rarity.Bronze,
                cost = 0,
                heroClass = HeroClass.Neutral,
                tags = new List<string> { "补偿" },
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        targetType = TargetType.Self,
                        effectType = EffectType.Discard,
                        value = 1
                    },
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        targetType = TargetType.Self,
                        effectType = EffectType.Draw,
                        value = 2
                    }
                }
            };
            _compensationCards[10004] = greedyDraw;

            // 紧急治疗 - 恢复自己3点生命
            var emergencyHeal = new CardData
            {
                cardId = 10005,
                cardName = "紧急治疗",
                description = "恢复自己3点生命值",
                cardType = CardType.Spell,
                rarity = Rarity.Bronze,
                cost = 0,
                heroClass = HeroClass.Neutral,
                tags = new List<string> { "补偿" },
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        targetType = TargetType.AllyPlayer,
                        effectType = EffectType.Heal,
                        value = 3
                    }
                }
            };
            _compensationCards[10005] = emergencyHeal;

            // 创建小鬼衍生物（用于小鬼召唤）
            var imp = CardData.CreateMinion(99001, "小鬼", 1, 1, 1, HeroClass.Neutral, Rarity.Bronze);
            imp.tags = new List<string> { "恶魔", "衍生物" };
            _compensationCards[99001] = imp;
        }

        /// <summary>
        /// 获取所有补偿卡
        /// </summary>
        public List<CardData> GetAllCompensationCards()
        {
            var result = new List<CardData>();
            foreach (var card in _compensationCards.Values)
            {
                // 只返回补偿卡，不返回衍生物
                if (card.cardId >= COMPENSATION_CARD_ID_START && card.cardId <= COMPENSATION_CARD_ID_END)
                {
                    result.Add(card);
                }
            }
            return result;
        }

        /// <summary>
        /// 根据ID获取补偿卡
        /// </summary>
        public CardData GetCompensationCard(int cardId)
        {
            _compensationCards.TryGetValue(cardId, out var card);
            return card;
        }

        /// <summary>
        /// 检查是否是补偿卡
        /// </summary>
        public bool IsCompensationCard(int cardId)
        {
            return cardId >= COMPENSATION_CARD_ID_START && cardId <= COMPENSATION_CARD_ID_END
                   && _compensationCards.ContainsKey(cardId);
        }

        /// <summary>
        /// 获取衍生物卡牌
        /// </summary>
        public CardData GetTokenCard(int cardId)
        {
            _compensationCards.TryGetValue(cardId, out var card);
            return card;
        }
    }
}
