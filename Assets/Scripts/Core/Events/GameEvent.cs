using System;
using System.Collections.Generic;
using ShadowCardSmash.Core.Data;

namespace ShadowCardSmash.Core.Events
{
    /// <summary>
    /// 游戏事件基类
    /// </summary>
    [Serializable]
    public abstract class GameEvent
    {
        private static int _nextEventId = 1;

        /// <summary>
        /// 事件唯一ID
        /// </summary>
        public int eventId;

        /// <summary>
        /// 事件时间戳
        /// </summary>
        public long timestamp;

        /// <summary>
        /// 事件来源玩家ID
        /// </summary>
        public int sourcePlayerId;

        protected GameEvent()
        {
            eventId = _nextEventId++;
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        protected GameEvent(int sourcePlayerId) : this()
        {
            this.sourcePlayerId = sourcePlayerId;
        }

        /// <summary>
        /// 重置事件ID计数器（用于测试）
        /// </summary>
        public static void ResetEventIdCounter()
        {
            _nextEventId = 1;
        }
    }

    /// <summary>
    /// 卡牌抽取事件
    /// </summary>
    [Serializable]
    public class CardDrawnEvent : GameEvent
    {
        public int playerId;
        public int cardId;
        public int instanceId;
        public bool fromFatigue;
        public bool discardedDueToFull;

        public CardDrawnEvent() : base() { }

        public CardDrawnEvent(int playerId, int cardId, int instanceId, bool fromFatigue = false, bool discardedDueToFull = false)
            : base(playerId)
        {
            this.playerId = playerId;
            this.cardId = cardId;
            this.instanceId = instanceId;
            this.fromFatigue = fromFatigue;
            this.discardedDueToFull = discardedDueToFull;
        }
    }

    /// <summary>
    /// 卡牌使用事件
    /// </summary>
    [Serializable]
    public class CardPlayedEvent : GameEvent
    {
        public int playerId;
        public int cardId;
        public int instanceId;
        public int tileIndex; // 放置位置，法术为-1
        public int manaCost;

        public CardPlayedEvent() : base() { }

        public CardPlayedEvent(int playerId, int cardId, int instanceId, int tileIndex, int manaCost)
            : base(playerId)
        {
            this.playerId = playerId;
            this.cardId = cardId;
            this.instanceId = instanceId;
            this.tileIndex = tileIndex;
            this.manaCost = manaCost;
        }
    }

    /// <summary>
    /// 伤害事件
    /// </summary>
    [Serializable]
    public class DamageEvent : GameEvent
    {
        public int sourceInstanceId;
        public int targetInstanceId;
        public int amount;
        public bool targetIsPlayer;
        public int targetPlayerId;

        public DamageEvent() : base() { }

        public DamageEvent(int sourcePlayerId, int sourceInstanceId, int targetInstanceId, int amount,
            bool targetIsPlayer = false, int targetPlayerId = -1)
            : base(sourcePlayerId)
        {
            this.sourceInstanceId = sourceInstanceId;
            this.targetInstanceId = targetInstanceId;
            this.amount = amount;
            this.targetIsPlayer = targetIsPlayer;
            this.targetPlayerId = targetPlayerId;
        }
    }

    /// <summary>
    /// 治疗事件
    /// </summary>
    [Serializable]
    public class HealEvent : GameEvent
    {
        public int targetInstanceId;
        public int amount;
        public int actualHealed;
        public bool targetIsPlayer;
        public int targetPlayerId;

        public HealEvent() : base() { }

        public HealEvent(int sourcePlayerId, int targetInstanceId, int amount, int actualHealed,
            bool targetIsPlayer = false, int targetPlayerId = -1)
            : base(sourcePlayerId)
        {
            this.targetInstanceId = targetInstanceId;
            this.amount = amount;
            this.actualHealed = actualHealed;
            this.targetIsPlayer = targetIsPlayer;
            this.targetPlayerId = targetPlayerId;
        }
    }

    /// <summary>
    /// 单位被破坏事件
    /// </summary>
    [Serializable]
    public class UnitDestroyedEvent : GameEvent
    {
        public int instanceId;
        public int cardId;
        public int tileIndex;
        public int ownerId;
        public bool wasVanished; // true表示消失而非破坏

        public UnitDestroyedEvent() : base() { }

        public UnitDestroyedEvent(int sourcePlayerId, int instanceId, int cardId, int tileIndex, int ownerId, bool wasVanished = false)
            : base(sourcePlayerId)
        {
            this.instanceId = instanceId;
            this.cardId = cardId;
            this.tileIndex = tileIndex;
            this.ownerId = ownerId;
            this.wasVanished = wasVanished;
        }
    }

    /// <summary>
    /// 召唤事件
    /// </summary>
    [Serializable]
    public class SummonEvent : GameEvent
    {
        public int instanceId;
        public int cardId;
        public int tileIndex;
        public int ownerId;

        public SummonEvent() : base() { }

        public SummonEvent(int sourcePlayerId, int instanceId, int cardId, int tileIndex, int ownerId)
            : base(sourcePlayerId)
        {
            this.instanceId = instanceId;
            this.cardId = cardId;
            this.tileIndex = tileIndex;
            this.ownerId = ownerId;
        }
    }

    /// <summary>
    /// 攻击事件
    /// </summary>
    [Serializable]
    public class AttackEvent : GameEvent
    {
        public int attackerInstanceId;
        public int defenderInstanceId;
        public bool defenderIsPlayer;
        public int defenderPlayerId;

        public AttackEvent() : base() { }

        public AttackEvent(int sourcePlayerId, int attackerInstanceId, int defenderInstanceId,
            bool defenderIsPlayer = false, int defenderPlayerId = -1)
            : base(sourcePlayerId)
        {
            this.attackerInstanceId = attackerInstanceId;
            this.defenderInstanceId = defenderInstanceId;
            this.defenderIsPlayer = defenderIsPlayer;
            this.defenderPlayerId = defenderPlayerId;
        }
    }

    /// <summary>
    /// 进化事件
    /// </summary>
    [Serializable]
    public class EvolveEvent : GameEvent
    {
        public int instanceId;
        public int cardId;
        public bool wasManual; // 是否手动进化（消耗EP）

        public EvolveEvent() : base() { }

        public EvolveEvent(int sourcePlayerId, int instanceId, int cardId, bool wasManual)
            : base(sourcePlayerId)
        {
            this.instanceId = instanceId;
            this.cardId = cardId;
            this.wasManual = wasManual;
        }
    }

    /// <summary>
    /// 增益/减益事件
    /// </summary>
    [Serializable]
    public class BuffEvent : GameEvent
    {
        public int targetInstanceId;
        public int attackChange;
        public int healthChange;
        public List<Keyword> keywordsGranted;

        public BuffEvent() : base()
        {
            keywordsGranted = new List<Keyword>();
        }

        public BuffEvent(int sourcePlayerId, int targetInstanceId, int attackChange, int healthChange,
            List<Keyword> keywordsGranted = null)
            : base(sourcePlayerId)
        {
            this.targetInstanceId = targetInstanceId;
            this.attackChange = attackChange;
            this.healthChange = healthChange;
            this.keywordsGranted = keywordsGranted ?? new List<Keyword>();
        }
    }

    /// <summary>
    /// 沉默事件
    /// </summary>
    [Serializable]
    public class SilenceEvent : GameEvent
    {
        public int targetInstanceId;

        public SilenceEvent() : base() { }

        public SilenceEvent(int sourcePlayerId, int targetInstanceId)
            : base(sourcePlayerId)
        {
            this.targetInstanceId = targetInstanceId;
        }
    }

    /// <summary>
    /// 回合开始事件
    /// </summary>
    [Serializable]
    public class TurnStartEvent : GameEvent
    {
        public int playerId;
        public int turnNumber;
        public int newMaxMana;

        public TurnStartEvent() : base() { }

        public TurnStartEvent(int playerId, int turnNumber, int newMaxMana)
            : base(playerId)
        {
            this.playerId = playerId;
            this.turnNumber = turnNumber;
            this.newMaxMana = newMaxMana;
        }
    }

    /// <summary>
    /// 回合结束事件
    /// </summary>
    [Serializable]
    public class TurnEndEvent : GameEvent
    {
        public int playerId;

        public TurnEndEvent() : base() { }

        public TurnEndEvent(int playerId)
            : base(playerId)
        {
            this.playerId = playerId;
        }
    }

    /// <summary>
    /// 游戏开始事件
    /// </summary>
    [Serializable]
    public class GameStartEvent : GameEvent
    {
        public int firstPlayerId;
        public int randomSeed;

        public GameStartEvent() : base() { }

        public GameStartEvent(int firstPlayerId, int randomSeed)
            : base(-1)
        {
            this.firstPlayerId = firstPlayerId;
            this.randomSeed = randomSeed;
        }
    }

    /// <summary>
    /// 游戏结束事件
    /// </summary>
    [Serializable]
    public class GameOverEvent : GameEvent
    {
        public int winnerId;
        public string reason;

        public GameOverEvent() : base() { }

        public GameOverEvent(int winnerId, string reason)
            : base(-1)
        {
            this.winnerId = winnerId;
            this.reason = reason;
        }
    }

    /// <summary>
    /// 疲劳事件
    /// </summary>
    [Serializable]
    public class FatigueEvent : GameEvent
    {
        public int playerId;
        public int damage;
        public int newFatigueCounter;

        public FatigueEvent() : base() { }

        public FatigueEvent(int playerId, int damage, int newFatigueCounter)
            : base(playerId)
        {
            this.playerId = playerId;
            this.damage = damage;
            this.newFatigueCounter = newFatigueCounter;
        }
    }

    /// <summary>
    /// 护符启动事件
    /// </summary>
    [Serializable]
    public class AmuletActivatedEvent : GameEvent
    {
        public int instanceId;
        public int cardId;
        public bool wasDestroyed;

        public AmuletActivatedEvent() : base() { }

        public AmuletActivatedEvent(int sourcePlayerId, int instanceId, int cardId, bool wasDestroyed)
            : base(sourcePlayerId)
        {
            this.instanceId = instanceId;
            this.cardId = cardId;
            this.wasDestroyed = wasDestroyed;
        }
    }

    /// <summary>
    /// 倒计时减少事件
    /// </summary>
    [Serializable]
    public class CountdownTickEvent : GameEvent
    {
        public int instanceId;
        public int cardId;
        public int newCountdown;

        public CountdownTickEvent() : base() { }

        public CountdownTickEvent(int sourcePlayerId, int instanceId, int cardId, int newCountdown)
            : base(sourcePlayerId)
        {
            this.instanceId = instanceId;
            this.cardId = cardId;
            this.newCountdown = newCountdown;
        }
    }

    /// <summary>
    /// 弃牌事件
    /// </summary>
    [Serializable]
    public class DiscardEvent : GameEvent
    {
        public int playerId;
        public int cardId;
        public int instanceId;

        public DiscardEvent() : base() { }

        public DiscardEvent(int playerId, int cardId, int instanceId)
            : base(playerId)
        {
            this.playerId = playerId;
            this.cardId = cardId;
            this.instanceId = instanceId;
        }
    }

    /// <summary>
    /// 获得关键词事件
    /// </summary>
    [Serializable]
    public class KeywordGainedEvent : GameEvent
    {
        public int targetInstanceId;
        public Keyword keyword;

        public KeywordGainedEvent() : base() { }

        public KeywordGainedEvent(int sourcePlayerId, int targetInstanceId, Keyword keyword)
            : base(sourcePlayerId)
        {
            this.targetInstanceId = targetInstanceId;
            this.keyword = keyword;
        }
    }

    /// <summary>
    /// 费用变化事件
    /// </summary>
    [Serializable]
    public class ManaChangeEvent : GameEvent
    {
        public int playerId;
        public int oldMana;
        public int newMana;
        public int oldMaxMana;
        public int newMaxMana;

        public ManaChangeEvent() : base() { }

        public ManaChangeEvent(int playerId, int oldMana, int newMana, int oldMaxMana, int newMaxMana)
            : base(playerId)
        {
            this.playerId = playerId;
            this.oldMana = oldMana;
            this.newMana = newMana;
            this.oldMaxMana = oldMaxMana;
            this.newMaxMana = newMaxMana;
        }
    }

    /// <summary>
    /// 获得屏障事件
    /// </summary>
    [Serializable]
    public class BarrierGainedEvent : GameEvent
    {
        public int targetId; // 随从instanceId 或 玩家ID
        public bool isPlayer; // 是否为玩家

        public BarrierGainedEvent() : base() { }

        public BarrierGainedEvent(int sourcePlayerId, int targetId, bool isPlayer)
            : base(sourcePlayerId)
        {
            this.targetId = targetId;
            this.isPlayer = isPlayer;
        }
    }

    /// <summary>
    /// 屏障消耗事件
    /// </summary>
    [Serializable]
    public class BarrierConsumedEvent : GameEvent
    {
        public int targetId;
        public bool isPlayer;
        public int damageBlocked;

        public BarrierConsumedEvent() : base() { }

        public BarrierConsumedEvent(int sourcePlayerId, int targetId, bool isPlayer, int damageBlocked)
            : base(sourcePlayerId)
        {
            this.targetId = targetId;
            this.isPlayer = isPlayer;
            this.damageBlocked = damageBlocked;
        }
    }

    /// <summary>
    /// 吸血回复事件
    /// </summary>
    [Serializable]
    public class DrainEvent : GameEvent
    {
        public int attackerInstanceId;
        public int damageDealt;
        public int healedAmount;

        public DrainEvent() : base() { }

        public DrainEvent(int sourcePlayerId, int attackerInstanceId, int damageDealt, int healedAmount)
            : base(sourcePlayerId)
        {
            this.attackerInstanceId = attackerInstanceId;
            this.damageDealt = damageDealt;
            this.healedAmount = healedAmount;
        }
    }
}
