using System;
using System.Collections.Generic;

namespace ShadowCardSmash.Core.Data
{
    /// <summary>
    /// Buff数据 - 记录随从身上的增益/减益效果
    /// </summary>
    [Serializable]
    public class BuffData
    {
        /// <summary>
        /// Buff的唯一ID
        /// </summary>
        public int buffId;

        /// <summary>
        /// 来源卡牌ID
        /// </summary>
        public int sourceCardId;

        /// <summary>
        /// 攻击力修正值
        /// </summary>
        public int attackModifier;

        /// <summary>
        /// 生命值修正值
        /// </summary>
        public int healthModifier;

        /// <summary>
        /// 持续回合数（-1表示永久）
        /// </summary>
        public int duration;

        /// <summary>
        /// 赋予的关键词
        /// </summary>
        public List<Keyword> grantedKeywords;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public BuffData()
        {
            grantedKeywords = new List<Keyword>();
            duration = -1; // 默认永久
        }

        /// <summary>
        /// 创建属性Buff
        /// </summary>
        public static BuffData CreateStatBuff(int buffId, int sourceCardId, int attackMod, int healthMod, int duration = -1)
        {
            return new BuffData
            {
                buffId = buffId,
                sourceCardId = sourceCardId,
                attackModifier = attackMod,
                healthModifier = healthMod,
                duration = duration,
                grantedKeywords = new List<Keyword>()
            };
        }

        /// <summary>
        /// 创建关键词Buff
        /// </summary>
        public static BuffData CreateKeywordBuff(int buffId, int sourceCardId, Keyword keyword, int duration = -1)
        {
            return new BuffData
            {
                buffId = buffId,
                sourceCardId = sourceCardId,
                attackModifier = 0,
                healthModifier = 0,
                duration = duration,
                grantedKeywords = new List<Keyword> { keyword }
            };
        }
    }

    /// <summary>
    /// 被添加的效果数据（如渴血符文添加的回合结束自伤效果）
    /// </summary>
    [Serializable]
    public class AddedEffectData
    {
        /// <summary>
        /// 触发器类型
        /// </summary>
        public EffectTrigger trigger;

        /// <summary>
        /// 效果类型
        /// </summary>
        public EffectType effectType;

        /// <summary>
        /// 效果数值
        /// </summary>
        public int value;

        /// <summary>
        /// 目标类型
        /// </summary>
        public TargetType targetType;
    }
}
