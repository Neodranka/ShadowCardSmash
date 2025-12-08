using System;
using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;
using ShadowCardSmash.Core.Rules;

namespace ShadowCardSmash.Network.Messages
{
    /// <summary>
    /// 连接请求载荷
    /// </summary>
    [Serializable]
    public class ConnectPayload
    {
        public string playerName;
        public string version;

        public ConnectPayload() { }

        public ConnectPayload(string playerName, string version)
        {
            this.playerName = playerName;
            this.version = version;
        }
    }

    /// <summary>
    /// 连接响应载荷
    /// </summary>
    [Serializable]
    public class ConnectResponsePayload
    {
        public bool accepted;
        public int assignedPlayerId;
        public string rejectReason;

        public ConnectResponsePayload() { }

        public ConnectResponsePayload(bool accepted, int assignedPlayerId, string rejectReason = null)
        {
            this.accepted = accepted;
            this.assignedPlayerId = assignedPlayerId;
            this.rejectReason = rejectReason;
        }
    }

    /// <summary>
    /// 提交卡组载荷
    /// </summary>
    [Serializable]
    public class DeckSubmitPayload
    {
        public DeckData deck;

        public DeckSubmitPayload() { }

        public DeckSubmitPayload(DeckData deck)
        {
            this.deck = deck;
        }
    }

    /// <summary>
    /// 卡组验证结果载荷
    /// </summary>
    [Serializable]
    public class DeckValidationPayload
    {
        public bool accepted;
        public List<string> errors;

        public DeckValidationPayload()
        {
            errors = new List<string>();
        }

        public DeckValidationPayload(bool accepted, List<string> errors = null)
        {
            this.accepted = accepted;
            this.errors = errors ?? new List<string>();
        }
    }

    /// <summary>
    /// 游戏开始载荷
    /// </summary>
    [Serializable]
    public class GameStartPayload
    {
        public int randomSeed;
        public int firstPlayerId;
        public PlayerStatePayload player0State;
        public PlayerStatePayload player1State;

        public GameStartPayload() { }

        public GameStartPayload(int randomSeed, int firstPlayerId, PlayerState player0, PlayerState player1)
        {
            this.randomSeed = randomSeed;
            this.firstPlayerId = firstPlayerId;
            this.player0State = PlayerStatePayload.FromPlayerState(player0);
            this.player1State = PlayerStatePayload.FromPlayerState(player1);
        }
    }

    /// <summary>
    /// 玩家状态载荷（简化版，用于网络传输）
    /// </summary>
    [Serializable]
    public class PlayerStatePayload
    {
        public int playerId;
        public HeroClass heroClass;
        public int health;
        public int maxMana;
        public int evolutionPoints;
        public List<int> deckCardIds;
        public int compensationCardId;

        public PlayerStatePayload() { }

        public static PlayerStatePayload FromPlayerState(PlayerState state)
        {
            return new PlayerStatePayload
            {
                playerId = state.playerId,
                heroClass = state.heroClass,
                health = state.health,
                maxMana = state.maxMana,
                evolutionPoints = state.evolutionPoints,
                deckCardIds = new List<int>(state.deck),
                compensationCardId = state.compensationCardId
            };
        }
    }

    /// <summary>
    /// 玩家操作载荷
    /// </summary>
    [Serializable]
    public class PlayerActionPayload
    {
        public int playerId;
        public ActionType actionType;
        public int handIndex;
        public int tileIndex;
        public int sourceInstanceId;
        public int targetInstanceId;
        public bool targetIsPlayer;
        public int targetPlayerId;

        public PlayerActionPayload() { }

        public PlayerActionPayload(PlayerAction action)
        {
            playerId = action.playerId;
            actionType = action.actionType;
            handIndex = action.handIndex;
            tileIndex = action.tileIndex;
            sourceInstanceId = action.sourceInstanceId;
            targetInstanceId = action.targetInstanceId;
            targetIsPlayer = action.targetIsPlayer;
            targetPlayerId = action.targetPlayerId;
        }

        public PlayerAction ToPlayerAction()
        {
            return new PlayerAction
            {
                playerId = playerId,
                actionType = actionType,
                handIndex = handIndex,
                tileIndex = tileIndex,
                sourceInstanceId = sourceInstanceId,
                targetInstanceId = targetInstanceId,
                targetIsPlayer = targetIsPlayer,
                targetPlayerId = targetPlayerId
            };
        }
    }

    /// <summary>
    /// 操作结果载荷
    /// </summary>
    [Serializable]
    public class ActionResultPayload
    {
        public bool success;
        public string errorMessage;
        public List<GameEventPayload> events;

        public ActionResultPayload()
        {
            events = new List<GameEventPayload>();
        }

        public ActionResultPayload(bool success, string errorMessage = null)
        {
            this.success = success;
            this.errorMessage = errorMessage;
            this.events = new List<GameEventPayload>();
        }
    }

    /// <summary>
    /// 游戏事件载荷（用于网络传输）
    /// </summary>
    [Serializable]
    public class GameEventPayload
    {
        public string eventType;
        public string eventData;

        public GameEventPayload() { }

        public GameEventPayload(GameEvent evt)
        {
            eventType = evt.GetType().Name;
            eventData = UnityEngine.JsonUtility.ToJson(evt);
        }
    }

    /// <summary>
    /// 状态同步载荷
    /// </summary>
    [Serializable]
    public class StateSyncPayload
    {
        public string gameStateJson;
        public string stateHash;

        public StateSyncPayload() { }

        public StateSyncPayload(GameState state)
        {
            gameStateJson = UnityEngine.JsonUtility.ToJson(state);
            stateHash = ComputeHash(gameStateJson);
        }

        private string ComputeHash(string input)
        {
            // 简单哈希（生产环境应使用更安全的哈希）
            int hash = 0;
            foreach (char c in input)
            {
                hash = ((hash << 5) + hash) + c;
            }
            return hash.ToString("X8");
        }
    }

    /// <summary>
    /// Ping载荷
    /// </summary>
    [Serializable]
    public class PingPayload
    {
        public long sendTime;

        public PingPayload()
        {
            sendTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    /// <summary>
    /// Pong载荷
    /// </summary>
    [Serializable]
    public class PongPayload
    {
        public long originalSendTime;
        public long serverTime;

        public PongPayload() { }

        public PongPayload(long originalSendTime)
        {
            this.originalSendTime = originalSendTime;
            this.serverTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    /// <summary>
    /// 准备就绪载荷
    /// </summary>
    [Serializable]
    public class ReadyPayload
    {
        public int playerId;
        public bool isReady;

        public ReadyPayload() { }

        public ReadyPayload(int playerId, bool isReady)
        {
            this.playerId = playerId;
            this.isReady = isReady;
        }
    }

    /// <summary>
    /// 游戏结束载荷
    /// </summary>
    [Serializable]
    public class GameOverPayload
    {
        public int winnerId;
        public string reason;

        public GameOverPayload() { }

        public GameOverPayload(int winnerId, string reason)
        {
            this.winnerId = winnerId;
            this.reason = reason;
        }
    }
}
