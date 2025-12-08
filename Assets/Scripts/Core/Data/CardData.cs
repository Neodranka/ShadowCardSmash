using System;
using System.Collections.Generic;

namespace ShadowCardSmash.Core.Data
{
    /// <summary>
    /// 卡牌数据 - 定义卡牌的所有静态属性
    /// </summary>
    [Serializable]
    public class CardData
    {
        // ========== 基础信息 ==========

        /// <summary>
        /// 卡牌ID（唯一标识）
        /// </summary>
        public int cardId;

        /// <summary>
        /// 卡牌名称
        /// </summary>
        public string cardName;

        /// <summary>
        /// 卡牌描述（效果文本）
        /// </summary>
        public string description;

        /// <summary>
        /// 卡牌类型（随从/法术/护符）
        /// </summary>
        public CardType cardType;

        /// <summary>
        /// 稀有度
        /// </summary>
        public Rarity rarity;

        /// <summary>
        /// 费用
        /// </summary>
        public int cost;

        // ========== 归属 ==========

        /// <summary>
        /// 所属职业
        /// </summary>
        public HeroClass heroClass;

        /// <summary>
        /// 标签列表（如：龙、士兵、机械等）
        /// </summary>
        public List<string> tags;

        // ========== 随从属性 ==========

        /// <summary>
        /// 攻击力
        /// </summary>
        public int attack;

        /// <summary>
        /// 生命值
        /// </summary>
        public int health;

        // ========== 护符属性 ==========

        /// <summary>
        /// 倒计时（-1表示无倒计时）
        /// </summary>
        public int countdown;

        /// <summary>
        /// 是否可启动
        /// </summary>
        public bool canActivate;

        /// <summary>
        /// 启动费用
        /// </summary>
        public int activateCost;

        // ========== 进化属性 ==========

        /// <summary>
        /// 进化后攻击力
        /// </summary>
        public int evolvedAttack;

        /// <summary>
        /// 进化后生命值
        /// </summary>
        public int evolvedHealth;

        // ========== 效果 ==========

        /// <summary>
        /// 效果列表
        /// </summary>
        public List<EffectData> effects;

        // ========== 资源引用 ==========

        /// <summary>
        /// 卡牌插画路径
        /// </summary>
        public string artworkPath;

        /// <summary>
        /// 进化后插画路径
        /// </summary>
        public string evolvedArtworkPath;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public CardData()
        {
            cardName = string.Empty;
            description = string.Empty;
            tags = new List<string>();
            effects = new List<EffectData>();
            artworkPath = string.Empty;
            evolvedArtworkPath = string.Empty;
            countdown = -1; // 默认无倒计时
        }

        /// <summary>
        /// 创建随从卡
        /// </summary>
        public static CardData CreateMinion(int id, string name, int cost, int attack, int health,
            HeroClass heroClass = HeroClass.Neutral, Rarity rarity = Rarity.Bronze)
        {
            return new CardData
            {
                cardId = id,
                cardName = name,
                cardType = CardType.Minion,
                cost = cost,
                attack = attack,
                health = health,
                evolvedAttack = attack + 2,
                evolvedHealth = health + 2,
                heroClass = heroClass,
                rarity = rarity,
                tags = new List<string>(),
                effects = new List<EffectData>(),
                countdown = -1
            };
        }

        /// <summary>
        /// 创建法术卡
        /// </summary>
        public static CardData CreateSpell(int id, string name, int cost, string description,
            HeroClass heroClass = HeroClass.Neutral, Rarity rarity = Rarity.Bronze)
        {
            return new CardData
            {
                cardId = id,
                cardName = name,
                cardType = CardType.Spell,
                cost = cost,
                description = description,
                heroClass = heroClass,
                rarity = rarity,
                tags = new List<string>(),
                effects = new List<EffectData>(),
                countdown = -1
            };
        }

        /// <summary>
        /// 创建护符卡
        /// </summary>
        public static CardData CreateAmulet(int id, string name, int cost, int countdown,
            HeroClass heroClass = HeroClass.Neutral, Rarity rarity = Rarity.Bronze)
        {
            return new CardData
            {
                cardId = id,
                cardName = name,
                cardType = CardType.Amulet,
                cost = cost,
                countdown = countdown,
                heroClass = heroClass,
                rarity = rarity,
                tags = new List<string>(),
                effects = new List<EffectData>()
            };
        }
    }
}
