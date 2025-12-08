using System;
using System.Collections.Generic;
using UnityEngine;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;
using ShadowCardSmash.Core.Rules;

namespace ShadowCardSmash.Network.Messages
{
    /// <summary>
    /// 消息序列化器
    /// </summary>
    public static class MessageSerializer
    {
        private static int _sequenceCounter = 0;

        /// <summary>
        /// 序列化对象为JSON
        /// </summary>
        public static string Serialize<T>(T obj)
        {
            return JsonUtility.ToJson(obj);
        }

        /// <summary>
        /// 反序列化JSON为对象
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }

        /// <summary>
        /// 创建网络消息
        /// </summary>
        public static NetworkMessage CreateMessage(NetworkMessageType type, object payload = null)
        {
            var message = new NetworkMessage(type)
            {
                sequence = _sequenceCounter++
            };

            if (payload != null)
            {
                message.payload = JsonUtility.ToJson(payload);
            }

            return message;
        }

        /// <summary>
        /// 序列化网络消息为JSON字符串
        /// </summary>
        public static string SerializeMessage(NetworkMessage message)
        {
            return JsonUtility.ToJson(message);
        }

        /// <summary>
        /// 反序列化JSON字符串为网络消息
        /// </summary>
        public static NetworkMessage DeserializeMessage(string json)
        {
            return JsonUtility.FromJson<NetworkMessage>(json);
        }

        /// <summary>
        /// 从消息中提取载荷
        /// </summary>
        public static T GetPayload<T>(NetworkMessage message)
        {
            if (string.IsNullOrEmpty(message.payload))
            {
                return default(T);
            }
            return JsonUtility.FromJson<T>(message.payload);
        }

        /// <summary>
        /// 序列化游戏事件列表
        /// </summary>
        public static List<GameEventPayload> SerializeEvents(List<GameEvent> events)
        {
            var payloads = new List<GameEventPayload>();

            foreach (var evt in events)
            {
                payloads.Add(new GameEventPayload(evt));
            }

            return payloads;
        }

        /// <summary>
        /// 反序列化游戏事件
        /// </summary>
        public static GameEvent DeserializeEvent(GameEventPayload payload)
        {
            // 根据事件类型反序列化
            switch (payload.eventType)
            {
                case nameof(CardDrawnEvent):
                    return JsonUtility.FromJson<CardDrawnEvent>(payload.eventData);
                case nameof(CardPlayedEvent):
                    return JsonUtility.FromJson<CardPlayedEvent>(payload.eventData);
                case nameof(DamageEvent):
                    return JsonUtility.FromJson<DamageEvent>(payload.eventData);
                case nameof(HealEvent):
                    return JsonUtility.FromJson<HealEvent>(payload.eventData);
                case nameof(UnitDestroyedEvent):
                    return JsonUtility.FromJson<UnitDestroyedEvent>(payload.eventData);
                case nameof(SummonEvent):
                    return JsonUtility.FromJson<SummonEvent>(payload.eventData);
                case nameof(AttackEvent):
                    return JsonUtility.FromJson<AttackEvent>(payload.eventData);
                case nameof(EvolveEvent):
                    return JsonUtility.FromJson<EvolveEvent>(payload.eventData);
                case nameof(BuffEvent):
                    return JsonUtility.FromJson<BuffEvent>(payload.eventData);
                case nameof(SilenceEvent):
                    return JsonUtility.FromJson<SilenceEvent>(payload.eventData);
                case nameof(TurnStartEvent):
                    return JsonUtility.FromJson<TurnStartEvent>(payload.eventData);
                case nameof(TurnEndEvent):
                    return JsonUtility.FromJson<TurnEndEvent>(payload.eventData);
                case nameof(GameStartEvent):
                    return JsonUtility.FromJson<GameStartEvent>(payload.eventData);
                case nameof(GameOverEvent):
                    return JsonUtility.FromJson<GameOverEvent>(payload.eventData);
                case nameof(FatigueEvent):
                    return JsonUtility.FromJson<FatigueEvent>(payload.eventData);
                case nameof(AmuletActivatedEvent):
                    return JsonUtility.FromJson<AmuletActivatedEvent>(payload.eventData);
                case nameof(CountdownTickEvent):
                    return JsonUtility.FromJson<CountdownTickEvent>(payload.eventData);
                case nameof(DiscardEvent):
                    return JsonUtility.FromJson<DiscardEvent>(payload.eventData);
                case nameof(KeywordGainedEvent):
                    return JsonUtility.FromJson<KeywordGainedEvent>(payload.eventData);
                case nameof(ManaChangeEvent):
                    return JsonUtility.FromJson<ManaChangeEvent>(payload.eventData);
                default:
                    Debug.LogWarning($"Unknown event type: {payload.eventType}");
                    return null;
            }
        }

        /// <summary>
        /// 反序列化所有游戏事件
        /// </summary>
        public static List<GameEvent> DeserializeEvents(List<GameEventPayload> payloads)
        {
            var events = new List<GameEvent>();

            foreach (var payload in payloads)
            {
                var evt = DeserializeEvent(payload);
                if (evt != null)
                {
                    events.Add(evt);
                }
            }

            return events;
        }

        /// <summary>
        /// 重置序列号计数器
        /// </summary>
        public static void ResetSequenceCounter()
        {
            _sequenceCounter = 0;
        }

        #region 便捷创建方法

        public static NetworkMessage CreateConnectMessage(string playerName, string version)
        {
            return CreateMessage(NetworkMessageType.Connect, new ConnectPayload(playerName, version));
        }

        public static NetworkMessage CreateDeckSubmitMessage(DeckData deck)
        {
            return CreateMessage(NetworkMessageType.DeckSubmit, new DeckSubmitPayload(deck));
        }

        public static NetworkMessage CreateReadyMessage(int playerId, bool isReady)
        {
            return CreateMessage(NetworkMessageType.Ready, new ReadyPayload(playerId, isReady));
        }

        public static NetworkMessage CreateActionMessage(PlayerAction action)
        {
            return CreateMessage(NetworkMessageType.PlayerAction, new PlayerActionPayload(action));
        }

        public static NetworkMessage CreatePingMessage()
        {
            return CreateMessage(NetworkMessageType.Ping, new PingPayload());
        }

        public static NetworkMessage CreatePongMessage(long originalSendTime)
        {
            return CreateMessage(NetworkMessageType.Pong, new PongPayload(originalSendTime));
        }

        public static NetworkMessage CreateSurrenderMessage()
        {
            return CreateMessage(NetworkMessageType.Surrender);
        }

        #endregion
    }
}
