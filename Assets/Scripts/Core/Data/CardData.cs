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

        // ========== 增幅（Enhance）==========

        /// <summary>
        /// 增幅费用（0表示无增幅）
        /// </summary>
        public int enhanceCost;

        /// <summary>
        /// 增幅效果列表
        /// </summary>
        public List<EffectData> enhanceEffects;

        // ========== 进化效果 ==========

        /// <summary>
        /// 进化时效果列表
        /// </summary>
        public List<EffectData> evolveEffects;

        // ========== 目标选择 ==========

        /// <summary>
        /// 是否需要选择目标
        /// </summary>
        public bool requiresTarget;

        /// <summary>
        /// 有效目标类型
        /// </summary>
        public TargetType validTargets;

        // ========== 关键词 ==========

        /// <summary>
        /// 关键词列表
        /// </summary>
        public List<Keyword> keywords;

        // ========== 特殊标记 ==========

        /// <summary>
        /// 是否可以用EP进化（默认true）
        /// </summary>
        public bool canEvolveWithEP = true;

        /// <summary>
        /// 是否是衍生卡（TOKEN）
        /// </summary>
        public bool isToken;

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
            enhanceEffects = new List<EffectData>();
            evolveEffects = new List<EffectData>();
            keywords = new List<Keyword>();
            enhanceCost = 0;
            artworkPath = string.Empty;
            evolvedArtworkPath = string.Empty;
            countdown = -1; // 默认无倒计时
            canEvolveWithEP = true;
            isToken = false;
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
                enhanceEffects = new List<EffectData>(),
                enhanceCost = 0,
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
                enhanceEffects = new List<EffectData>(),
                enhanceCost = 0,
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
                effects = new List<EffectData>(),
                enhanceEffects = new List<EffectData>(),
                enhanceCost = 0
            };
        }

        /// <summary>
        /// 检查是否有增幅效果
        /// </summary>
        public bool HasEnhance()
        {
            return enhanceCost > 0 && enhanceEffects != null && enhanceEffects.Count > 0;
        }

        /// <summary>
        /// 检查是否可以以增幅费用使用
        /// </summary>
        public bool CanEnhance(int availableMana)
        {
            return HasEnhance() && availableMana >= enhanceCost;
        }
    }
}
