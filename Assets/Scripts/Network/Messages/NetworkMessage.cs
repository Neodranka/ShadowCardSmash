using System;
using ShadowCardSmash.Core.Data;

namespace ShadowCardSmash.Network.Messages
{
    /// <summary>
    /// 网络消息基类
    /// </summary>
    [Serializable]
    public class NetworkMessage
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public NetworkMessageType messageType;

        /// <summary>
        /// 序列号（用于消息排序和确认）
        /// </summary>
        public int sequence;

        /// <summary>
        /// 时间戳
        /// </summary>
        public long timestamp;

        /// <summary>
        /// 载荷（JSON序列化的数据）
        /// </summary>
        public string payload;

        public NetworkMessage()
        {
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public NetworkMessage(NetworkMessageType type) : this()
        {
            messageType = type;
        }

        public NetworkMessage(NetworkMessageType type, string payload) : this(type)
        {
            this.payload = payload;
        }
    }
}
