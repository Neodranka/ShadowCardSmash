using System;
using System.Collections.Generic;

namespace ShadowCardSmash.Core.Data
{
    /// <summary>
    /// 效果数据 - 定义卡牌效果的触发条件和执行内容
    /// </summary>
    [Serializable]
    public class EffectData
    {
        /// <summary>
        /// 触发时机
        /// </summary>
        public EffectTrigger trigger;

        /// <summary>
        /// 条件类型标识
        /// </summary>
        public string conditionType;

        /// <summary>
        /// 条件参数
        /// </summary>
        public List<string> conditionParams;

        /// <summary>
        /// 目标类型
        /// </summary>
        public TargetType targetType;

        /// <summary>
        /// 效果类型
        /// </summary>
        public EffectType effectType;

        /// <summary>
        /// 效果数值
        /// </summary>
        public int value;

        /// <summary>
        /// 次要数值（如Buff的生命值部分）
        /// </summary>
        public int secondaryValue;

        /// <summary>
        /// 条件表达式（如 "total_self_damage>=10"）
        /// </summary>
        public string condition;

        /// <summary>
        /// 额外参数
        /// </summary>
        public List<string> parameters;

        /// <summary>
        /// 强化时是否替代普通效果
        /// </summary>
        public bool overrideNormalEffect;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public EffectData()
        {
            conditionType = string.Empty;
            conditionParams = new List<string>();
            parameters = new List<string>();
            condition = string.Empty;
        }

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        public EffectData(EffectTrigger trigger, TargetType targetType, EffectType effectType, int value)
        {
            this.trigger = trigger;
            this.targetType = targetType;
            this.effectType = effectType;
            this.value = value;
            this.secondaryValue = 0;
            this.condition = string.Empty;
            this.conditionType = string.Empty;
            this.conditionParams = new List<string>();
            this.parameters = new List<string>();
        }
    }
}
