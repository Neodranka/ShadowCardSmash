using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Effects;

namespace ShadowCardSmash.UI.Battle
{
    /// <summary>
    /// 手牌区域控制器
    /// </summary>
    public class HandAreaController : MonoBehaviour
    {
        [Header("Settings")]
        public bool isOpponentHand;
        public Transform handContainer;
        public GameObject cardPrefab;
        public GameObject cardBackPrefab;

        [Header("Layout Settings")]
        public float cardSpacing = 10f;
        public float maxHandWidth = 1200f;
        public float hoverRaiseAmount = 30f;
        public float selectedScale = 1.2f;
        public float cardWidth = 160f;

        [Header("Animation Settings")]
        public float drawAnimationDuration = 0.3f;
        public float rearrangeAnimationDuration = 0.2f;

        [Header("Attack Target Overlay")]
        public Image attackTargetOverlay;
        public Button attackTargetButton;  // 用于接收点击的Button
        public Color attackTargetColor = new Color(1f, 0f, 0f, 0.3f);

        // 手牌列表
        private List<CardViewController> _handCards = new List<CardViewController>();
        private List<GameObject> _handCardObjects = new List<GameObject>(); // 用于跟踪所有卡牌对象（包括卡背）
        private Dictionary<CardViewController, Vector2> _cardOriginalPositions = new Dictionary<CardViewController, Vector2>(); // 保存卡牌原始位置
        private CardViewController _hoveredCard;
        private CardViewController _selectedCard;
        private int _selectedHandIndex = -1;

        // 卡牌数据库引用
        private ICardDatabase _cardDatabase;

        // 内部状态
        private bool _isValidAttackTarget;

        // 事件
        public event Action<int> OnCardClicked;      // 参数：手牌索引
        public event Action<int> OnCardHovered;
        public event Action OnCardUnhovered;
        public event Action<int> OnCardRightClicked; // 显示详情
        public event Action OnAreaClicked;           // 手牌区域被点击（用于攻击玩家）

        // 属性
        public int CardCount => _handCards.Count;
        public bool IsValidAttackTarget => _isValidAttackTarget;
        public CardViewController SelectedCard => _selectedCard;
        public int SelectedHandIndex => _selectedHandIndex;

        void Awake()
        {
            // 绑定攻击目标按钮点击事件
            if (attackTargetButton != null)
            {
                attackTargetButton.onClick.AddListener(OnHandAreaClicked);
            }
        }

        /// <summary>
        /// 设置卡牌数据库引用
        /// </summary>
        public void SetCardDatabase(ICardDatabase cardDatabase)
        {
            _cardDatabase = cardDatabase;
        }

        /// <summary>
        /// 设置手牌（完整刷新）
        /// </summary>
        public void SetHand(List<RuntimeCard> cards)
        {
            Debug.Log($"HandAreaController.SetHand: 设置 {cards?.Count ?? 0} 张手牌, isOpponent={isOpponentHand}, cardPrefab={cardPrefab != null}, handContainer={handContainer != null}");

            // 检查并修复 handContainer 大小
            if (handContainer != null)
            {
                var containerRect = handContainer.GetComponent<RectTransform>();
                if (containerRect != null && containerRect.sizeDelta == Vector2.zero && containerRect.anchorMin == containerRect.anchorMax)
                {
                    Debug.Log("HandAreaController.SetHand: 检测到容器大小为0，自动拉伸填充");
                    containerRect.anchorMin = Vector2.zero;
                    containerRect.anchorMax = Vector2.one;
                    containerRect.offsetMin = Vector2.zero;
                    containerRect.offsetMax = Vector2.zero;
                }
            }

            // 清除现有手牌
            ClearHand();

            if (cards == null || cards.Count == 0)
            {
                Debug.Log("HandAreaController.SetHand: 手牌列表为空");
                return;
            }

            // 创建新的手牌显示
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                var cardView = CreateCardView(card, i, false);
                Debug.Log($"  创建手牌 {i}: cardId={card.cardId}, cardView={cardView != null}");
            }

            Debug.Log($"HandAreaController.SetHand: 创建完成, _handCards.Count={_handCards.Count}");

            // 刷新布局
            RefreshLayout();
        }

        /// <summary>
        /// 添加一张牌（带动画）
        /// </summary>
        public void AddCard(RuntimeCard card, bool animate = true)
        {
            int newIndex = _handCards.Count;
            var cardView = CreateCardView(card, newIndex, animate);

            if (animate)
            {
                // TODO: 播放抽牌动画
                PlayDrawAnimation(cardView);
            }

            RefreshLayout();
        }

        /// <summary>
        /// 移除一张牌（带动画）
        /// </summary>
        public void RemoveCard(int instanceId, bool animate = true)
        {
            CardViewController toRemove = null;
            int removeIndex = -1;

            for (int i = 0; i < _handCards.Count; i++)
            {
                if (_handCards[i].RuntimeCard != null && _handCards[i].RuntimeCard.instanceId == instanceId)
                {
                    toRemove = _handCards[i];
                    removeIndex = i;
                    break;
                }
            }

            if (toRemove != null)
            {
                _handCards.RemoveAt(removeIndex);

                if (animate)
                {
                    // 播放移除动画后销毁
                    toRemove.PlayDeathAnimation(() =>
                    {
                        Destroy(toRemove.gameObject);
                    });
                }
                else
                {
                    Destroy(toRemove.gameObject);
                }

                // 更新剩余卡牌的索引
                for (int i = 0; i < _handCards.Count; i++)
                {
                    _handCards[i].HandIndex = i;
                }

                RefreshLayout();
            }
        }

        /// <summary>
        /// 移除指定索引的手牌
        /// </summary>
        public void RemoveCardAtIndex(int handIndex, bool animate = true)
        {
            if (handIndex < 0 || handIndex >= _handCards.Count) return;

            var toRemove = _handCards[handIndex];
            _handCards.RemoveAt(handIndex);

            if (animate)
            {
                toRemove.PlayDeathAnimation(() =>
                {
                    Destroy(toRemove.gameObject);
                });
            }
            else
            {
                Destroy(toRemove.gameObject);
            }

            // 更新索引
            for (int i = 0; i < _handCards.Count; i++)
            {
                _handCards[i].HandIndex = i;
            }

            RefreshLayout();
        }

        /// <summary>
        /// 更新手牌布局
        /// </summary>
        public void RefreshLayout()
        {
            if (handContainer == null || _handCards.Count == 0) return;

            // 计算总宽度和间距
            float totalWidth = _handCards.Count * cardWidth + (_handCards.Count - 1) * cardSpacing;

            // 如果超出最大宽度，减小间距
            float actualSpacing = cardSpacing;
            if (totalWidth > maxHandWidth)
            {
                actualSpacing = (maxHandWidth - _handCards.Count * cardWidth) / (_handCards.Count - 1);
                totalWidth = maxHandWidth;
            }

            // 计算起始位置（居中）
            float startX = -totalWidth / 2 + cardWidth / 2;

            // 设置每张卡牌的位置
            for (int i = 0; i < _handCards.Count; i++)
            {
                var cardView = _handCards[i];
                var rectTransform = cardView.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    float targetX = startX + i * (cardWidth + actualSpacing);
                    Vector2 targetPos = new Vector2(targetX, 0);

                    // 可以加入平滑移动动画
                    rectTransform.anchoredPosition = targetPos;
                }

                // 设置层级（后面的卡牌在上层）
                cardView.transform.SetSiblingIndex(i);
            }
        }

        /// <summary>
        /// 高亮可使用的卡牌
        /// </summary>
        public void HighlightPlayableCards(List<int> playableIndices)
        {
            for (int i = 0; i < _handCards.Count; i++)
            {
                bool isPlayable = playableIndices.Contains(i);
                _handCards[i].SetPlayable(isPlayable);
            }
        }

        /// <summary>
        /// 清除所有高亮
        /// </summary>
        public void ClearHighlights()
        {
            foreach (var card in _handCards)
            {
                card.SetPlayable(false);
                card.SetSelected(false);
            }
            _selectedCard = null;
            _selectedHandIndex = -1;
        }

        /// <summary>
        /// 选中指定索引的手牌
        /// </summary>
        public void SelectCard(int handIndex)
        {
            // 取消之前的选中
            if (_selectedCard != null)
            {
                _selectedCard.SetSelected(false);
            }

            if (handIndex >= 0 && handIndex < _handCards.Count)
            {
                _selectedCard = _handCards[handIndex];
                _selectedHandIndex = handIndex;
                _selectedCard.SetSelected(true);
            }
            else
            {
                _selectedCard = null;
                _selectedHandIndex = -1;
            }
        }

        /// <summary>
        /// 取消选中
        /// </summary>
        public void DeselectCard()
        {
            if (_selectedCard != null)
            {
                _selectedCard.SetSelected(false);
            }
            _selectedCard = null;
            _selectedHandIndex = -1;
        }

        /// <summary>
        /// 获取指定索引的卡牌视图
        /// </summary>
        public CardViewController GetCardAtIndex(int index)
        {
            if (index >= 0 && index < _handCards.Count)
            {
                return _handCards[index];
            }
            return null;
        }

        /// <summary>
        /// 获取所有手牌视图
        /// </summary>
        public List<CardViewController> GetAllCards()
        {
            return new List<CardViewController>(_handCards);
        }

        private CardViewController CreateCardView(RuntimeCard card, int index, bool animate)
        {
            if (handContainer == null)
            {
                Debug.LogError("HandAreaController.CreateCardView: handContainer 为空!");
                return null;
            }

            // 选择预制体（对手显示卡背，我方显示正面）
            GameObject prefab = isOpponentHand ? cardBackPrefab : cardPrefab;
            if (prefab == null)
            {
                Debug.LogError($"HandAreaController.CreateCardView: 缺少卡牌预制体! isOpponent={isOpponentHand}, cardPrefab={cardPrefab != null}, cardBackPrefab={cardBackPrefab != null}");
                // 尝试动态创建一个简单的卡牌视图
                prefab = CreateFallbackCardPrefab();
                if (prefab == null) return null;
            }

            var cardObj = Instantiate(prefab, handContainer);
            _handCardObjects.Add(cardObj); // 跟踪所有卡牌对象

            var cardView = cardObj.GetComponent<CardViewController>();

            if (cardView != null)
            {
                // 设置卡牌数据
                CardData cardData = _cardDatabase?.GetCardById(card.cardId);
                if (cardData != null)
                {
                    cardView.SetRuntimeCard(card, cardData);
                }
                cardView.HandIndex = index;

                // 订阅事件
                cardView.OnCardClicked += HandleCardClicked;
                cardView.OnCardHovered += HandleCardHovered;
                cardView.OnCardUnhovered += HandleCardUnhovered;
                cardView.OnCardRightClicked += HandleCardRightClicked;

                _handCards.Add(cardView);
            }

            return cardView;
        }

        private void ClearHand()
        {
            // 取消订阅事件
            foreach (var card in _handCards)
            {
                if (card != null)
                {
                    card.OnCardClicked -= HandleCardClicked;
                    card.OnCardHovered -= HandleCardHovered;
                    card.OnCardUnhovered -= HandleCardUnhovered;
                    card.OnCardRightClicked -= HandleCardRightClicked;
                }
            }
            _handCards.Clear();

            // 销毁所有卡牌对象（包括卡背）
            foreach (var cardObj in _handCardObjects)
            {
                if (cardObj != null)
                {
                    Destroy(cardObj);
                }
            }
            _handCardObjects.Clear();
            _cardOriginalPositions.Clear();

            _selectedCard = null;
            _selectedHandIndex = -1;
            _hoveredCard = null;
        }

        private void PlayDrawAnimation(CardViewController cardView)
        {
            // TODO: 实现抽牌动画（从牌库飞入手牌）
            Debug.Log($"HandAreaController: 播放抽牌动画");
        }

        /// <summary>
        /// 创建备用卡牌预制体（当没有预制体时使用）
        /// </summary>
        private GameObject CreateFallbackCardPrefab()
        {
            Debug.Log("HandAreaController: 创建备用卡牌预制体");

            var cardObj = new GameObject("FallbackCard");

            // 添加 RectTransform
            var rect = cardObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(cardWidth, 220);

            // 添加背景图片
            var bg = cardObj.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.3f, 0.25f, 0.2f, 1f);
            bg.raycastTarget = true;

            // 添加按钮组件
            var button = cardObj.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = bg;

            // 创建费用文本
            var costObj = new GameObject("CostText");
            costObj.transform.SetParent(cardObj.transform, false);
            var costRect = costObj.AddComponent<RectTransform>();
            costRect.anchorMin = new Vector2(0, 1);
            costRect.anchorMax = new Vector2(0, 1);
            costRect.pivot = new Vector2(0, 1);
            costRect.anchoredPosition = new Vector2(5, -5);
            costRect.sizeDelta = new Vector2(30, 30);
            var costText = costObj.AddComponent<TMPro.TextMeshProUGUI>();
            costText.text = "0";
            costText.fontSize = 18;
            costText.fontStyle = TMPro.FontStyles.Bold;
            costText.alignment = TMPro.TextAlignmentOptions.Center;
            costText.color = Color.white;
            costText.raycastTarget = false;

            // 创建名称文本
            var nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(cardObj.transform, false);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameRect.anchoredPosition = new Vector2(0, 0);
            nameRect.sizeDelta = new Vector2(140, 60);
            var nameText = nameObj.AddComponent<TMPro.TextMeshProUGUI>();
            nameText.text = "卡牌";
            nameText.fontSize = 14;
            nameText.alignment = TMPro.TextAlignmentOptions.Center;
            nameText.color = Color.white;
            nameText.raycastTarget = false;

            // 创建攻击力文本
            var atkObj = new GameObject("AttackText");
            atkObj.transform.SetParent(cardObj.transform, false);
            var atkRect = atkObj.AddComponent<RectTransform>();
            atkRect.anchorMin = new Vector2(0, 0);
            atkRect.anchorMax = new Vector2(0, 0);
            atkRect.pivot = new Vector2(0, 0);
            atkRect.anchoredPosition = new Vector2(5, 5);
            atkRect.sizeDelta = new Vector2(30, 30);
            var atkText = atkObj.AddComponent<TMPro.TextMeshProUGUI>();
            atkText.text = "0";
            atkText.fontSize = 16;
            atkText.fontStyle = TMPro.FontStyles.Bold;
            atkText.alignment = TMPro.TextAlignmentOptions.Center;
            atkText.color = Color.yellow;
            atkText.raycastTarget = false;

            // 创建生命值文本
            var hpObj = new GameObject("HealthText");
            hpObj.transform.SetParent(cardObj.transform, false);
            var hpRect = hpObj.AddComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(1, 0);
            hpRect.anchorMax = new Vector2(1, 0);
            hpRect.pivot = new Vector2(1, 0);
            hpRect.anchoredPosition = new Vector2(-5, 5);
            hpRect.sizeDelta = new Vector2(30, 30);
            var hpText = hpObj.AddComponent<TMPro.TextMeshProUGUI>();
            hpText.text = "0";
            hpText.fontSize = 16;
            hpText.fontStyle = TMPro.FontStyles.Bold;
            hpText.alignment = TMPro.TextAlignmentOptions.Center;
            hpText.color = Color.red;
            hpText.raycastTarget = false;

            // 添加 CardViewController 组件
            var cardView = cardObj.AddComponent<CardViewController>();
            cardView.costText = costText;
            cardView.cardNameText = nameText;
            cardView.attackText = atkText;
            cardView.healthText = hpText;

            return cardObj;
        }

        #region Event Handlers

        private void HandleCardClicked(CardViewController cardView)
        {
            if (isOpponentHand) return; // 对手手牌不可点击

            OnCardClicked?.Invoke(cardView.HandIndex);
        }

        private void HandleCardHovered(CardViewController cardView)
        {
            _hoveredCard = cardView;

            // 悬停时抬起卡牌
            var rectTransform = cardView.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 保存原始位置（如果还没保存）
                if (!_cardOriginalPositions.ContainsKey(cardView))
                {
                    _cardOriginalPositions[cardView] = rectTransform.anchoredPosition;
                }

                var pos = rectTransform.anchoredPosition;
                pos.y = _cardOriginalPositions[cardView].y + hoverRaiseAmount;
                rectTransform.anchoredPosition = pos;
            }

            OnCardHovered?.Invoke(cardView.HandIndex);
        }

        private void HandleCardUnhovered(CardViewController cardView)
        {
            if (_hoveredCard == cardView)
            {
                _hoveredCard = null;
            }

            // 恢复到原始位置
            var rectTransform = cardView.GetComponent<RectTransform>();
            if (rectTransform != null && _cardOriginalPositions.TryGetValue(cardView, out var originalPos))
            {
                rectTransform.anchoredPosition = originalPos;
            }

            OnCardUnhovered?.Invoke();
        }

        private void HandleCardRightClicked(CardViewController cardView)
        {
            OnCardRightClicked?.Invoke(cardView.HandIndex);
        }

        #endregion

        #region Attack Target

        /// <summary>
        /// 设置为有效攻击目标（用于攻击玩家）
        /// </summary>
        public void SetValidAttackTarget(bool valid)
        {
            _isValidAttackTarget = valid;

            if (attackTargetOverlay != null)
            {
                var color = attackTargetColor;
                color.a = valid ? attackTargetColor.a : 0f;
                attackTargetOverlay.color = color;
            }
        }

        /// <summary>
        /// 清除攻击目标高亮
        /// </summary>
        public void ClearAttackTargetHighlight()
        {
            SetValidAttackTarget(false);
        }

        /// <summary>
        /// 处理手牌区域点击（由外部Button调用）
        /// </summary>
        public void OnHandAreaClicked()
        {
            if (_isValidAttackTarget)
            {
                OnAreaClicked?.Invoke();
            }
        }

        #endregion

        void OnDestroy()
        {
            ClearHand();
        }
    }
}
