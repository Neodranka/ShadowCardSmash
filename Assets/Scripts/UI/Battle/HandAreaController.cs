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

        // 手牌列表
        private List<CardViewController> _handCards = new List<CardViewController>();
        private List<GameObject> _handCardObjects = new List<GameObject>(); // 用于跟踪所有卡牌对象（包括卡背）
        private CardViewController _hoveredCard;
        private CardViewController _selectedCard;
        private int _selectedHandIndex = -1;

        // 卡牌数据库引用
        private ICardDatabase _cardDatabase;

        // 事件
        public event Action<int> OnCardClicked;      // 参数：手牌索引
        public event Action<int> OnCardHovered;
        public event Action OnCardUnhovered;
        public event Action<int> OnCardRightClicked; // 显示详情

        // 属性
        public int CardCount => _handCards.Count;
        public CardViewController SelectedCard => _selectedCard;
        public int SelectedHandIndex => _selectedHandIndex;

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
            // 清除现有手牌
            ClearHand();

            // 创建新的手牌显示
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                CreateCardView(card, i, false);
            }

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
            if (handContainer == null) return null;

            // 选择预制体（对手显示卡背，我方显示正面）
            GameObject prefab = isOpponentHand ? cardBackPrefab : cardPrefab;
            if (prefab == null)
            {
                Debug.LogWarning("HandAreaController: 缺少卡牌预制体");
                return null;
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

            _selectedCard = null;
            _selectedHandIndex = -1;
            _hoveredCard = null;
        }

        private void PlayDrawAnimation(CardViewController cardView)
        {
            // TODO: 实现抽牌动画（从牌库飞入手牌）
            Debug.Log($"HandAreaController: 播放抽牌动画");
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
                var pos = rectTransform.anchoredPosition;
                pos.y = hoverRaiseAmount;
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

            // 放下卡牌
            var rectTransform = cardView.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                var pos = rectTransform.anchoredPosition;
                pos.y = 0;
                rectTransform.anchoredPosition = pos;
            }

            OnCardUnhovered?.Invoke();
        }

        private void HandleCardRightClicked(CardViewController cardView)
        {
            OnCardRightClicked?.Invoke(cardView.HandIndex);
        }

        #endregion

        void OnDestroy()
        {
            ClearHand();
        }
    }
}
