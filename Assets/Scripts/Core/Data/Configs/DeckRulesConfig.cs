using UnityEngine;

namespace ShadowCardSmash.Core.Data.Configs
{
    /// <summary>
    /// 卡组规则配置 - ScriptableObject配置文件
    /// </summary>
    [CreateAssetMenu(fileName = "DeckRules", menuName = "CardGame/DeckRules")]
    public class DeckRulesConfig : ScriptableObject
    {
        [Header("卡组大小")]
        [Tooltip("最小卡组大小")]
        public int minDeckSize = 40;

        [Tooltip("最大卡组大小")]
        public int maxDeckSize = 40;

        [Header("卡牌数量限制")]
        [Tooltip("每张普通卡最大数量")]
        public int maxCopiesPerCard = 3;

        [Tooltip("传说卡最大数量（此游戏统一为3）")]
        public int maxCopiesLegendary = 3;

        [Header("职业限制")]
        [Tooltip("是否强制职业限制")]
        public bool enforceClassRestriction = true;

        [Tooltip("是否允许中立卡")]
        public bool allowNeutralCards = true;

        /// <summary>
        /// 验证卡牌数量是否合法
        /// </summary>
        public bool IsValidCardCount(int count, Rarity rarity)
        {
            if (count < 0) return false;

            if (rarity == Rarity.Legendary)
            {
                return count <= maxCopiesLegendary;
            }
            return count <= maxCopiesPerCard;
        }

        /// <summary>
        /// 验证卡组大小是否合法
        /// </summary>
        public bool IsValidDeckSize(int size)
        {
            return size >= minDeckSize && size <= maxDeckSize;
        }

        /// <summary>
        /// 检查卡牌职业是否可用于卡组
        /// </summary>
        public bool IsCardClassValidForDeck(HeroClass cardClass, HeroClass deckClass)
        {
            if (!enforceClassRestriction) return true;

            // 中立卡可用于任何卡组
            if (cardClass == HeroClass.Neutral && allowNeutralCards)
            {
                return true;
            }

            // 职业卡必须匹配
            return cardClass == deckClass;
        }
    }
}
