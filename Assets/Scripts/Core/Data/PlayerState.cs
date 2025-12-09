using System;
using System.Collections.Generic;

namespace ShadowCardSmash.Core.Data
{
    /// <summary>
    /// 玩家状态 - 记录玩家在游戏中的所有状态
    /// </summary>
    [Serializable]
    public class PlayerState
    {
        // ========== 游戏常量 ==========
        public const int MAX_HEALTH = 30;
        public const int MAX_MANA = 10;
        public const int MAX_HAND_SIZE = 10;
        public const int FIELD_SIZE = 6;
        public const int SECOND_PLAYER_EP = 3;

        // ========== 基本信息 ==========

        /// <summary>
        /// 玩家ID（0=先手，1=后手）
        /// </summary>
        public int playerId;

        /// <summary>
        /// 玩家选择的职业
        /// </summary>
        public HeroClass heroClass;

        // ========== 生命与费用 ==========

        /// <summary>
        /// 当前生命值
        /// </summary>
        public int health;

        /// <summary>
        /// 最大生命值
        /// </summary>
        public int maxHealth;

        /// <summary>
        /// 当前费用
        /// </summary>
        public int mana;

        /// <summary>
        /// 费用上限
        /// </summary>
        public int maxMana;

        // ========== 进化点 ==========

        /// <summary>
        /// 进化点数
        /// </summary>
        public int evolutionPoints;

        /// <summary>
        /// 本回合是否已手动进化
        /// </summary>
        public bool hasEvolvedThisTurn;

        // ========== 疲劳 ==========

        /// <summary>
        /// 疲劳计数器（每次空牌库抽牌+1）
        /// </summary>
        public int fatigueCounter;

        // ========== 屏障 ==========

        /// <summary>
        /// 玩家是否拥有屏障（免疫一次伤害）
        /// </summary>
        public bool hasBarrier;

        // ========== 自伤统计（吸血鬼职业用）==========

        /// <summary>
        /// 本回合自伤伤害量
        /// </summary>
        public int selfDamageThisTurn;

        /// <summary>
        /// 游戏中累计自伤伤害量
        /// </summary>
        public int totalSelfDamage;

        // ========== 卡牌区域 ==========

        /// <summary>
        /// 牌库（cardId列表）
        /// </summary>
        public List<int> deck;

        /// <summary>
        /// 手牌
        /// </summary>
        public List<RuntimeCard> hand;

        /// <summary>
        /// 战场（6个格子）
        /// </summary>
        public TileState[] field;

        /// <summary>
        /// 墓地
        /// </summary>
        public List<int> graveyard;

        // ========== 后手补偿 ==========

        /// <summary>
        /// 后手补偿卡ID（-1表示无/先手）
        /// </summary>
        public int compensationCardId;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public PlayerState()
        {
            deck = new List<int>();
            hand = new List<RuntimeCard>();
            field = new TileState[FIELD_SIZE];
            graveyard = new List<int>();
            compensationCardId = -1;

            // 初始化格子
            for (int i = 0; i < FIELD_SIZE; i++)
            {
                field[i] = new TileState(i);
            }
        }

        /// <summary>
        /// 创建初始玩家状态
        /// </summary>
        public static PlayerState CreateInitial(int playerId, HeroClass heroClass, List<int> deckCardIds,
            int compensationCardId = -1)
        {
            var state = new PlayerState
            {
                playerId = playerId,
                heroClass = heroClass,
                health = MAX_HEALTH,
                maxHealth = MAX_HEALTH,
                mana = 0,
                maxMana = 0,
                evolutionPoints = SECOND_PLAYER_EP, // 双方都有3EP，但开放时机不同
                hasEvolvedThisTurn = false,
                fatigueCounter = 0,
                hasBarrier = false,
                selfDamageThisTurn = 0,
                totalSelfDamage = 0,
                deck = new List<int>(deckCardIds),
                hand = new List<RuntimeCard>(),
                graveyard = new List<int>(),
                compensationCardId = compensationCardId
            };

            // 初始化战场格子
            state.field = new TileState[FIELD_SIZE];
            for (int i = 0; i < FIELD_SIZE; i++)
            {
                state.field[i] = new TileState(i);
            }

            return state;
        }

        /// <summary>
        /// 检查手牌是否已满
        /// </summary>
        public bool IsHandFull()
        {
            return hand.Count >= MAX_HAND_SIZE;
        }

        /// <summary>
        /// 检查牌库是否为空
        /// </summary>
        public bool IsDeckEmpty()
        {
            return deck.Count == 0;
        }

        /// <summary>
        /// 获取空闲格子数量
        /// </summary>
        public int GetEmptyTileCount()
        {
            int count = 0;
            foreach (var tile in field)
            {
                if (tile.IsEmpty()) count++;
            }
            return count;
        }

        /// <summary>
        /// 获取第一个空闲格子索引
        /// </summary>
        public int GetFirstEmptyTileIndex()
        {
            for (int i = 0; i < FIELD_SIZE; i++)
            {
                if (field[i].IsEmpty()) return i;
            }
            return -1;
        }

        /// <summary>
        /// 检查是否有守护随从
        /// </summary>
        public bool HasWardMinion()
        {
            foreach (var tile in field)
            {
                if (tile.occupant != null && tile.occupant.hasWard)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取所有守护随从
        /// </summary>
        public List<RuntimeCard> GetWardMinions()
        {
            var result = new List<RuntimeCard>();
            foreach (var tile in field)
            {
                if (tile.occupant != null && tile.occupant.hasWard)
                {
                    result.Add(tile.occupant);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取战场上所有单位
        /// </summary>
        public List<RuntimeCard> GetAllFieldUnits()
        {
            var result = new List<RuntimeCard>();
            foreach (var tile in field)
            {
                if (tile.occupant != null)
                {
                    result.Add(tile.occupant);
                }
            }
            return result;
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        /// <returns>实际受到的伤害</returns>
        public int TakeDamage(int damage)
        {
            if (hasBarrier && damage > 0)
            {
                hasBarrier = false;
                UnityEngine.Debug.Log($"PlayerState: 玩家{playerId}的屏障抵挡了{damage}点伤害");
                return 0;
            }
            health -= damage;
            return damage;
        }

        /// <summary>
        /// 自伤（吸血鬼职业技能）
        /// </summary>
        /// <returns>实际受到的伤害</returns>
        public int TakeSelfDamage(int damage)
        {
            int actualDamage = TakeDamage(damage);
            if (actualDamage > 0)
            {
                selfDamageThisTurn += actualDamage;
                totalSelfDamage += actualDamage;
                UnityEngine.Debug.Log($"PlayerState: 玩家{playerId}自伤{actualDamage}点 (本回合:{selfDamageThisTurn}, 累计:{totalSelfDamage})");
            }
            return actualDamage;
        }

        /// <summary>
        /// 重置回合自伤计数
        /// </summary>
        public void ResetTurnSelfDamage()
        {
            selfDamageThisTurn = 0;
        }

        /// <summary>
        /// 治疗
        /// </summary>
        public void Heal(int amount)
        {
            health += amount;
            if (health > maxHealth)
            {
                health = maxHealth;
            }
        }

        /// <summary>
        /// 检查是否死亡
        /// </summary>
        public bool IsDead()
        {
            return health <= 0;
        }
    }
}
