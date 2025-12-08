using System;
using System.Collections.Generic;
using UnityEngine;
using ShadowCardSmash.Network.Messages;

namespace ShadowCardSmash.Network
{
    /// <summary>
    /// 本地网络服务 - 用于单机双端测试
    /// 模拟两个玩家在同一台机器上的网络通信
    /// </summary>
    public class LocalNetworkService : INetworkService
    {
        // 事件
        public event Action<NetworkMessage> OnMessageReceived;
        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<string> OnError;
        public event Action<int> OnClientConnected;
        public event Action<int> OnClientDisconnected;

        // 状态
        private bool _isHost;
        private bool _isConnected;
        private int _localPlayerId;

        // 消息队列（模拟网络传输）
        private Queue<DelayedMessage> _pendingMessages;
        private float _simulatedLatency; // 模拟延迟（秒）

        // 配对的另一端
        private LocalNetworkService _otherEnd;

        public bool IsHost => _isHost;
        public bool IsConnected => _isConnected;
        public int LocalPlayerId => _localPlayerId;

        public LocalNetworkService(float simulatedLatency = 0.05f)
        {
            _pendingMessages = new Queue<DelayedMessage>();
            _simulatedLatency = simulatedLatency;
        }

        /// <summary>
        /// 设置配对的另一端（用于本地测试）
        /// </summary>
        public void SetOtherEnd(LocalNetworkService otherEnd)
        {
            _otherEnd = otherEnd;
        }

        public void StartHost(int port)
        {
            _isHost = true;
            _localPlayerId = 0;
            _isConnected = true;

            Debug.Log($"LocalNetworkService: Started host on port {port}");
            OnConnected?.Invoke();
        }

        public void Connect(string ip, int port)
        {
            _isHost = false;
            _localPlayerId = 1;
            _isConnected = true;

            Debug.Log($"LocalNetworkService: Connected to {ip}:{port}");
            OnConnected?.Invoke();

            // 通知Host有客户端连接
            _otherEnd?.SimulateClientConnected(1);
        }

        public void Send(NetworkMessage message)
        {
            if (!_isConnected)
            {
                OnError?.Invoke("Not connected");
                return;
            }

            // 发送到另一端
            _otherEnd?.ReceiveMessage(message);
        }

        public void SendTo(int playerId, NetworkMessage message)
        {
            if (!_isHost)
            {
                OnError?.Invoke("Only host can send to specific player");
                return;
            }

            // 在本地测试中，只有一个客户端
            if (playerId == 1)
            {
                _otherEnd?.ReceiveMessage(message);
            }
            else if (playerId == 0)
            {
                // 发给自己
                ReceiveMessage(message);
            }
        }

        public void Broadcast(NetworkMessage message)
        {
            if (!_isHost)
            {
                OnError?.Invoke("Only host can broadcast");
                return;
            }

            // 发给所有人（包括自己）
            ReceiveMessage(message);
            _otherEnd?.ReceiveMessage(message);
        }

        public void Disconnect()
        {
            if (_isConnected)
            {
                _isConnected = false;

                if (_isHost)
                {
                    _otherEnd?.SimulateDisconnected("Host disconnected");
                }
                else
                {
                    _otherEnd?.SimulateClientDisconnected(1);
                }

                OnDisconnected?.Invoke("Disconnected");
            }
        }

        public void Update()
        {
            // 处理延迟消息
            float currentTime = Time.time;

            while (_pendingMessages.Count > 0)
            {
                var delayed = _pendingMessages.Peek();
                if (currentTime >= delayed.deliveryTime)
                {
                    _pendingMessages.Dequeue();
                    OnMessageReceived?.Invoke(delayed.message);
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 接收消息（从另一端调用）
        /// </summary>
        private void ReceiveMessage(NetworkMessage message)
        {
            if (!_isConnected) return;

            // 添加到延迟队列
            var delayed = new DelayedMessage
            {
                message = message,
                deliveryTime = Time.time + _simulatedLatency
            };

            _pendingMessages.Enqueue(delayed);
        }

        /// <summary>
        /// 模拟客户端连接（从另一端调用）
        /// </summary>
        private void SimulateClientConnected(int clientId)
        {
            OnClientConnected?.Invoke(clientId);
        }

        /// <summary>
        /// 模拟客户端断开（从另一端调用）
        /// </summary>
        private void SimulateClientDisconnected(int clientId)
        {
            OnClientDisconnected?.Invoke(clientId);
        }

        /// <summary>
        /// 模拟断开连接（从另一端调用）
        /// </summary>
        private void SimulateDisconnected(string reason)
        {
            _isConnected = false;
            OnDisconnected?.Invoke(reason);
        }

        /// <summary>
        /// 设置模拟延迟
        /// </summary>
        public void SetSimulatedLatency(float latency)
        {
            _simulatedLatency = latency;
        }

        /// <summary>
        /// 延迟消息结构
        /// </summary>
        private struct DelayedMessage
        {
            public NetworkMessage message;
            public float deliveryTime;
        }
    }

    /// <summary>
    /// 本地网络测试助手 - 创建配对的本地网络服务
    /// </summary>
    public static class LocalNetworkTestHelper
    {
        /// <summary>
        /// 创建一对配对的本地网络服务
        /// </summary>
        public static (LocalNetworkService host, LocalNetworkService client) CreatePair(float latency = 0.05f)
        {
            var host = new LocalNetworkService(latency);
            var client = new LocalNetworkService(latency);

            host.SetOtherEnd(client);
            client.SetOtherEnd(host);

            return (host, client);
        }
    }
}
