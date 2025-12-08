using System;
using System.Collections.Generic;
using UnityEngine;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Rules;
using ShadowCardSmash.Network.Messages;

namespace ShadowCardSmash.Network
{
    /// <summary>
    /// 网络管理器 - 处理网络通信
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        private INetworkService _networkService;
        private Queue<NetworkMessage> _messageQueue;

        // 状态
        private bool _isHost;
        private bool _isConnected;
        private int _localPlayerId;
        private string _playerName;

        // 玩家状态
        private bool[] _playerReady = new bool[2];
        private DeckData[] _playerDecks = new DeckData[2];

        // 公开属性
        public bool IsHost => _isHost;
        public bool IsConnected => _isConnected;
        public int LocalPlayerId => _localPlayerId;
        public bool AllPlayersReady => _playerReady[0] && _playerReady[1];

        // 事件
        public event Action<int> OnPlayerConnected;
        public event Action<int> OnPlayerDisconnected;
        public event Action<int, bool> OnPlayerReadyChanged;
        public event Action<GameStartPayload> OnGameStart;
        public event Action<ActionResultPayload> OnActionResult;
        public event Action<StateSyncPayload> OnStateSync;
        public event Action<string> OnError;
        public event Action OnDisconnected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _messageQueue = new Queue<NetworkMessage>();
        }

        private void Update()
        {
            _networkService?.Update();
            ProcessMessageQueue();
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        /// <summary>
        /// 设置网络服务
        /// </summary>
        public void SetNetworkService(INetworkService service)
        {
            // 确保消息队列已初始化
            if (_messageQueue == null)
            {
                _messageQueue = new Queue<NetworkMessage>();
            }

            if (_networkService != null)
            {
                UnsubscribeEvents();
            }

            _networkService = service;
            SubscribeEvents();
        }

        /// <summary>
        /// 作为Host启动游戏
        /// </summary>
        public void HostGame(string playerName, int port = 7777)
        {
            _playerName = playerName;
            _isHost = true;
            _localPlayerId = 0;

            _playerReady[0] = false;
            _playerReady[1] = false;
            _playerDecks[0] = null;
            _playerDecks[1] = null;

            _networkService.StartHost(port);
            Debug.Log($"NetworkManager: Hosting game on port {port}");
        }

        /// <summary>
        /// 作为Client加入游戏
        /// </summary>
        public void JoinGame(string playerName, string ip, int port = 7777)
        {
            _playerName = playerName;
            _isHost = false;
            _localPlayerId = -1; // 等待服务器分配

            _networkService.Connect(ip, port);
            Debug.Log($"NetworkManager: Connecting to {ip}:{port}");
        }

        /// <summary>
        /// 提交卡组
        /// </summary>
        public void SubmitDeck(DeckData deck)
        {
            var message = MessageSerializer.CreateDeckSubmitMessage(deck);
            _networkService.Send(message);
            Debug.Log($"NetworkManager: Submitted deck '{deck.deckName}'");
        }

        /// <summary>
        /// 发送准备状态
        /// </summary>
        public void SendReady(bool isReady = true)
        {
            var message = MessageSerializer.CreateReadyMessage(_localPlayerId, isReady);
            _networkService.Send(message);
            Debug.Log($"NetworkManager: Sent ready state: {isReady}");
        }

        /// <summary>
        /// 发送玩家操作
        /// </summary>
        public void SendAction(PlayerAction action)
        {
            var message = MessageSerializer.CreateActionMessage(action);
            _networkService.Send(message);
            Debug.Log($"NetworkManager: Sent action: {action.actionType}");
        }

        /// <summary>
        /// 发送Ping
        /// </summary>
        public void SendPing()
        {
            var message = MessageSerializer.CreatePingMessage();
            _networkService.Send(message);
        }

        /// <summary>
        /// 发送投降
        /// </summary>
        public void SendSurrender()
        {
            var message = MessageSerializer.CreateSurrenderMessage();
            _networkService.Send(message);
            Debug.Log("NetworkManager: Sent surrender");
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            _networkService?.Disconnect();
            _isConnected = false;
            _isHost = false;
            _localPlayerId = -1;
            Debug.Log("NetworkManager: Disconnected");
        }

        #region Host Methods

        /// <summary>
        /// 广播游戏开始（Host用）
        /// </summary>
        public void BroadcastGameStart(GameStartPayload payload)
        {
            if (!_isHost) return;

            var message = MessageSerializer.CreateMessage(NetworkMessageType.GameStart, payload);
            _networkService.Broadcast(message);
            Debug.Log("NetworkManager: Broadcasted game start");
        }

        /// <summary>
        /// 发送操作结果（Host用）
        /// </summary>
        public void SendActionResult(int playerId, ActionResultPayload result)
        {
            if (!_isHost) return;

            var message = MessageSerializer.CreateMessage(NetworkMessageType.ActionResult, result);
            _networkService.Broadcast(message); // 广播给所有玩家
            Debug.Log($"NetworkManager: Sent action result to all players");
        }

        /// <summary>
        /// 发送状态同步（Host用）
        /// </summary>
        public void SendStateSync(GameState state)
        {
            if (!_isHost) return;

            var payload = new StateSyncPayload(state);
            var message = MessageSerializer.CreateMessage(NetworkMessageType.StateSync, payload);
            _networkService.Broadcast(message);
        }

        #endregion

        #region Event Handlers

        private void SubscribeEvents()
        {
            _networkService.OnMessageReceived += HandleMessage;
            _networkService.OnConnected += HandleConnected;
            _networkService.OnDisconnected += HandleDisconnected;
            _networkService.OnError += HandleError;
            _networkService.OnClientConnected += HandleClientConnected;
            _networkService.OnClientDisconnected += HandleClientDisconnected;
        }

        private void UnsubscribeEvents()
        {
            _networkService.OnMessageReceived -= HandleMessage;
            _networkService.OnConnected -= HandleConnected;
            _networkService.OnDisconnected -= HandleDisconnected;
            _networkService.OnError -= HandleError;
            _networkService.OnClientConnected -= HandleClientConnected;
            _networkService.OnClientDisconnected -= HandleClientDisconnected;
        }

        private void HandleMessage(NetworkMessage message)
        {
            _messageQueue.Enqueue(message);
        }

        private void HandleConnected()
        {
            _isConnected = true;
            Debug.Log("NetworkManager: Connected");

            // 如果是客户端，发送连接请求
            if (!_isHost)
            {
                var connectMsg = MessageSerializer.CreateConnectMessage(_playerName, Application.version);
                _networkService.Send(connectMsg);
            }
        }

        private void HandleDisconnected(string reason)
        {
            _isConnected = false;
            Debug.Log($"NetworkManager: Disconnected - {reason}");
            OnDisconnected?.Invoke();
        }

        private void HandleError(string error)
        {
            Debug.LogError($"NetworkManager: Error - {error}");
            OnError?.Invoke(error);
        }

        private void HandleClientConnected(int clientId)
        {
            Debug.Log($"NetworkManager: Client {clientId} connected");
            OnPlayerConnected?.Invoke(clientId);
        }

        private void HandleClientDisconnected(int clientId)
        {
            Debug.Log($"NetworkManager: Client {clientId} disconnected");
            OnPlayerDisconnected?.Invoke(clientId);
        }

        #endregion

        #region Message Processing

        private void ProcessMessageQueue()
        {
            while (_messageQueue.Count > 0)
            {
                var message = _messageQueue.Dequeue();
                ProcessMessage(message);
            }
        }

        private void ProcessMessage(NetworkMessage message)
        {
            switch (message.messageType)
            {
                case NetworkMessageType.Connect:
                    HandleConnectMessage(message);
                    break;

                case NetworkMessageType.DeckSubmit:
                    HandleDeckSubmitMessage(message);
                    break;

                case NetworkMessageType.DeckAccepted:
                case NetworkMessageType.DeckRejected:
                    HandleDeckValidationMessage(message);
                    break;

                case NetworkMessageType.Ready:
                    HandleReadyMessage(message);
                    break;

                case NetworkMessageType.GameStart:
                    HandleGameStartMessage(message);
                    break;

                case NetworkMessageType.PlayerAction:
                    HandlePlayerActionMessage(message);
                    break;

                case NetworkMessageType.ActionResult:
                    HandleActionResultMessage(message);
                    break;

                case NetworkMessageType.StateSync:
                    HandleStateSyncMessage(message);
                    break;

                case NetworkMessageType.Ping:
                    HandlePingMessage(message);
                    break;

                case NetworkMessageType.Pong:
                    HandlePongMessage(message);
                    break;

                case NetworkMessageType.Surrender:
                    HandleSurrenderMessage(message);
                    break;
            }
        }

        private void HandleConnectMessage(NetworkMessage message)
        {
            if (!_isHost) return;

            var payload = MessageSerializer.GetPayload<ConnectPayload>(message);
            Debug.Log($"NetworkManager: Player '{payload.playerName}' connecting (version: {payload.version})");

            // 分配玩家ID (1 = 客户端)
            var response = new ConnectResponsePayload(true, 1);
            var responseMsg = MessageSerializer.CreateMessage(NetworkMessageType.Connect, response);
            _networkService.SendTo(1, responseMsg);
        }

        private void HandleDeckSubmitMessage(NetworkMessage message)
        {
            var payload = MessageSerializer.GetPayload<DeckSubmitPayload>(message);
            Debug.Log($"NetworkManager: Received deck submission");

            // Host验证并存储卡组
            if (_isHost)
            {
                // TODO: 验证卡组
                // 暂时直接接受
                var response = new DeckValidationPayload(true);
                var responseMsg = MessageSerializer.CreateMessage(NetworkMessageType.DeckAccepted, response);
                _networkService.Broadcast(responseMsg);
            }
        }

        private void HandleDeckValidationMessage(NetworkMessage message)
        {
            var payload = MessageSerializer.GetPayload<DeckValidationPayload>(message);
            Debug.Log($"NetworkManager: Deck validation result: {payload.accepted}");
        }

        private void HandleReadyMessage(NetworkMessage message)
        {
            var payload = MessageSerializer.GetPayload<ReadyPayload>(message);

            if (payload.playerId >= 0 && payload.playerId < 2)
            {
                _playerReady[payload.playerId] = payload.isReady;
                OnPlayerReadyChanged?.Invoke(payload.playerId, payload.isReady);
                Debug.Log($"NetworkManager: Player {payload.playerId} ready: {payload.isReady}");
            }
        }

        private void HandleGameStartMessage(NetworkMessage message)
        {
            var payload = MessageSerializer.GetPayload<GameStartPayload>(message);
            Debug.Log($"NetworkManager: Game starting, first player: {payload.firstPlayerId}");
            OnGameStart?.Invoke(payload);
        }

        private void HandlePlayerActionMessage(NetworkMessage message)
        {
            // Host处理玩家操作
            if (!_isHost) return;

            var payload = MessageSerializer.GetPayload<PlayerActionPayload>(message);
            Debug.Log($"NetworkManager: Received action from player {payload.playerId}: {payload.actionType}");

            // TODO: 在GameController中处理
        }

        private void HandleActionResultMessage(NetworkMessage message)
        {
            var payload = MessageSerializer.GetPayload<ActionResultPayload>(message);
            Debug.Log($"NetworkManager: Action result: {payload.success}");
            OnActionResult?.Invoke(payload);
        }

        private void HandleStateSyncMessage(NetworkMessage message)
        {
            var payload = MessageSerializer.GetPayload<StateSyncPayload>(message);
            Debug.Log($"NetworkManager: State sync received, hash: {payload.stateHash}");
            OnStateSync?.Invoke(payload);
        }

        private void HandlePingMessage(NetworkMessage message)
        {
            var payload = MessageSerializer.GetPayload<PingPayload>(message);
            var pongMsg = MessageSerializer.CreatePongMessage(payload.sendTime);
            _networkService.Send(pongMsg);
        }

        private void HandlePongMessage(NetworkMessage message)
        {
            var payload = MessageSerializer.GetPayload<PongPayload>(message);
            long latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - payload.originalSendTime;
            Debug.Log($"NetworkManager: Ping latency: {latency}ms");
        }

        private void HandleSurrenderMessage(NetworkMessage message)
        {
            Debug.Log("NetworkManager: Opponent surrendered");
            // TODO: 处理投降
        }

        #endregion
    }
}
