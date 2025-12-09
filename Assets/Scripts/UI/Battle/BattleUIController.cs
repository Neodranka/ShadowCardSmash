using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;
using ShadowCardSmash.Core.Effects;
using ShadowCardSmash.Core.Rules;
using ShadowCardSmash.Managers;

namespace ShadowCardSmash.UI.Battle
{
    /// <summary>
    /// 战斗界面主控制器
    /// </summary>
    public class BattleUIController : MonoBehaviour
    {
        [Header("Player Info Panels")]
        public PlayerInfoPanel myInfoPanel;
        public PlayerInfoPanel opponentInfoPanel;

        [Header("Hand Areas")]
        public HandAreaController myHandArea;
        public HandAreaController opponentHandArea;

        [Header("Battlefield")]
        public TileSlotController[] myTiles;      // 6个
        public TileSlotController[] opponentTiles; // 6个

        [Header("Deck & Graveyard")]
        public DeckPileDisplay myDeckPile;
        public DeckPileDisplay opponentDeckPile;
        public GraveyardDisplay myGraveyard;
        public GraveyardDisplay opponentGraveyard;

        [Header("Buttons")]
        public Button endTurnButton;
        public Button evolveButton;
        public TextMeshProUGUI endTurnButtonText;
        public TextMeshProUGUI evolveButtonText;

        [Header("Popups")]
        public CardDetailPopup cardDetailPopup;
        public CardListPopup cardListPopup;

        [Header("Turn Indicator")]
        public GameObject myTurnIndicator;
        public TextMeshProUGUI turnNumberText;
        public TextMeshProUGUI phaseText;

        [Header("References")]
        public GameController gameController;

        // 卡牌数据库
        private ICardDatabase _cardDatabase;

        // 本地玩家ID
        private int _localPlayerId;

        // 当前交互状态
        private BattleUIState _currentState = BattleUIState.Idle;
        #pragma warning disable CS0414
        private CardViewController _selectedCard; // 保留用于未来扩展
        #pragma warning restore CS0414
        private int _selectedHandIndex = -1;
        private TileSlotController _selectedTile;
        private int _selectedAttackerInstanceId = -1;

        // 有效目标列表
        private List<int> _validTargetInstanceIds = new List<int>();
        private List<int> _validTileIndices = new List<int>();

        // 属性
        public BattleUIState CurrentState => _currentState;
        public int LocalPlayerId => _localPlayerId;

        // 事件
        public event Action<GameEvent> OnGameEventReceived;

        void Start()
        {
            // 绑定按钮事件
            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }
            if (evolveButton != null)
            {
                evolveButton.onClick.AddListener(OnEvolveClicked);
            }

            // 绑定手牌事件
            if (myHandArea != null)
            {
                myHandArea.OnCardClicked += OnHandCardClicked;
                myHandArea.OnCardRightClicked += OnHandCardRightClicked;
            }

            // 绑定格子事件
            BindTileEvents(myTiles, false);
            BindTileEvents(opponentTiles, true);

            // 绑定牌库/墓地事件
            BindDeckGraveyardEvents();

            // 绑定对手手牌区点击事件（用于攻击玩家）
            if (opponentHandArea != null)
            {
                opponentHandArea.OnAreaClicked += OnOpponentHandAreaClicked;
            }

            // 注意：游戏控制器事件订阅在 Initialize() 中完成，不在这里重复订阅
        }

        void Update()
        {
            // ESC 或右键取消当前选择状态
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                if (_currentState != BattleUIState.Idle && _currentState != BattleUIState.WaitingForOpponent)
                {
                    CancelCurrentAction();
                }
            }
        }

        void OnDestroy()
        {
            // 取消订阅事件
            if (gameController != null)
            {
                gameController.OnGameEvent -= HandleGameEvent;
                gameController.OnStateChanged -= RefreshAllUI;
                gameController.OnTurnChanged -= OnTurnChanged;
                gameController.OnGameOver -= OnGameOver;
            }
        }

        #region Initialization

        /// <summary>
        /// 初始化界面
        /// </summary>
        public void Initialize(ICardDatabase cardDatabase, int localPlayerId)
        {
            _cardDatabase = cardDatabase;
            _localPlayerId = localPlayerId;

            // 设置手牌区域的数据库引用
            if (myHandArea != null)
            {
                myHandArea.SetCardDatabase(cardDatabase);
                myHandArea.isOpponentHand = false;
            }
            if (opponentHandArea != null)
            {
                opponentHandArea.SetCardDatabase(cardDatabase);
                opponentHandArea.isOpponentHand = true;
            }

            // 设置弹窗的数据库引用
            if (cardListPopup != null)
            {
                cardListPopup.SetCardDatabase(cardDatabase);
            }

            // 初始化格子索引
            InitializeTileIndices();

            // 订阅游戏控制器事件（如果还没订阅）
            SubscribeToGameController();

            Debug.Log($"BattleUIController: 初始化完成，本地玩家ID: {localPlayerId}");
        }

        /// <summary>
        /// 订阅游戏控制器事件
        /// </summary>
        private void SubscribeToGameController()
        {
            if (gameController == null) return;

            // 先取消订阅（避免重复订阅）
            gameController.OnGameEvent -= HandleGameEvent;
            gameController.OnStateChanged -= RefreshAllUI;
            gameController.OnTurnChanged -= OnTurnChanged;
            gameController.OnGameOver -= OnGameOver;

            // 重新订阅
            gameController.OnGameEvent += HandleGameEvent;
            gameController.OnStateChanged += RefreshAllUI;
            gameController.OnTurnChanged += OnTurnChanged;
            gameController.OnGameOver += OnGameOver;

            Debug.Log("BattleUIController: 已订阅 GameController 事件");
        }

        private void InitializeTileIndices()
        {
            if (myTiles != null)
            {
                for (int i = 0; i < myTiles.Length; i++)
                {
                    if (myTiles[i] != null)
                    {
                        myTiles[i].tileIndex = i;
                        myTiles[i].isOpponentTile = false;
                    }
                }
            }

            if (opponentTiles != null)
            {
                for (int i = 0; i < opponentTiles.Length; i++)
                {
                    if (opponentTiles[i] != null)
                    {
                        opponentTiles[i].tileIndex = i;
                        opponentTiles[i].isOpponentTile = true;
                    }
                }
            }
        }

        private void BindTileEvents(TileSlotController[] tiles, bool isOpponent)
        {
            if (tiles == null) return;

            foreach (var tile in tiles)
            {
                if (tile != null)
                {
                    tile.OnTileClicked += OnTileClicked;
                    tile.OnTileRightClicked += OnTileRightClicked;
                }
            }
        }

        private void BindDeckGraveyardEvents()
        {
            if (myDeckPile != null)
            {
                myDeckPile.OnDeckClicked += () => ShowDeckList(_localPlayerId);
            }
            if (opponentDeckPile != null)
            {
                opponentDeckPile.OnDeckClicked += () => ShowDeckList(1 - _localPlayerId);
            }
            if (myGraveyard != null)
            {
                myGraveyard.OnGraveyardClicked += () => ShowGraveyardList(_localPlayerId);
            }
            if (opponentGraveyard != null)
            {
                opponentGraveyard.OnGraveyardClicked += () => ShowGraveyardList(1 - _localPlayerId);
            }
        }

        #endregion

        #region Refresh UI

        /// <summary>
        /// 刷新所有UI
        /// </summary>
        public void RefreshAllUI()
        {
            if (gameController == null || gameController.CurrentState == null) return;

            var state = gameController.CurrentState;

            // 刷新双方信息
            RefreshPlayerInfo(_localPlayerId);
            RefreshPlayerInfo(1 - _localPlayerId);

            // 刷新手牌
            RefreshHand(_localPlayerId);
            RefreshHand(1 - _localPlayerId);

            // 刷新战场
            RefreshBattlefield();

            // 刷新牌库/墓地
            RefreshDeckGraveyard();

            // 刷新回合指示
            RefreshTurnIndicator();

            // 刷新按钮状态
            RefreshButtons();

            // 更新可操作提示
            if (gameController.IsMyTurn)
            {
                HighlightPlayableCards();
            }
        }

        /// <summary>
        /// 刷新玩家信息
        /// </summary>
        public void RefreshPlayerInfo(int playerId)
        {
            var state = gameController?.CurrentState;
            if (state == null) return;

            var playerState = state.GetPlayer(playerId);
            if (playerState == null) return;

            PlayerInfoPanel panel = playerId == _localPlayerId ? myInfoPanel : opponentInfoPanel;
            if (panel != null)
            {
                panel.UpdateDisplay(playerState);
            }
        }

        /// <summary>
        /// 刷新手牌
        /// </summary>
        public void RefreshHand(int playerId)
        {
            Debug.Log($"BattleUIController.RefreshHand: playerId={playerId}, localPlayerId={_localPlayerId}");

            var state = gameController?.CurrentState;
            if (state == null)
            {
                Debug.LogError("BattleUIController.RefreshHand: gameController.CurrentState 为空!");
                return;
            }

            var playerState = state.GetPlayer(playerId);
            if (playerState == null)
            {
                Debug.LogError($"BattleUIController.RefreshHand: 找不到玩家 {playerId} 的状态!");
                return;
            }

            Debug.Log($"BattleUIController.RefreshHand: 玩家{playerId}手牌数={playerState.hand?.Count ?? 0}");

            HandAreaController handArea = playerId == _localPlayerId ? myHandArea : opponentHandArea;
            if (handArea != null)
            {
                Debug.Log($"BattleUIController.RefreshHand: 调用 handArea.SetHand, handArea={handArea.gameObject.name}");
                handArea.SetHand(playerState.hand);
            }
            else
            {
                Debug.LogError($"BattleUIController.RefreshHand: handArea 为空! (playerId={playerId}, isLocal={playerId == _localPlayerId})");
            }
        }

        /// <summary>
        /// 刷新战场
        /// </summary>
        public void RefreshBattlefield()
        {
            var state = gameController?.CurrentState;
            if (state == null) return;

            // 刷新我方战场
            var myField = state.GetPlayer(_localPlayerId)?.field;
            RefreshFieldTiles(myTiles, myField);

            // 刷新对手战场
            var opponentField = state.GetPlayer(1 - _localPlayerId)?.field;
            RefreshFieldTiles(opponentTiles, opponentField);
        }

        private void RefreshFieldTiles(TileSlotController[] tiles, TileState[] fieldState)
        {
            if (tiles == null || fieldState == null) return;

            for (int i = 0; i < tiles.Length && i < fieldState.Length; i++)
            {
                var tile = tiles[i];
                var tileState = fieldState[i];

                if (tile == null) continue;

                // 清除现有单位
                var currentOccupant = tile.RemoveUnit();
                if (currentOccupant != null)
                {
                    Destroy(currentOccupant.gameObject);
                }

                // 如果格子有单位，创建卡牌视图
                if (!tileState.IsEmpty() && tileState.occupant != null)
                {
                    var cardData = _cardDatabase?.GetCardById(tileState.occupant.cardId);
                    if (cardData != null)
                    {
                        var cardView = CreateCardViewForTile(tileState.occupant, cardData);
                        tile.PlaceUnit(cardView);
                    }
                }

                // 清除高亮
                tile.ClearHighlights();
            }
        }

        private CardViewController CreateCardViewForTile(RuntimeCard runtimeCard, CardData cardData)
        {
            if (myHandArea == null || myHandArea.cardPrefab == null) return null;

            var cardObj = Instantiate(myHandArea.cardPrefab);
            var cardView = cardObj.GetComponent<CardViewController>();
            if (cardView != null)
            {
                cardView.SetRuntimeCard(runtimeCard, cardData);
            }
            return cardView;
        }

        private void RefreshDeckGraveyard()
        {
            var state = gameController?.CurrentState;
            if (state == null) return;

            var myState = state.GetPlayer(_localPlayerId);
            var opponentState = state.GetPlayer(1 - _localPlayerId);

            if (myDeckPile != null && myState != null)
            {
                myDeckPile.UpdateCount(myState.deck.Count);
            }
            if (opponentDeckPile != null && opponentState != null)
            {
                opponentDeckPile.UpdateCount(opponentState.deck.Count);
            }
            if (myGraveyard != null && myState != null)
            {
                myGraveyard.UpdateCount(myState.graveyard.Count);
            }
            if (opponentGraveyard != null && opponentState != null)
            {
                opponentGraveyard.UpdateCount(opponentState.graveyard.Count);
            }
        }

        private void RefreshTurnIndicator()
        {
            var state = gameController?.CurrentState;
            if (state == null) return;

            bool isMyTurn = gameController.IsMyTurn;

            if (myTurnIndicator != null)
            {
                myTurnIndicator.SetActive(isMyTurn);
            }

            if (turnNumberText != null)
            {
                turnNumberText.text = $"回合 {state.turnNumber}";
            }

            if (phaseText != null)
            {
                phaseText.text = isMyTurn ? "你的回合" : "对手回合";
            }
        }

        private void RefreshButtons()
        {
            bool isMyTurn = gameController?.IsMyTurn ?? false;
            // 只要是我的回合，就可以结束回合（不受 UI 状态影响）
            bool canEndTurn = isMyTurn && _currentState != BattleUIState.WaitingForOpponent;

            Debug.Log($"RefreshButtons: isMyTurn={isMyTurn}, state={_currentState}, canEndTurn={canEndTurn}");

            if (endTurnButton != null)
            {
                endTurnButton.interactable = canEndTurn;
            }

            if (evolveButton != null)
            {
                var myState = gameController?.GetLocalPlayerState();
                bool canEvolve = isMyTurn && myState != null && myState.evolutionPoints > 0;
                evolveButton.interactable = canEvolve;

                if (evolveButtonText != null)
                {
                    evolveButtonText.text = $"进化 ({myState?.evolutionPoints ?? 0})";
                }
            }
        }

        private void HighlightPlayableCards()
        {
            if (myHandArea == null || gameController == null) return;

            var playableIndices = gameController.GetPlayableCardIndices();
            myHandArea.HighlightPlayableCards(playableIndices);
        }

        #endregion

        #region Turn Management

        /// <summary>
        /// 回合开始
        /// </summary>
        public void OnTurnStart(int playerId)
        {
            Debug.Log($"BattleUIController: 回合开始 - 玩家{playerId}, localPlayerId={_localPlayerId}");

            if (playerId == _localPlayerId)
            {
                // 我的回合
                SetState(BattleUIState.Idle);
                ShowMessage("你的回合");
                HighlightPlayableCards();
            }
            else
            {
                // 对手回合
                SetState(BattleUIState.WaitingForOpponent);
                ShowMessage("对手回合");
                ClearAllHighlights();
            }

            // 确保按钮状态正确更新
            RefreshButtons();
        }

        private void OnTurnChanged(int playerId)
        {
            OnTurnStart(playerId);
        }

        /// <summary>
        /// 回合结束
        /// </summary>
        public void OnTurnEnd(int playerId)
        {
            Debug.Log($"BattleUIController: 回合结束 - 玩家{playerId}");

            ClearAllHighlights();
            SetState(BattleUIState.Idle);
        }

        /// <summary>
        /// 设置是否为我的回合
        /// </summary>
        public void SetMyTurn(bool isMyTurn)
        {
            if (isMyTurn)
            {
                SetState(BattleUIState.Idle);
                HighlightPlayableCards();
            }
            else
            {
                SetState(BattleUIState.WaitingForOpponent);
                ClearAllHighlights();
            }

            RefreshButtons();
        }

        #endregion

        #region Interaction Handlers

        private void OnHandCardClicked(int handIndex)
        {
            if (!gameController.IsMyTurn) return;

            switch (_currentState)
            {
                case BattleUIState.Idle:
                case BattleUIState.CardSelected:
                    // 选中卡牌
                    SelectHandCard(handIndex);
                    break;

                case BattleUIState.SelectingTile:
                case BattleUIState.SelectingTarget:
                    // 取消当前操作，选中新卡牌
                    CancelCurrentAction();
                    SelectHandCard(handIndex);
                    break;
            }
        }

        private void OnHandCardRightClicked(int handIndex)
        {
            // 显示卡牌详情
            var state = gameController?.CurrentState;
            if (state == null) return;

            var myState = state.GetPlayer(_localPlayerId);
            if (myState == null || handIndex >= myState.hand.Count) return;

            var card = myState.hand[handIndex];
            var cardData = _cardDatabase?.GetCardById(card.cardId);
            if (cardData != null && cardDetailPopup != null)
            {
                cardDetailPopup.Show(card, cardData);
            }
        }

        private void OnTileClicked(TileSlotController tile)
        {
            Debug.Log($"BattleUIController: 点击格子 - index={tile.tileIndex}, isOpponent={tile.isOpponentTile}, isEmpty={tile.IsEmpty}, state={_currentState}");

            if (!gameController.IsMyTurn)
            {
                Debug.Log("BattleUIController: 不是我的回合，忽略点击");
                return;
            }

            switch (_currentState)
            {
                case BattleUIState.Idle:
                    // 点击我方有单位的格子，准备攻击
                    if (!tile.isOpponentTile && !tile.IsEmpty)
                    {
                        Debug.Log($"BattleUIController: Idle状态，点击我方有单位的格子{tile.tileIndex}，尝试选择攻击者");
                        SelectAttacker(tile);
                    }
                    break;

                case BattleUIState.CardSelected:
                case BattleUIState.SelectingTile:
                    // 选择放置位置
                    if (!tile.isOpponentTile && tile.IsEmpty && tile.IsValidPlacement)
                    {
                        PlaceCardOnTile(tile);
                    }
                    else
                    {
                        // 点击了无效位置，取消选择
                        CancelCurrentAction();
                    }
                    break;

                case BattleUIState.SelectingAttacker:
                    if (!tile.isOpponentTile && !tile.IsEmpty)
                    {
                        SelectAttacker(tile);
                    }
                    break;

                case BattleUIState.SelectingAttackTarget:
                    // 选择攻击目标
                    if (tile.IsValidTarget)
                    {
                        ExecuteAttack(tile);
                    }
                    else
                    {
                        CancelCurrentAction();
                    }
                    break;

                case BattleUIState.SelectingTarget:
                    // 选择效果目标
                    if (tile.IsValidTarget)
                    {
                        ExecuteCardWithTarget(tile);
                    }
                    break;

                case BattleUIState.SelectingEvolutionTarget:
                    // 选择进化目标
                    if (!tile.isOpponentTile && tile.IsValidTarget && !tile.IsEmpty)
                    {
                        ExecuteEvolution(tile);
                    }
                    else
                    {
                        CancelCurrentAction();
                    }
                    break;
            }
        }

        private void OnTileRightClicked(TileSlotController tile)
        {
            // 右键点击格子显示卡牌详情
            if (tile != null && !tile.IsEmpty && tile.CurrentOccupant != null)
            {
                var cardView = tile.CurrentOccupant;
                var cardData = _cardDatabase?.GetCardById(cardView.RuntimeCard?.cardId ?? cardView.CardData?.cardId ?? 0);

                if (cardData != null && cardDetailPopup != null)
                {
                    cardDetailPopup.Show(cardView.RuntimeCard, cardData);
                    Debug.Log($"BattleUIController: 右键显示格子{tile.tileIndex}的卡牌详情 - {cardData.cardName}");
                }
            }
        }

        private void OnEndTurnClicked()
        {
            if (!gameController.IsMyTurn) return;

            CancelCurrentAction();
            gameController.TryEndTurn();
        }

        private void OnOpponentHandAreaClicked()
        {
            if (!gameController.IsMyTurn) return;

            // 只有在选择攻击目标状态下，且对手是有效目标时才能攻击
            if (_currentState == BattleUIState.SelectingAttackTarget &&
                opponentHandArea != null &&
                opponentHandArea.IsValidAttackTarget)
            {
                ExecuteAttackOnPlayer();
            }
        }

        private void OnEvolveClicked()
        {
            if (!gameController.IsMyTurn) return;

            // 进入进化选择模式
            var evolvableMinions = gameController.GetEvolvableMinions();
            if (evolvableMinions.Count > 0)
            {
                // 高亮可进化的随从
                HighlightEvolvableMinions(evolvableMinions);
                SetState(BattleUIState.SelectingEvolutionTarget);
                Debug.Log($"BattleUIController: 进入进化选择模式，可进化随从数: {evolvableMinions.Count}");
            }
            else
            {
                Debug.Log("BattleUIController: 没有可进化的随从");
            }
        }

        #endregion

        #region Card Playing

        private void SelectHandCard(int handIndex)
        {
            var playableIndices = gameController.GetPlayableCardIndices();
            if (!playableIndices.Contains(handIndex))
            {
                Debug.Log($"BattleUIController: 手牌{handIndex}不可使用");
                return;
            }

            _selectedHandIndex = handIndex;
            myHandArea?.SelectCard(handIndex);

            // 高亮可放置的格子
            HighlightValidPlacementTiles();

            SetState(BattleUIState.SelectingTile);
            Debug.Log($"BattleUIController: 选中手牌{handIndex}");
        }

        private void PlaceCardOnTile(TileSlotController tile)
        {
            if (_selectedHandIndex < 0) return;

            // 获取卡牌信息，检查是否需要选择目标
            var myState = gameController.GetLocalPlayerState();
            if (myState == null || _selectedHandIndex >= myState.hand.Count) return;

            var card = myState.hand[_selectedHandIndex];
            var cardData = _cardDatabase?.GetCardById(card.cardId);

            // TODO: 检查是否需要选择目标
            // 暂时直接使用
            bool success = gameController.TryPlayCard(_selectedHandIndex, tile.tileIndex);

            if (success)
            {
                Debug.Log($"BattleUIController: 打出卡牌到格子{tile.tileIndex}");
            }
            else
            {
                Debug.LogWarning($"BattleUIController: 打出卡牌失败");
            }

            CancelCurrentAction();
        }

        private void ExecuteCardWithTarget(TileSlotController targetTile)
        {
            if (_selectedHandIndex < 0) return;

            int targetInstanceId = -1;
            bool targetIsPlayer = false;
            int targetPlayerId = -1;

            if (!targetTile.IsEmpty)
            {
                var occupant = targetTile.GetOccupant();
                if (occupant?.RuntimeCard != null)
                {
                    targetInstanceId = occupant.RuntimeCard.instanceId;
                }
            }

            bool success = gameController.TryPlayCard(_selectedHandIndex, 0, targetInstanceId, targetIsPlayer, targetPlayerId);

            if (success)
            {
                Debug.Log($"BattleUIController: 打出卡牌，目标: {targetInstanceId}");
            }

            CancelCurrentAction();
        }

        #endregion

        #region Attack

        private void SelectAttacker(TileSlotController tile)
        {
            var occupant = tile.GetOccupant();
            if (occupant == null)
            {
                Debug.Log($"BattleUIController: 格子{tile.tileIndex}没有占据者(occupant为null)");
                return;
            }
            if (occupant.RuntimeCard == null)
            {
                Debug.Log($"BattleUIController: 格子{tile.tileIndex}的占据者没有RuntimeCard");
                return;
            }

            Debug.Log($"BattleUIController: 尝试选择攻击者 - 格子{tile.tileIndex}, 单位ID={occupant.RuntimeCard.instanceId}, canAttack={occupant.RuntimeCard.canAttack}");

            if (!occupant.RuntimeCard.canAttack)
            {
                Debug.Log("BattleUIController: 该单位无法攻击（可能是召唤病或已攻击过）");
                return;
            }

            _selectedAttackerInstanceId = occupant.RuntimeCard.instanceId;
            _selectedTile = tile;
            tile.SetValidAttackTarget(true); // 标记选中

            // 高亮可攻击目标
            HighlightValidAttackTargets();

            SetState(BattleUIState.SelectingAttackTarget);
            Debug.Log($"BattleUIController: 选中攻击者 {_selectedAttackerInstanceId}");
        }

        private void ExecuteAttack(TileSlotController targetTile)
        {
            if (_selectedAttackerInstanceId < 0) return;

            int targetInstanceId = -1;
            bool targetIsPlayer = false;
            int targetPlayerId = -1;

            if (!targetTile.IsEmpty)
            {
                var occupant = targetTile.GetOccupant();
                if (occupant?.RuntimeCard != null)
                {
                    targetInstanceId = occupant.RuntimeCard.instanceId;
                }
            }

            // TODO: 支持攻击玩家

            bool success = gameController.TryAttack(_selectedAttackerInstanceId, targetInstanceId, targetIsPlayer, targetPlayerId);

            if (success)
            {
                Debug.Log($"BattleUIController: 执行攻击 {_selectedAttackerInstanceId} -> {targetInstanceId}");
            }

            CancelCurrentAction();
        }

        private void ExecuteAttackOnPlayer()
        {
            if (_selectedAttackerInstanceId < 0) return;

            int opponentId = 1 - _localPlayerId;
            bool success = gameController.TryAttack(_selectedAttackerInstanceId, -1, true, opponentId);

            if (success)
            {
                Debug.Log($"BattleUIController: 执行攻击玩家 {_selectedAttackerInstanceId} -> 玩家{opponentId}");
            }

            CancelCurrentAction();
        }

        private void ExecuteEvolution(TileSlotController tile)
        {
            var occupant = tile.GetOccupant();
            if (occupant?.RuntimeCard == null)
            {
                Debug.LogWarning("BattleUIController: 进化目标格子没有单位");
                CancelCurrentAction();
                return;
            }

            int instanceId = occupant.RuntimeCard.instanceId;
            bool success = gameController.TryEvolve(instanceId);

            if (success)
            {
                Debug.Log($"BattleUIController: 进化成功 - 单位 {instanceId}");
            }
            else
            {
                Debug.LogWarning($"BattleUIController: 进化失败 - 单位 {instanceId}");
            }

            CancelCurrentAction();
        }

        #endregion

        #region Highlighting

        private void HighlightValidPlacementTiles()
        {
            _validTileIndices.Clear();

            var myField = gameController.GetLocalField();
            if (myField == null || myTiles == null) return;

            for (int i = 0; i < myTiles.Length && i < myField.Length; i++)
            {
                bool isEmpty = myField[i].IsEmpty();
                myTiles[i].SetValidPlacementTarget(isEmpty);
                if (isEmpty)
                {
                    _validTileIndices.Add(i);
                }
            }
        }

        private void HighlightValidAttackTargets()
        {
            if (_selectedAttackerInstanceId < 0) return;

            var targets = gameController.GetValidAttackTargets(_selectedAttackerInstanceId);
            _validTargetInstanceIds.Clear();

            foreach (var target in targets)
            {
                _validTargetInstanceIds.Add(target.instanceId);

                if (target.isPlayer)
                {
                    // 高亮对手手牌区
                    if (opponentHandArea != null)
                    {
                        opponentHandArea.SetValidAttackTarget(true);
                    }
                }
                else if (opponentTiles != null)
                {
                    // 在对手战场找到对应的格子
                    foreach (var tile in opponentTiles)
                    {
                        var occupant = tile.GetOccupant();
                        if (occupant?.RuntimeCard != null && occupant.RuntimeCard.instanceId == target.instanceId)
                        {
                            tile.SetValidAttackTarget(true);
                            break;
                        }
                    }
                }
            }
        }

        private void HighlightEvolvableMinions(List<RuntimeCard> evolvableMinions)
        {
            if (myTiles == null) return;

            foreach (var minion in evolvableMinions)
            {
                foreach (var tile in myTiles)
                {
                    var occupant = tile.GetOccupant();
                    if (occupant?.RuntimeCard != null && occupant.RuntimeCard.instanceId == minion.instanceId)
                    {
                        tile.SetValidAttackTarget(true);
                        break;
                    }
                }
            }
        }

        private void ClearAllHighlights()
        {
            myHandArea?.ClearHighlights();

            if (myTiles != null)
            {
                foreach (var tile in myTiles)
                {
                    tile?.ClearHighlights();
                }
            }

            if (opponentTiles != null)
            {
                foreach (var tile in opponentTiles)
                {
                    tile?.ClearHighlights();
                }
            }

            // 清除对手手牌区攻击目标高亮
            opponentHandArea?.ClearAttackTargetHighlight();

            _validTileIndices.Clear();
            _validTargetInstanceIds.Clear();
        }

        #endregion

        #region State Management

        private void SetState(BattleUIState newState)
        {
            _currentState = newState;
            Debug.Log($"BattleUIController: 状态切换到 {newState}");
        }

        private void CancelCurrentAction()
        {
            _selectedHandIndex = -1;
            _selectedCard = null;
            _selectedTile = null;
            _selectedAttackerInstanceId = -1;

            ClearAllHighlights();

            if (gameController.IsMyTurn)
            {
                SetState(BattleUIState.Idle);
                HighlightPlayableCards();
            }
            else
            {
                SetState(BattleUIState.WaitingForOpponent);
            }
        }

        #endregion

        #region Game Event Handling

        private void HandleGameEvent(GameEvent gameEvent)
        {
            OnGameEventReceived?.Invoke(gameEvent);
            PlayGameEvent(gameEvent);
        }

        /// <summary>
        /// 播放游戏事件动画
        /// </summary>
        public void PlayGameEvent(GameEvent gameEvent)
        {
            // TODO: 根据事件类型播放对应动画
            switch (gameEvent)
            {
                case CardDrawnEvent drawEvent:
                    PlayDrawAnimation(drawEvent.playerId);
                    break;

                case DamageEvent damageEvent:
                    PlayDamageAnimation(damageEvent.targetInstanceId, damageEvent.amount, damageEvent.targetIsPlayer, damageEvent.targetPlayerId);
                    break;

                case HealEvent healEvent:
                    PlayHealAnimation(healEvent.targetInstanceId, healEvent.amount, healEvent.targetIsPlayer, healEvent.targetPlayerId);
                    break;

                case SummonEvent summonEvent:
                    PlaySummonAnimation(summonEvent.tileIndex, summonEvent.ownerId != _localPlayerId);
                    break;

                case UnitDestroyedEvent destroyEvent:
                    PlayDeathAnimation(destroyEvent.instanceId);
                    break;

                case AttackEvent attackEvent:
                    PlayAttackAnimation(attackEvent.attackerInstanceId, attackEvent.defenderInstanceId);
                    break;

                case EvolveEvent evolveEvent:
                    PlayEvolveAnimation(evolveEvent.instanceId);
                    break;
            }
        }

        public void PlayDamageAnimation(int targetInstanceId, int amount, bool targetIsPlayer = false, int targetPlayerId = -1)
        {
            if (targetIsPlayer)
            {
                var panel = targetPlayerId == _localPlayerId ? myInfoPanel : opponentInfoPanel;
                panel?.PlayDamageAnimation(amount);
            }
            else
            {
                var cardView = FindCardViewByInstanceId(targetInstanceId);
                cardView?.PlayDamageAnimation(amount);
            }
        }

        public void PlayHealAnimation(int targetInstanceId, int amount, bool targetIsPlayer = false, int targetPlayerId = -1)
        {
            if (targetIsPlayer)
            {
                var panel = targetPlayerId == _localPlayerId ? myInfoPanel : opponentInfoPanel;
                panel?.PlayHealAnimation(amount);
            }
            else
            {
                var cardView = FindCardViewByInstanceId(targetInstanceId);
                cardView?.PlayHealAnimation(amount);
            }
        }

        public void PlaySummonAnimation(int tileIndex, bool isOpponent)
        {
            Debug.Log($"BattleUIController: 播放召唤动画 - 格子{tileIndex}, 对手={isOpponent}");
        }

        public void PlayDeathAnimation(int instanceId)
        {
            var cardView = FindCardViewByInstanceId(instanceId);
            cardView?.PlayDeathAnimation();
        }

        public void PlayDrawAnimation(int playerId)
        {
            var deckPile = playerId == _localPlayerId ? myDeckPile : opponentDeckPile;
            deckPile?.PlayDrawAnimation();
        }

        public void PlayAttackAnimation(int attackerInstanceId, int targetInstanceId)
        {
            Debug.Log($"BattleUIController: 播放攻击动画 - {attackerInstanceId} -> {targetInstanceId}");
        }

        public void PlayEvolveAnimation(int instanceId)
        {
            var cardView = FindCardViewByInstanceId(instanceId);
            cardView?.PlayEvolveAnimation();
        }

        private CardViewController FindCardViewByInstanceId(int instanceId)
        {
            // 在我方战场查找
            if (myTiles != null)
            {
                foreach (var tile in myTiles)
                {
                    var occupant = tile?.GetOccupant();
                    if (occupant?.RuntimeCard != null && occupant.RuntimeCard.instanceId == instanceId)
                    {
                        return occupant;
                    }
                }
            }

            // 在对手战场查找
            if (opponentTiles != null)
            {
                foreach (var tile in opponentTiles)
                {
                    var occupant = tile?.GetOccupant();
                    if (occupant?.RuntimeCard != null && occupant.RuntimeCard.instanceId == instanceId)
                    {
                        return occupant;
                    }
                }
            }

            return null;
        }

        #endregion

        #region Popups

        private void ShowDeckList(int playerId)
        {
            var state = gameController?.CurrentState?.GetPlayer(playerId);
            if (state != null && cardListPopup != null)
            {
                cardListPopup.ShowDeck(state.deck);
            }
        }

        private void ShowGraveyardList(int playerId)
        {
            var state = gameController?.CurrentState?.GetPlayer(playerId);
            if (state != null && cardListPopup != null)
            {
                cardListPopup.ShowGraveyard(state.graveyard);
            }
        }

        #endregion

        #region Utility

        private void ShowMessage(string message)
        {
            Debug.Log($"BattleUIController: {message}");
            // TODO: 显示UI消息
        }

        private void OnGameOver(int winnerId, string reason)
        {
            Debug.Log($"BattleUIController: 游戏结束 - 胜者:{winnerId}, 原因:{reason}");
            SetState(BattleUIState.Idle);

            // TODO: 显示游戏结束UI
            bool isWinner = winnerId == _localPlayerId;
            ShowMessage(isWinner ? "你赢了！" : "你输了...");
        }

        #endregion
    }

    /// <summary>
    /// 战斗UI状态
    /// </summary>
    public enum BattleUIState
    {
        Idle,                   // 等待输入
        CardSelected,           // 已选中手牌
        SelectingTile,          // 选择放置格子
        SelectingTarget,        // 选择效果目标
        SelectingAttacker,      // 选择攻击者
        SelectingAttackTarget,  // 选择攻击目标
        SelectingEvolutionTarget, // 选择进化目标
        WaitingForOpponent,     // 等待对手
        Animating               // 播放动画中
    }
}
