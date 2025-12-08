using System;
using System.Collections.Generic;

namespace ShadowCardSmash.Core.Data
{
    /// <summary>
    /// 运行时卡牌实例 - 游戏中实际存在的卡牌对象
    /// </summary>
    [Serializable]
    public class RuntimeCard
    {
        /// <summary>
        /// 运行时唯一ID
        /// </summary>
        public int instanceId;

        /// <summary>
        /// 对应的CardData ID
        /// </summary>
        public int cardId;

        /// <summary>
        /// 所属玩家ID
        /// </summary>
        public int ownerId;

        // ========== 当前状态 ==========

        /// <summary>
        /// 当前攻击力
        /// </summary>
        public int currentAttack;

        /// <summary>
        /// 当前生命值
        /// </summary>
        public int currentHealth;

        /// <summary>
        /// 最大生命值
        /// </summary>
        public int maxHealth;

        /// <summary>
        /// 是否已进化
        /// </summary>
        public bool isEvolved;

        /// <summary>
        /// 是否可以攻击
        /// </summary>
        public bool canAttack;

        /// <summary>
        /// 本回合是否已攻击
        /// </summary>
        public bool attackedThisTurn;

        /// <summary>
        /// 是否被沉默
        /// </summary>
        public bool isSilenced;

        /// <summary>
        /// 当前倒计时（护符用）
        /// </summary>
        public int currentCountdown;

        // ========== 关键词状态 ==========

        /// <summary>
        /// 是否有守护
        /// </summary>
        public bool hasWard;

        /// <summary>
        /// 是否有突进
        /// </summary>
        public bool hasRush;

        /// <summary>
        /// 是否有疾驰
        /// </summary>
        public bool hasStorm;

        // ========== Buff列表 ==========

        /// <summary>
        /// 当前身上的Buff
        /// </summary>
        public List<BuffData> buffs;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public RuntimeCard()
        {
            buffs = new List<BuffData>();
        }

        /// <summary>
        /// 从CardData创建运行时卡牌
        /// </summary>
        public static RuntimeCard FromCardData(CardData cardData, int instanceId, int ownerId)
        {
            var runtimeCard = new RuntimeCard
            {
                instanceId = instanceId,
                cardId = cardData.cardId,
                ownerId = ownerId,
                currentAttack = cardData.attack,
                currentHealth = cardData.health,
                maxHealth = cardData.health,
                isEvolved = false,
                canAttack = false, // 默认召唤失调
                attackedThisTurn = false,
                isSilenced = false,
                currentCountdown = cardData.countdown,
                hasWard = false,
                hasRush = false,
                hasStorm = false,
                buffs = new List<BuffData>()
            };

            // 从效果中提取初始关键词
            if (cardData.effects != null)
            {
                foreach (var effect in cardData.effects)
                {
                    if (effect.effectType == EffectType.GainKeyword && effect.trigger == EffectTrigger.OnPlay)
                    {
                        // 注：实际关键词应该在效果系统中处理
                    }
                }
            }

            return runtimeCard;
        }

        /// <summary>
        /// 应用伤害
        /// </summary>
        public void TakeDamage(int damage)
        {
            currentHealth -= damage;
        }

        /// <summary>
        /// 治疗
        /// </summary>
        public void Heal(int amount)
        {
            currentHealth += amount;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
        }

        /// <summary>
        /// 检查是否死亡
        /// </summary>
        public bool IsDead()
        {
            return currentHealth <= 0;
        }

        /// <summary>
        /// 进化
        /// </summary>
        public void Evolve(int evolvedAttack, int evolvedHealth)
        {
            if (isEvolved) return;

            int attackGain = evolvedAttack - (currentAttack - GetBuffAttack());
            int healthGain = evolvedHealth - maxHealth;

            currentAttack += attackGain;
            currentHealth += healthGain;
            maxHealth += healthGain;
            isEvolved = true;
            hasRush = true; // 进化获得突进
        }

        /// <summary>
        /// 获取Buff提供的攻击力
        /// </summary>
        private int GetBuffAttack()
        {
            int total = 0;
            foreach (var buff in buffs)
            {
                total += buff.attackModifier;
            }
            return total;
        }

        /// <summary>
        /// 沉默
        /// </summary>
        public void Silence()
        {
            isSilenced = true;
            hasWard = false;
            hasRush = false;
            hasStorm = false;
            // 注意：Buff数值保留，但关键词Buff的效果被移除
        }
    }
}
