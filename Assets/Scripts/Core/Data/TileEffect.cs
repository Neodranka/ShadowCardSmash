using System;

namespace ShadowCardSmash.Core.Data
{
    /// <summary>
    /// 格子效果 - 附加在战场格子上的持续效果
    /// </summary>
    [Serializable]
    public class TileEffect
    {
        /// <summary>
        /// 效果ID
        /// </summary>
        public int effectId;

        /// <summary>
        /// 来源卡牌ID
        /// </summary>
        public int sourceCardId;

        /// <summary>
        /// 效果类型
        /// </summary>
        public EffectType effectType;

        /// <summary>
        /// 效果数值
        /// </summary>
        public int value;

        /// <summary>
        /// 剩余回合数（-1表示永久）
        /// </summary>
        public int remainingTurns;

        /// <summary>
        /// 触发时机
        /// </summary>
        public EffectTrigger triggerTiming;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public TileEffect()
        {
            remainingTurns = -1; // 默认永久
        }

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        public TileEffect(int effectId, int sourceCardId, EffectType effectType, int value,
            int remainingTurns, EffectTrigger triggerTiming)
        {
            this.effectId = effectId;
            this.sourceCardId = sourceCardId;
            this.effectType = effectType;
            this.value = value;
            this.remainingTurns = remainingTurns;
            this.triggerTiming = triggerTiming;
        }
    }
}
