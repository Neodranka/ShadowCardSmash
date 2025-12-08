using System;
using ShadowCardSmash.Network.Messages;

namespace ShadowCardSmash.Network
{
    /// <summary>
    /// 网络服务接口
    /// </summary>
    public interface INetworkService
    {
        /// <summary>
        /// 收到消息事件
        /// </summary>
        event Action<NetworkMessage> OnMessageReceived;

        /// <summary>
        /// 连接成功事件
        /// </summary>
        event Action OnConnected;

        /// <summary>
        /// 断开连接事件
        /// </summary>
        event Action<string> OnDisconnected;

        /// <summary>
        /// 错误事件
        /// </summary>
        event Action<string> OnError;

        /// <summary>
        /// 客户端连接事件（Host端）
        /// </summary>
        event Action<int> OnClientConnected;

        /// <summary>
        /// 客户端断开事件（Host端）
        /// </summary>
        event Action<int> OnClientDisconnected;

        /// <summary>
        /// 是否为Host
        /// </summary>
        bool IsHost { get; }

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 本地玩家ID
        /// </summary>
        int LocalPlayerId { get; }

        /// <summary>
        /// 启动Host（服务器+客户端）
        /// </summary>
        void StartHost(int port);

        /// <summary>
        /// 作为客户端连接
        /// </summary>
        void Connect(string ip, int port);

        /// <summary>
        /// 发送消息
        /// </summary>
        void Send(NetworkMessage message);

        /// <summary>
        /// 发送消息给指定玩家（Host用）
        /// </summary>
        void SendTo(int playerId, NetworkMessage message);

        /// <summary>
        /// 广播消息给所有客户端（Host用）
        /// </summary>
        void Broadcast(NetworkMessage message);

        /// <summary>
        /// 断开连接
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 更新（处理消息队列）
        /// </summary>
        void Update();
    }
}
