using UnityEngine;
using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Rules;
using ShadowCardSmash.Network;
using ShadowCardSmash.Network.Messages;

/// <summary>
/// Phase 5 网络层测试
/// </summary>
public class NetworkTest : MonoBehaviour
{
    private LocalNetworkService _hostService;
    private LocalNetworkService _clientService;
    private NetworkManager _hostManager;
    private NetworkManager _clientManager;

    private int _messagesReceivedByHost = 0;
    private int _messagesReceivedByClient = 0;

    void Start()
    {
        Debug.Log("=== 网络层测试开始 ===");

        // 测试消息序列化
        TestMessageSerialization();

        // 测试本地网络服务
        TestLocalNetworkService();

        // 测试网络管理器
        TestNetworkManager();

        Debug.Log("=== 所有网络层测试完成 ===");
    }

    void Update()
    {
        // 更新网络服务
        _hostService?.Update();
        _clientService?.Update();
    }

    void TestMessageSerialization()
    {
        Debug.Log("--- 测试消息序列化 ---");

        // 测试连接消息
        var connectMsg = MessageSerializer.CreateConnectMessage("TestPlayer", "1.0.0");
        string json = MessageSerializer.SerializeMessage(connectMsg);
        var deserializedMsg = MessageSerializer.DeserializeMessage(json);

        Debug.Log($"连接消息序列化: type={deserializedMsg.messageType}, seq={deserializedMsg.sequence}");

        var payload = MessageSerializer.GetPayload<ConnectPayload>(deserializedMsg);
        Debug.Log($"连接载荷: playerName={payload.playerName}, version={payload.version}");

        // 测试玩家操作消息
        var action = PlayerAction.CreatePlayCard(0, 2, 3, 5);
        var actionMsg = MessageSerializer.CreateActionMessage(action);
        json = MessageSerializer.SerializeMessage(actionMsg);
        deserializedMsg = MessageSerializer.DeserializeMessage(json);

        var actionPayload = MessageSerializer.GetPayload<PlayerActionPayload>(deserializedMsg);
        Debug.Log($"操作载荷: playerId={actionPayload.playerId}, type={actionPayload.actionType}, handIndex={actionPayload.handIndex}");

        // 测试卡组消息
        var deck = DeckData.Create("测试卡组", HeroClass.ClassA);
        deck.cards.Add(new DeckEntry(1001, 3));
        deck.cards.Add(new DeckEntry(1002, 2));

        var deckMsg = MessageSerializer.CreateDeckSubmitMessage(deck);
        json = MessageSerializer.SerializeMessage(deckMsg);
        deserializedMsg = MessageSerializer.DeserializeMessage(json);

        var deckPayload = MessageSerializer.GetPayload<DeckSubmitPayload>(deserializedMsg);
        Debug.Log($"卡组载荷: name={deckPayload.deck.deckName}, class={deckPayload.deck.heroClass}, cards={deckPayload.deck.cards.Count}");

        // 测试Ping/Pong
        var pingMsg = MessageSerializer.CreatePingMessage();
        var pingPayload = MessageSerializer.GetPayload<PingPayload>(pingMsg);
        Debug.Log($"Ping载荷: sendTime={pingPayload.sendTime}");

        var pongMsg = MessageSerializer.CreatePongMessage(pingPayload.sendTime);
        var pongPayload = MessageSerializer.GetPayload<PongPayload>(pongMsg);
        Debug.Log($"Pong载荷: originalTime={pongPayload.originalSendTime}, serverTime={pongPayload.serverTime}");
    }

    void TestLocalNetworkService()
    {
        Debug.Log("--- 测试本地网络服务 ---");

        // 创建配对的网络服务
        var (host, client) = LocalNetworkTestHelper.CreatePair(0f); // 0延迟便于测试
        _hostService = host;
        _clientService = client;

        // 订阅事件
        _hostService.OnMessageReceived += (msg) =>
        {
            _messagesReceivedByHost++;
            Debug.Log($"Host收到消息: {msg.messageType}");
        };

        _hostService.OnClientConnected += (id) =>
        {
            Debug.Log($"Host: 客户端 {id} 已连接");
        };

        _clientService.OnMessageReceived += (msg) =>
        {
            _messagesReceivedByClient++;
            Debug.Log($"Client收到消息: {msg.messageType}");
        };

        _clientService.OnConnected += () =>
        {
            Debug.Log("Client: 已连接到服务器");
        };

        // 启动Host
        _hostService.StartHost(7777);
        Debug.Log($"Host状态: IsHost={_hostService.IsHost}, IsConnected={_hostService.IsConnected}");

        // 客户端连接
        _clientService.Connect("localhost", 7777);
        Debug.Log($"Client状态: IsHost={_clientService.IsHost}, IsConnected={_clientService.IsConnected}");

        // 客户端发送消息给Host
        var msg1 = MessageSerializer.CreateConnectMessage("Player2", "1.0.0");
        _clientService.Send(msg1);

        // Host广播消息
        var msg2 = MessageSerializer.CreateMessage(NetworkMessageType.Ready, new ReadyPayload(0, true));
        _hostService.Broadcast(msg2);

        // 更新以处理消息
        _hostService.Update();
        _clientService.Update();

        Debug.Log($"Host收到消息数: {_messagesReceivedByHost}");
        Debug.Log($"Client收到消息数: {_messagesReceivedByClient}");

        // 测试Ping延迟
        TestPingLatency();
    }

    void TestPingLatency()
    {
        Debug.Log("--- 测试Ping延迟 ---");

        long sendTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        _clientService.OnMessageReceived += (msg) =>
        {
            if (msg.messageType == NetworkMessageType.Pong)
            {
                var payload = MessageSerializer.GetPayload<PongPayload>(msg);
                long latency = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - payload.originalSendTime;
                Debug.Log($"Ping往返延迟: {latency}ms");
            }
        };

        // Host响应Ping
        _hostService.OnMessageReceived += (msg) =>
        {
            if (msg.messageType == NetworkMessageType.Ping)
            {
                var payload = MessageSerializer.GetPayload<PingPayload>(msg);
                var pongMsg = MessageSerializer.CreatePongMessage(payload.sendTime);
                _hostService.Send(pongMsg);
            }
        };

        var pingMsg = MessageSerializer.CreatePingMessage();
        _clientService.Send(pingMsg);

        _hostService.Update();
        _clientService.Update();
    }

    void TestNetworkManager()
    {
        Debug.Log("--- 测试网络管理器 ---");

        // 创建两个NetworkManager（模拟两个游戏实例）
        var hostGO = new GameObject("HostManager");
        _hostManager = hostGO.AddComponent<NetworkManager>();

        var clientGO = new GameObject("ClientManager");
        _clientManager = clientGO.AddComponent<NetworkManager>();

        // 创建新的网络服务对
        var (hostSvc, clientSvc) = LocalNetworkTestHelper.CreatePair(0f);

        _hostManager.SetNetworkService(hostSvc);
        _clientManager.SetNetworkService(clientSvc);

        // 订阅事件
        _hostManager.OnPlayerConnected += (id) => Debug.Log($"HostManager: 玩家 {id} 已连接");
        _hostManager.OnPlayerReadyChanged += (id, ready) => Debug.Log($"HostManager: 玩家 {id} 准备状态: {ready}");

        _clientManager.OnGameStart += (payload) =>
        {
            Debug.Log($"ClientManager: 游戏开始! 先手玩家={payload.firstPlayerId}, 种子={payload.randomSeed}");
        };

        _clientManager.OnActionResult += (result) =>
        {
            Debug.Log($"ClientManager: 操作结果 success={result.success}");
        };

        // Host启动游戏
        _hostManager.HostGame("HostPlayer", 7778);
        Debug.Log($"HostManager: IsHost={_hostManager.IsHost}, LocalPlayerId={_hostManager.LocalPlayerId}");

        // Client加入游戏
        _clientManager.JoinGame("ClientPlayer", "localhost", 7778);
        Debug.Log($"ClientManager: IsHost={_clientManager.IsHost}");

        // 更新
        hostSvc.Update();
        clientSvc.Update();

        // 测试提交卡组
        var deck = DeckData.Create("客户端卡组", HeroClass.ClassB);
        _clientManager.SubmitDeck(deck);

        hostSvc.Update();
        clientSvc.Update();

        // 测试准备状态
        _clientManager.SendReady(true);

        hostSvc.Update();
        clientSvc.Update();

        // 测试游戏开始广播
        var gameStartPayload = new GameStartPayload
        {
            randomSeed = 12345,
            firstPlayerId = 0
        };
        _hostManager.BroadcastGameStart(gameStartPayload);

        hostSvc.Update();
        clientSvc.Update();

        // 测试发送操作
        var action = PlayerAction.CreateEndTurn(1);
        _clientManager.SendAction(action);

        hostSvc.Update();
        clientSvc.Update();

        // 清理
        Destroy(hostGO, 1f);
        Destroy(clientGO, 1f);
    }

    void OnDestroy()
    {
        _hostService?.Disconnect();
        _clientService?.Disconnect();
    }
}
