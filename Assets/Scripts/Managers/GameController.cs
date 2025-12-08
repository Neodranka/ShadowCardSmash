using System;
using System.Collections.Generic;
using UnityEngine;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Effects;
using ShadowCardSmash.Core.Events;
using ShadowCardSmash.Core.Rules;
using ShadowCardSmash.Network;
using ShadowCardSmash.Network.Messages;

namespace ShadowCardSmash.Managers
{
    /// <summary>
    /// 游戏控制器 - 管理游戏流程和状态
    /// </summary>
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        // 依赖
        private GameRuleEngine _ruleEngine;
        private NetworkManager _networkManager;
        private ICardDatabase _cardDatabase;

        // 状态
        private GameState _currentState;
        private int _localPlayerId;
        private bool _isMyTurn;
        private bool _waitingForResponse;
        private bool _isGameStarted;
        private bool _isGameOver;

        // 公开属性
        public GameState CurrentState => _currentState;
        public int LocalPlayerId => _localPlayerId;
        public bool IsMyTurn => _isMyTurn;
        public bool IsGameStarted => _isGameStarted;
        public bool IsGameOver => _isGameOver;
        public bool WaitingForResponse => _waitingForResponse;

        // 事件
        public event Action<GameEvent> OnGameEvent;
        public event Action OnStateChanged;
        public event Action<int> OnTurnChanged;
        public event Action<int, string> OnGameOver;
        public event Action OnGameStarted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            UnsubscribeNetworkEvents();
        }

        #region Initialization

        /// <summary>
        /// 初始化游戏控制器
        /// </summary>
        public void Initialize(ICardDatabase cardDatabase, NetworkManager networkManager = null)
        {
            _cardDatabase = cardDatabase;
            _networkManager = networkManager;

            if (_networkManager != null)
            {
                SubscribeNetworkEvents();
            }

            Debug.Log("GameController: Initialized");
        }

        /// <summary>
        /// 初始化单机游戏（本地双人或AI）
        /// </summary>
        public void InitializeLocalGame(DeckData player0Deck, DeckData player1Deck, int randomSeed = -1)
        {
            if (randomSeed < 0)
            {
                randomSeed = UnityEngine.Random.Range(0, int.MaxValue);
            }

            // 创建规则引擎
            _ruleEngine = new GameRuleEngine(_cardDatabase, randomSeed);

            // 创建玩家状态
            var player0 = PlayerState.CreateInitial(0, player0Deck.heroClass, player0Deck.ToCardIdList());
            var player1 = PlayerState.CreateInitial(1, player1Deck.heroClass, player1Deck.ToCardIdList(),
                player1Deck.compensationCardId);

            // 创建游戏状态
            _currentState = GameState.CreateInitial(player0, player1, randomSeed);
            _ruleEngine.Initialize(_currentState);

            _localPlayerId = 0; // 本地游戏默认控制玩家0
            _isGameStarted = false;
            _isGameOver = false;

            Debug.Log($"GameController: Local game initialized with seed {randomSeed}");
        }

        /// <summary>
        /// 从网络载荷初始化游戏
        /// </summary>
        public void InitializeFromNetwork(GameStartPayload payload, int localPlayerId)
        {
            _localPlayerId = localPlayerId;

            // 重建玩家状态
            var player0 = RebuildPlayerState(payload.player0State);
            var player1 = RebuildPlayerState(payload.player1State);

            // 创建规则引擎
            _ruleEngine = new GameRuleEngine(_cardDatabase, payload.randomSeed);

            // 创建游戏状态
            _currentState = GameState.CreateInitial(player0, player1, payload.randomSeed);
            _ruleEngine.Initialize(_currentState);

            _isGameStarted = false;
            _isGameOver = false;

            Debug.Log($"GameController: Network game initialized, local player: {localPlayerId}");
        }

        private PlayerState RebuildPlayerState(PlayerStatePayload payload)
        {
            return PlayerState.CreateInitial(
                payload.playerId,
                payload.heroClass,
                payload.deckCardIds,
                payload.compensationCardId
            );
        }

        #endregion

        #region Game Flow

        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            if (_isGameStarted)
            {
                Debug.LogWarning("GameController: Game already started");
                return;
            }

            var events = _ruleEngine.StartGame();
            _isGameStarted = true;
            _currentState = _ruleEngine.CurrentState;

            UpdateTurnState();
            ProcessEvents(events);

            OnGameStarted?.Invoke();
            OnStateChanged?.Invoke();

            Debug.Log($"GameController: Game started, turn {_currentState.turnNumber}, player {_currentState.currentPlayerId}");
        }

        /// <summary>
        /// 更新回合状态
        /// </summary>
        private void UpdateTurnState()
        {
            _isMyTurn = _currentState.currentPlayerId == _localPlayerId;
        }

        #endregion

        #region Player Actions

        /// <summary>
        /// 尝试使用卡牌
        /// </summary>
        public bool TryPlayCard(int handIndex, int tileIndex, int targetInstanceId = -1,
            bool targetIsPlayer = false, int targetPlayerId = -1)
        {
            if (!CanPerformAction()) return false;

            var action = PlayerAction.CreatePlayCard(
                _localPlayerId, handIndex, tileIndex,
                targetInstanceId, targetIsPlayer, targetPlayerId);

            return ExecuteAction(action);
        }

        /// <summary>
        /// 尝试攻击
        /// </summary>
        public bool TryAttack(int attackerInstanceId, int targetInstanceId = -1,
            bool targetIsPlayer = false, int targetPlayerId = -1)
        {
            if (!CanPerformAction()) return false;

            var action = PlayerAction.CreateAttack(
                _localPlayerId, attackerInstanceId,
                targetInstanceId, targetIsPlayer, targetPlayerId);

            return ExecuteAction(action);
        }

        /// <summary>
        /// 尝试进化
        /// </summary>
        public bool TryEvolve(int instanceId)
        {
            if (!CanPerformAction()) return false;

            var action = PlayerAction.CreateEvolve(_localPlayerId, instanceId);
            return ExecuteAction(action);
        }

        /// <summary>
        /// 尝试启动护符
        /// </summary>
        public bool TryActivateAmulet(int instanceId, int targetInstanceId = -1,
            bool targetIsPlayer = false, int targetPlayerId = -1)
        {
            if (!CanPerformAction()) return false;

            var action = PlayerAction.CreateActivateAmulet(
                _localPlayerId, instanceId,
                targetInstanceId, targetIsPlayer, targetPlayerId);

            return ExecuteAction(action);
        }

        /// <summary>
        /// 尝试结束回合
        /// </summary>
        public bool TryEndTurn()
        {
            if (!CanPerformAction()) return false;

            var action = PlayerAction.CreateEndTurn(_localPlayerId);
            return ExecuteAction(action);
        }

        /// <summary>
        /// 投降
        /// </summary>
        public void Surrender()
        {
            var action = PlayerAction.CreateSurrender(_localPlayerId);

            if (_networkManager != null && _networkManager.IsConnected)
            {
                _networkManager.SendSurrender();
            }

            ExecuteAction(action);
        }

        /// <summary>
        /// 检查是否可以执行操作
        /// </summary>
        private bool CanPerformAction()
        {
            if (!_isGameStarted)
            {
                Debug.LogWarning("GameController: Game not started");
                return false;
            }

            if (_isGameOver)
            {
                Debug.LogWarning("GameController: Game is over");
                return false;
            }

            if (!_isMyTurn)
            {
                Debug.LogWarning("GameController: Not your turn");
                return false;
            }

            if (_waitingForResponse)
            {
                Debug.LogWarning("GameController: Waiting for server response");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 执行操作
        /// </summary>
        private bool ExecuteAction(PlayerAction action)
        {
            // 验证操作
            if (!_ruleEngine.ValidateAction(action))
            {
                Debug.LogWarning($"GameController: Invalid action {action.actionType}");
                return false;
            }

            // 如果是网络游戏，发送到服务器
            if (_networkManager != null && _networkManager.IsConnected && !_networkManager.IsHost)
            {
                _networkManager.SendAction(action);
                _waitingForResponse = true;
                return true;
            }

            // 本地执行
            var events = _ruleEngine.ProcessAction(action);
            _currentState = _ruleEngine.CurrentState;

            ProcessEvents(events);
            CheckGameOver();
            UpdateTurnState();

            OnStateChanged?.Invoke();

            // 如果是Host，广播结果
            if (_networkManager != null && _networkManager.IsHost)
            {
                var result = new ActionResultPayload(true);
                result.events = MessageSerializer.SerializeEvents(events);
                _networkManager.SendActionResult(_localPlayerId, result);
            }

            return true;
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// 获取可使用的手牌索引
        /// </summary>
        public List<int> GetPlayableCardIndices()
        {
            if (_ruleEngine == null) return new List<int>();
            return _ruleEngine.GetPlayableCards(_localPlayerId);
        }

        /// <summary>
        /// 获取卡牌的有效目标
        /// </summary>
        public List<RuntimeCard> GetValidTargetsForCard(int handIndex)
        {
            if (_ruleEngine == null) return new List<RuntimeCard>();
            return _ruleEngine.GetValidTargets(_localPlayerId, handIndex);
        }

        /// <summary>
        /// 获取有效攻击目标
        /// </summary>
        public List<AttackTarget> GetValidAttackTargets(int attackerInstanceId)
        {
            if (_ruleEngine == null) return new List<AttackTarget>();
            return _ruleEngine.GetValidAttackTargets(attackerInstanceId);
        }

        /// <summary>
        /// 检查是否可以进化
        /// </summary>
        public bool CanEvolve(int instanceId)
        {
            if (_ruleEngine == null) return false;
            return _ruleEngine.CanEvolve(_localPlayerId, instanceId);
        }

        /// <summary>
        /// 获取可进化的随从
        /// </summary>
        public List<RuntimeCard> GetEvolvableMinions()
        {
            if (_ruleEngine == null) return new List<RuntimeCard>();
            return _ruleEngine.GetEvolvableMinions(_localPlayerId);
        }

        /// <summary>
        /// 获取本地玩家状态
        /// </summary>
        public PlayerState GetLocalPlayerState()
        {
            return _currentState?.GetPlayer(_localPlayerId);
        }

        /// <summary>
        /// 获取对手玩家状态
        /// </summary>
        public PlayerState GetOpponentPlayerState()
        {
            return _currentState?.GetPlayer(1 - _localPlayerId);
        }

        /// <summary>
        /// 获取本地玩家战场
        /// </summary>
        public TileState[] GetLocalField()
        {
            return GetLocalPlayerState()?.field;
        }

        /// <summary>
        /// 获取对手战场
        /// </summary>
        public TileState[] GetOpponentField()
        {
            return GetOpponentPlayerState()?.field;
        }

        /// <summary>
        /// 根据instanceId查找卡牌
        /// </summary>
        public RuntimeCard FindCard(int instanceId)
        {
            return _currentState?.FindCardByInstanceId(instanceId);
        }

        /// <summary>
        /// 获取卡牌数据
        /// </summary>
        public CardData GetCardData(int cardId)
        {
            return _cardDatabase?.GetCardById(cardId);
        }

        #endregion

        #region Event Processing

        /// <summary>
        /// 处理事件列表
        /// </summary>
        private void ProcessEvents(List<GameEvent> events)
        {
            foreach (var evt in events)
            {
                ProcessSingleEvent(evt);
            }
        }

        /// <summary>
        /// 处理单个事件
        /// </summary>
        private void ProcessSingleEvent(GameEvent evt)
        {
            // 触发事件回调（用于UI/动画）
            OnGameEvent?.Invoke(evt);

            // 特殊事件处理
            if (evt is TurnStartEvent turnStart)
            {
                UpdateTurnState();
                OnTurnChanged?.Invoke(turnStart.playerId);
            }
            else if (evt is GameOverEvent gameOver)
            {
                HandleGameOver(gameOver.winnerId, gameOver.reason);
            }

            Debug.Log($"GameController: Event {evt.GetType().Name}");
        }

        /// <summary>
        /// 检查游戏是否结束
        /// </summary>
        private void CheckGameOver()
        {
            if (_currentState.IsGameOver())
            {
                int winnerId = _currentState.GetWinnerId();
                HandleGameOver(winnerId, "Game ended");
            }
        }

        /// <summary>
        /// 处理游戏结束
        /// </summary>
        private void HandleGameOver(int winnerId, string reason)
        {
            _isGameOver = true;
            _isMyTurn = false;

            bool isWinner = winnerId == _localPlayerId;
            Debug.Log($"GameController: Game over! Winner: Player {winnerId}, Reason: {reason}");

            OnGameOver?.Invoke(winnerId, reason);
        }

        #endregion

        #region Network Event Handlers

        private void SubscribeNetworkEvents()
        {
            if (_networkManager == null) return;

            _networkManager.OnGameStart += HandleNetworkGameStart;
            _networkManager.OnActionResult += HandleNetworkActionResult;
            _networkManager.OnStateSync += HandleNetworkStateSync;
        }

        private void UnsubscribeNetworkEvents()
        {
            if (_networkManager == null) return;

            _networkManager.OnGameStart -= HandleNetworkGameStart;
            _networkManager.OnActionResult -= HandleNetworkActionResult;
            _networkManager.OnStateSync -= HandleNetworkStateSync;
        }

        private void HandleNetworkGameStart(GameStartPayload payload)
        {
            InitializeFromNetwork(payload, _networkManager.LocalPlayerId);
            StartGame();
        }

        private void HandleNetworkActionResult(ActionResultPayload result)
        {
            _waitingForResponse = false;

            if (!result.success)
            {
                Debug.LogWarning($"GameController: Action failed - {result.errorMessage}");
                return;
            }

            // 处理事件
            var events = MessageSerializer.DeserializeEvents(result.events);
            ProcessEvents(events);

            // 更新状态
            UpdateTurnState();
            OnStateChanged?.Invoke();
        }

        private void HandleNetworkStateSync(StateSyncPayload payload)
        {
            // 反序列化状态
            var syncedState = JsonUtility.FromJson<GameState>(payload.gameStateJson);

            // 验证哈希（可选）
            // TODO: 实现状态哈希验证

            _currentState = syncedState;
            UpdateTurnState();
            OnStateChanged?.Invoke();

            Debug.Log($"GameController: State synced, hash: {payload.stateHash}");
        }

        #endregion

        #region AI Support

        /// <summary>
        /// 切换控制的玩家（用于本地双人或调试）
        /// </summary>
        public void SwitchControlledPlayer()
        {
            _localPlayerId = 1 - _localPlayerId;
            UpdateTurnState();
            OnStateChanged?.Invoke();
            Debug.Log($"GameController: Switched to control player {_localPlayerId}");
        }

        /// <summary>
        /// 设置控制的玩家
        /// </summary>
        public void SetControlledPlayer(int playerId)
        {
            if (playerId >= 0 && playerId <= 1)
            {
                _localPlayerId = playerId;
                UpdateTurnState();
                OnStateChanged?.Invoke();
            }
        }

        #endregion
    }
}
