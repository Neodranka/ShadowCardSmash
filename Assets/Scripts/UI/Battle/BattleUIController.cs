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

        [Header("Hand Card Selection")]
        public HandCardSelectionUI handCardSelectionUI;

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

        // 手牌选择回调
        private System.Action<int> _handCardSelectionCallback;
        private System.Action _handCardSelectionCancelCallback;

        // 多选地格相关
        private List<int> _selectedTileIndices = new List<int>();
        private int _requiredTileCount = 0;
        private bool _selectingEnemyTiles = false;
        private System.Action<List<int>> _tileSelectionCallback;

        // 目标选择相关（爆破专家等需要选择目标的卡牌）
        private int _pendingCardHandIndex = -1;
        private int _pendingCardTileIndex = -1;
        private bool _pendingUseEnhance = false;

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

            // 取消手牌选择UI事件
            if (handCardSelectionUI != null)
            {
                handCardSelectionUI.OnCardSelected -= OnHandCardSelected;
                handCardSelectionUI.OnCancelled -= OnHandCardSelectionCancelled;
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

            // 设置手牌选择UI的数据库引用
            // 如果未设置，尝试自动查找
            if (handCardSelectionUI == null)
            {
                handCardSelectionUI = FindObjectOfType<HandCardSelectionUI>(true);
                if (handCardSelectionUI != null)
                {
                    Debug.Log("BattleUIController: 自动找到 HandCardSelectionUI");
                }
            }
            if (handCardSelectionUI != null)
            {
                handCardSelectionUI.SetCardDatabase(cardDatabase);
                handCardSelectionUI.OnCardSelected += OnHandCardSelected;
                handCardSelectionUI.OnCancelled += OnHandCardSelectionCancelled;
            }
            else
            {
                Debug.LogWarning("BattleUIController: HandCardSelectionUI 未找到，手牌选择功能将不可用");
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
            else
            {
                // 不是我的回合时清除所有高亮
                ClearAllHighlights();
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

                // 更新地格效果显示
                tile.UpdateTileEffectDisplay(tileState);

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
                cardDetailPopup.Show(card, cardData, myState);
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
                    // 获取当前选中的卡牌信息
                    var myState = gameController.GetLocalPlayerState();
                    CardData selectedCardData = null;
                    if (myState != null && _selectedHandIndex >= 0 && _selectedHandIndex < myState.hand.Count)
                    {
                        var selectedCard = myState.hand[_selectedHandIndex];
                        selectedCardData = _cardDatabase?.GetCardById(selectedCard.cardId);
                    }

                    if (selectedCardData != null && selectedCardData.cardType == CardType.Spell)
                    {
                        // 法术卡：点击任意格子都可以释放（点击整个场地区域）
                        PlaceCardOnTile(tile);
                    }
                    else
                    {
                        // 随从/护符：需要选择空格子
                        if (!tile.isOpponentTile && tile.IsEmpty && tile.IsValidPlacement)
                        {
                            PlaceCardOnTile(tile);
                        }
                        else
                        {
                            // 点击了无效位置，取消选择
                            CancelCurrentAction();
                        }
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

                case BattleUIState.SelectingMultipleTiles:
                    // 多选地格（倾盆大雨等）
                    HandleMultipleTileSelection(tile);
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
                    // 获取卡牌所有者的状态
                    var state = gameController?.CurrentState;
                    int ownerId = cardView.RuntimeCard?.ownerId ?? _localPlayerId;
                    var ownerState = state?.GetPlayer(ownerId);

                    cardDetailPopup.Show(cardView.RuntimeCard, cardData, ownerState);
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

            // 在选择攻击目标状态下，攻击敌方玩家
            if (_currentState == BattleUIState.SelectingAttackTarget &&
                opponentHandArea != null &&
                opponentHandArea.IsValidAttackTarget)
            {
                ExecuteAttackOnPlayer();
                return;
            }

            // 在选择法术目标状态下，选择敌方玩家为目标
            if (_currentState == BattleUIState.SelectingTarget &&
                opponentHandArea != null &&
                opponentHandArea.IsValidAttackTarget)
            {
                ExecuteSpellOnPlayer();
            }
        }

        private void ExecuteSpellOnPlayer()
        {
            if (_selectedHandIndex < 0) return;

            // 获取卡牌信息
            var myState = gameController.GetLocalPlayerState();
            if (myState == null || _selectedHandIndex >= myState.hand.Count) return;

            var card = myState.hand[_selectedHandIndex];
            var cardData = _cardDatabase?.GetCardById(card.cardId);

            int opponentId = 1 - _localPlayerId;

            // 判断是否可以使用强化
            bool useEnhance = cardData != null && cardData.HasEnhance() && myState.mana >= cardData.enhanceCost;

            bool success = gameController.TryPlayCard(_selectedHandIndex, 0, -1, true, opponentId, useEnhance);

            if (success)
            {
                Debug.Log($"BattleUIController: 对敌方玩家使用法术 (强化: {useEnhance})");
            }

            CancelCurrentAction();
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

            // 获取卡牌信息
            var myState = gameController.GetLocalPlayerState();
            if (myState == null || handIndex >= myState.hand.Count) return;

            var card = myState.hand[handIndex];
            var cardData = _cardDatabase?.GetCardById(card.cardId);

            if (cardData == null)
            {
                Debug.LogWarning($"BattleUIController: 找不到卡牌数据 cardId={card.cardId}");
                return;
            }

            // 根据卡牌类型决定行为
            if (cardData.cardType == CardType.Spell)
            {
                // 法术卡：检查是否需要选择目标
                var validTargets = gameController.GetValidTargetsForCard(handIndex);

                if (cardData.requiresTarget)
                {
                    // 需要选择目标的法术：高亮可选目标
                    HighlightSpellTargets(validTargets);

                    // 如果目标类型包括敌方玩家（SingleEnemyOrPlayer），也高亮敌方玩家区域
                    // SingleEnemy 只能选择敌方随从，不能选择玩家
                    if (cardData.validTargets == TargetType.SingleEnemyOrPlayer)
                    {
                        if (opponentHandArea != null)
                        {
                            opponentHandArea.SetValidAttackTarget(true);
                        }
                    }

                    SetState(BattleUIState.SelectingTarget);
                    Debug.Log($"BattleUIController: 选中指向性法术{handIndex}，等待选择目标，有效目标数: {validTargets.Count}");
                }
                else
                {
                    // 不需要目标的法术：高亮整个场地（显示可释放区域）
                    HighlightSpellCastArea();
                    SetState(BattleUIState.SelectingTile); // 用这个状态表示"点击任意位置释放"
                    Debug.Log($"BattleUIController: 选中非指向性法术{handIndex}，点击任意位置释放");
                }
            }
            else if (cardData.cardType == CardType.Minion || cardData.cardType == CardType.Amulet)
            {
                // 随从/护符卡：高亮可放置的格子
                HighlightValidPlacementTiles();
                SetState(BattleUIState.SelectingTile);
                Debug.Log($"BattleUIController: 选中随从/护符{handIndex}");
            }
        }

        private void PlaceCardOnTile(TileSlotController tile)
        {
            if (_selectedHandIndex < 0) return;

            // 获取卡牌信息
            var myState = gameController.GetLocalPlayerState();
            if (myState == null || _selectedHandIndex >= myState.hand.Count) return;

            var card = myState.hand[_selectedHandIndex];
            var cardData = _cardDatabase?.GetCardById(card.cardId);

            if (cardData == null)
            {
                CancelCurrentAction();
                return;
            }

            // 判断是否可以使用强化（费用足够且有强化效果）
            bool useEnhance = cardData.HasEnhance() && myState.mana >= cardData.enhanceCost;

            // 检查是否需要选择手牌（军需官、饥饿的捕食者等）
            // 情况1: validTargets == HandCard
            // 情况2: 有 DiscardToGain 效果
            bool needsHandCardSelection = cardData.validTargets == TargetType.HandCard ||
                                          RequiresHandCardDiscard(cardData);

            if (needsHandCardSelection)
            {
                int placementTileIndex = cardData.cardType == CardType.Spell ? 0 : tile.tileIndex;
                int excludeInstanceId = card.instanceId;
                int capturedHandIndex = _selectedHandIndex; // 捕获当前选中的手牌索引
                bool capturedUseEnhance = useEnhance;

                // 获取过滤器（如果是 DiscardToGain 效果，检查是否需要过滤随从）
                Func<RuntimeCard, bool> filter = GetHandCardFilter(cardData);
                string title = cardData.validTargets == TargetType.HandCard ? "选择手牌" : "选择要丢弃的手牌";

                ShowHandCardSelection(
                    title,
                    cardData.description,
                    filter,
                    excludeInstanceId,
                    (selectedHandIndex) => {
                        // 选择完成后执行卡牌
                        bool success = gameController.TryPlayCard(capturedHandIndex, placementTileIndex, -1, false, -1, capturedUseEnhance, selectedHandIndex);
                        if (success)
                        {
                            Debug.Log($"BattleUIController: 打出卡牌 {cardData.cardName}，选择手牌 {selectedHandIndex}");
                        }
                        else
                        {
                            Debug.LogWarning($"BattleUIController: 打出卡牌失败");
                        }
                        CancelCurrentAction();
                    },
                    () => {
                        // 取消选择
                        Debug.Log("BattleUIController: 取消手牌选择");
                        CancelCurrentAction();
                    }
                );
                return;
            }

            // 检查是否需要选择敌方地格（倾盆大雨等）
            if (cardData.cardType == CardType.Spell && RequiresTileSelection(cardData))
            {
                int capturedHandIndex = _selectedHandIndex;
                bool capturedUseEnhance = useEnhance;
                int tileCount = GetRequiredTileCount(cardData);

                StartMultipleTileSelection(tileCount, true, (selectedTiles) => {
                    // 选择完成后执行法术，将选择的地格索引传递给执行器
                    bool success = gameController.TryPlayCard(capturedHandIndex, 0, -1, false, -1, capturedUseEnhance, -1, selectedTiles);

                    if (success)
                    {
                        Debug.Log($"BattleUIController: 释放法术 {cardData.cardName}，选择了{selectedTiles.Count}个地格");
                    }
                    else
                    {
                        Debug.LogWarning($"BattleUIController: 打出法术失败");
                    }
                });
                return;
            }

            // 检查是否需要选择目标（爆破专家等）
            if (cardData.requiresTarget && cardData.validTargets != TargetType.HandCard)
            {
                // 需要选择随从目标
                int placementTileIndex = cardData.cardType == CardType.Spell ? 0 : tile.tileIndex;
                int capturedHandIndex = _selectedHandIndex;
                bool capturedUseEnhance = useEnhance;

                // 获取有效目标
                var validTargets = gameController.GetValidTargetsForCard(capturedHandIndex);
                if (validTargets == null || validTargets.Count == 0)
                {
                    // 没有有效目标，但仍然可以打出卡牌（效果不触发）
                    bool playResult = gameController.TryPlayCard(capturedHandIndex, placementTileIndex, -1, false, -1, capturedUseEnhance);
                    if (playResult)
                    {
                        Debug.Log($"BattleUIController: 打出卡牌 {cardData.cardName}（无有效目标）");
                    }
                    CancelCurrentAction();
                    return;
                }

                // 高亮有效目标
                HighlightSpellTargets(validTargets);

                // 保存状态用于目标选择完成后
                _pendingCardHandIndex = capturedHandIndex;
                _pendingCardTileIndex = placementTileIndex;
                _pendingUseEnhance = capturedUseEnhance;
                _currentState = BattleUIState.SelectingTarget;

                Debug.Log($"BattleUIController: 等待选择目标 - {cardData.cardName}");
                return;
            }

            bool success;

            if (cardData.cardType == CardType.Spell)
            {
                // 法术卡不需要格子索引，tileIndex 设为 0
                success = gameController.TryPlayCard(_selectedHandIndex, 0, -1, false, -1, useEnhance);

                if (success)
                {
                    Debug.Log($"BattleUIController: 释放法术 {cardData.cardName} (强化: {useEnhance})");
                }
            }
            else
            {
                // 随从/护符需要放置在格子上
                success = gameController.TryPlayCard(_selectedHandIndex, tile.tileIndex, -1, false, -1, useEnhance);

                if (success)
                {
                    Debug.Log($"BattleUIController: 打出卡牌到格子{tile.tileIndex} (强化: {useEnhance})");
                }
            }

            if (!success)
            {
                Debug.LogWarning($"BattleUIController: 打出卡牌失败");
            }

            CancelCurrentAction();
        }

        /// <summary>
        /// 检查卡牌是否需要选择地格
        /// </summary>
        private bool RequiresTileSelection(CardData cardData)
        {
            if (cardData.effects == null) return false;

            foreach (var effect in cardData.effects)
            {
                if (effect.effectType == EffectType.ApplyTileEffect &&
                    effect.targetType == TargetType.EnemyTiles)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取需要选择的地格数量
        /// </summary>
        private int GetRequiredTileCount(CardData cardData)
        {
            if (cardData.effects == null) return 0;

            foreach (var effect in cardData.effects)
            {
                if (effect.effectType == EffectType.ApplyTileEffect &&
                    effect.parameters != null && effect.parameters.Count >= 3)
                {
                    if (int.TryParse(effect.parameters[2], out int count))
                    {
                        return count;
                    }
                }
            }
            return 1;
        }

        /// <summary>
        /// 检查卡牌是否需要选择手牌丢弃（DiscardToGain 效果）
        /// </summary>
        private bool RequiresHandCardDiscard(CardData cardData)
        {
            if (cardData.effects == null) return false;

            foreach (var effect in cardData.effects)
            {
                if (effect.effectType == EffectType.DiscardToGain &&
                    effect.trigger == EffectTrigger.OnPlay)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取手牌选择的过滤器（用于 DiscardToGain 效果的 filter:minion 等参数）
        /// </summary>
        private Func<RuntimeCard, bool> GetHandCardFilter(CardData cardData)
        {
            if (cardData.effects == null) return null;

            foreach (var effect in cardData.effects)
            {
                if (effect.effectType == EffectType.DiscardToGain &&
                    effect.trigger == EffectTrigger.OnPlay &&
                    effect.parameters != null)
                {
                    foreach (var param in effect.parameters)
                    {
                        if (param.StartsWith("filter:"))
                        {
                            string filterType = param.Substring(7); // 去掉 "filter:" 前缀
                            if (filterType == "minion")
                            {
                                // 只显示随从牌
                                return (RuntimeCard handCard) =>
                                {
                                    var data = _cardDatabase?.GetCardById(handCard.cardId);
                                    return data != null && data.cardType == CardType.Minion;
                                };
                            }
                            // 可以在这里添加更多过滤类型
                        }
                    }
                }
            }
            return null;
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

        private void ExecuteCardWithTarget(TileSlotController targetTile)
        {
            if (_pendingCardHandIndex < 0)
            {
                Debug.LogWarning("BattleUIController: 没有待执行的卡牌");
                CancelCurrentAction();
                return;
            }

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

            bool success = gameController.TryPlayCard(
                _pendingCardHandIndex,
                _pendingCardTileIndex,
                targetInstanceId,
                targetIsPlayer,
                targetPlayerId,
                _pendingUseEnhance
            );

            if (success)
            {
                Debug.Log($"BattleUIController: 打出卡牌并选择目标 {targetInstanceId}");
            }
            else
            {
                Debug.LogWarning("BattleUIController: 打出卡牌失败");
            }

            // 清除待执行状态
            _pendingCardHandIndex = -1;
            _pendingCardTileIndex = -1;
            _pendingUseEnhance = false;

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

        /// <summary>
        /// 高亮法术释放区域（整个场地亮绿光）
        /// </summary>
        private void HighlightSpellCastArea()
        {
            // 高亮所有我方格子
            if (myTiles != null)
            {
                foreach (var tile in myTiles)
                {
                    tile?.SetValidPlacementTarget(true);
                }
            }

            // 高亮所有对手格子
            if (opponentTiles != null)
            {
                foreach (var tile in opponentTiles)
                {
                    tile?.SetValidPlacementTarget(true);
                }
            }

            Debug.Log("BattleUIController: 高亮法术释放区域");
        }

        /// <summary>
        /// 高亮法术的有效目标
        /// </summary>
        private void HighlightSpellTargets(List<RuntimeCard> validTargets)
        {
            _validTargetInstanceIds.Clear();

            foreach (var target in validTargets)
            {
                _validTargetInstanceIds.Add(target.instanceId);

                // 在所有战场格子中找到目标
                if (myTiles != null)
                {
                    foreach (var tile in myTiles)
                    {
                        var occupant = tile?.GetOccupant();
                        if (occupant?.RuntimeCard != null && occupant.RuntimeCard.instanceId == target.instanceId)
                        {
                            tile.SetValidAttackTarget(true);
                            break;
                        }
                    }
                }

                if (opponentTiles != null)
                {
                    foreach (var tile in opponentTiles)
                    {
                        var occupant = tile?.GetOccupant();
                        if (occupant?.RuntimeCard != null && occupant.RuntimeCard.instanceId == target.instanceId)
                        {
                            tile.SetValidAttackTarget(true);
                            break;
                        }
                    }
                }
            }

            Debug.Log($"BattleUIController: 高亮法术目标，数量: {validTargets.Count}");
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

        #region Hand Card Selection

        /// <summary>
        /// 显示手牌选择UI
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="instruction">说明</param>
        /// <param name="filter">过滤函数</param>
        /// <param name="excludeInstanceId">排除的卡牌ID（排除自己）</param>
        /// <param name="onSelected">选择回调</param>
        /// <param name="onCancelled">取消回调</param>
        public void ShowHandCardSelection(string title, string instruction,
            Func<RuntimeCard, bool> filter = null, int excludeInstanceId = -1,
            System.Action<int> onSelected = null, System.Action onCancelled = null)
        {
            if (handCardSelectionUI == null)
            {
                Debug.LogError("BattleUIController: handCardSelectionUI 未设置!");
                onCancelled?.Invoke();
                return;
            }

            var myState = gameController?.GetLocalPlayerState();
            if (myState == null || myState.hand.Count == 0)
            {
                Debug.LogWarning("BattleUIController: 没有手牌可选择");
                onCancelled?.Invoke();
                return;
            }

            _handCardSelectionCallback = onSelected;
            _handCardSelectionCancelCallback = onCancelled;

            handCardSelectionUI.Show(myState.hand, title, instruction, filter, excludeInstanceId);
            Debug.Log($"BattleUIController: 显示手牌选择UI - {title}");
        }

        /// <summary>
        /// 隐藏手牌选择UI
        /// </summary>
        public void HideHandCardSelection()
        {
            if (handCardSelectionUI != null)
            {
                handCardSelectionUI.Hide();
            }
            _handCardSelectionCallback = null;
            _handCardSelectionCancelCallback = null;
        }

        /// <summary>
        /// 检查手牌选择UI是否可见
        /// </summary>
        public bool IsHandCardSelectionVisible()
        {
            return handCardSelectionUI != null && handCardSelectionUI.IsVisible();
        }

        private void OnHandCardSelected(int handIndex)
        {
            Debug.Log($"BattleUIController: 手牌选择完成 - 索引{handIndex}");
            var callback = _handCardSelectionCallback;
            _handCardSelectionCallback = null;
            _handCardSelectionCancelCallback = null;
            callback?.Invoke(handIndex);
        }

        private void OnHandCardSelectionCancelled()
        {
            Debug.Log("BattleUIController: 手牌选择取消");
            var callback = _handCardSelectionCancelCallback;
            _handCardSelectionCallback = null;
            _handCardSelectionCancelCallback = null;
            callback?.Invoke();
        }

        #endregion

        #region Multiple Tile Selection

        /// <summary>
        /// 开始多选地格模式（用于倾盆大雨等）
        /// </summary>
        /// <param name="count">需要选择的地格数量</param>
        /// <param name="selectEnemy">是否选择敌方地格</param>
        /// <param name="onComplete">完成回调，参数为选中的地格索引列表</param>
        public void StartMultipleTileSelection(int count, bool selectEnemy, System.Action<List<int>> onComplete)
        {
            _selectedTileIndices.Clear();
            _requiredTileCount = count;
            _selectingEnemyTiles = selectEnemy;
            _tileSelectionCallback = onComplete;

            // 高亮可选的地格
            HighlightSelectableTiles(selectEnemy);

            SetState(BattleUIState.SelectingMultipleTiles);
            Debug.Log($"BattleUIController: 开始多选地格模式，需要选择{count}个{(selectEnemy ? "敌方" : "我方")}地格");
        }

        /// <summary>
        /// 取消多选地格模式
        /// </summary>
        public void CancelMultipleTileSelection()
        {
            _selectedTileIndices.Clear();
            _requiredTileCount = 0;
            _tileSelectionCallback = null;

            ClearAllHighlights();
            SetState(BattleUIState.Idle);
            Debug.Log("BattleUIController: 取消多选地格模式");
        }

        /// <summary>
        /// 处理多选地格时的点击
        /// </summary>
        private void HandleMultipleTileSelection(TileSlotController tile)
        {
            // 检查是否是有效的地格（敌方/我方）
            if (_selectingEnemyTiles != tile.isOpponentTile)
            {
                Debug.Log($"BattleUIController: 无效地格（需要{(_selectingEnemyTiles ? "敌方" : "我方")}地格）");
                return;
            }

            int tileIndex = tile.tileIndex;

            // 检查是否已经选中
            if (_selectedTileIndices.Contains(tileIndex))
            {
                // 取消选中
                _selectedTileIndices.Remove(tileIndex);
                tile.SetValidPlacementTarget(true); // 恢复高亮但未选中状态
                Debug.Log($"BattleUIController: 取消选中地格{tileIndex}，当前选中{_selectedTileIndices.Count}/{_requiredTileCount}");
            }
            else
            {
                // 选中
                if (_selectedTileIndices.Count < _requiredTileCount)
                {
                    _selectedTileIndices.Add(tileIndex);
                    tile.SetValidAttackTarget(true); // 用攻击目标高亮表示已选中
                    Debug.Log($"BattleUIController: 选中地格{tileIndex}，当前选中{_selectedTileIndices.Count}/{_requiredTileCount}");

                    // 检查是否已选够
                    if (_selectedTileIndices.Count >= _requiredTileCount)
                    {
                        CompleteMultipleTileSelection();
                    }
                }
            }
        }

        /// <summary>
        /// 完成多选地格
        /// </summary>
        private void CompleteMultipleTileSelection()
        {
            var callback = _tileSelectionCallback;
            var selectedTiles = new List<int>(_selectedTileIndices);

            _selectedTileIndices.Clear();
            _requiredTileCount = 0;
            _tileSelectionCallback = null;

            ClearAllHighlights();
            SetState(BattleUIState.Idle);

            Debug.Log($"BattleUIController: 完成多选地格，选中了{selectedTiles.Count}个地格");
            callback?.Invoke(selectedTiles);
        }

        /// <summary>
        /// 高亮可选择的地格
        /// </summary>
        private void HighlightSelectableTiles(bool selectEnemy)
        {
            var tiles = selectEnemy ? opponentTiles : myTiles;
            if (tiles == null) return;

            foreach (var tile in tiles)
            {
                if (tile != null)
                {
                    tile.SetValidPlacementTarget(true);
                }
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
        SelectingMultipleTiles, // 选择多个地格（倾盆大雨等）
        WaitingForOpponent,     // 等待对手
        Animating               // 播放动画中
    }
}
