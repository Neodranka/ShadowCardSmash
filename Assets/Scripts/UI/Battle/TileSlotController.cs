using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using ShadowCardSmash.Core.Data;

namespace ShadowCardSmash.UI.Battle
{
    /// <summary>
    /// 战场格子控制器 - 管理单个战场格子
    /// </summary>
    public class TileSlotController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IDropHandler
    {
        [Header("UI References")]
        public Image tileBackground;
        public Transform occupantHolder;
        public GameObject tileEffectIndicator;
        public Image tileEffectIcon;
        public TextMeshProUGUI tileEffectDurationText;
        public GameObject validTargetHighlight;
        public GameObject validPlacementHighlight;
        public GameObject invalidTargetIndicator;

        [Header("Settings")]
        public int tileIndex;
        public bool isOpponentTile;

        [Header("Colors")]
        public Color normalColor = new Color(0.55f, 0.45f, 0.33f, 0.5f);
        public Color highlightedColor = new Color(0.4f, 0.8f, 0.4f, 0.7f);
        public Color invalidColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);

        // 当前占据的单位
        private CardViewController _currentOccupant;
        private bool _isValidTarget;
        private bool _isValidPlacement;
        #pragma warning disable CS0414
        private bool _isHovered; // 保留用于未来扩展
        #pragma warning restore CS0414

        // 事件
        public event Action<TileSlotController> OnTileClicked;
        public event Action<TileSlotController> OnTileRightClicked;
        public event Action<TileSlotController> OnTileHovered;
        public event Action<TileSlotController> OnTileUnhovered;
        public event Action<TileSlotController, CardViewController> OnCardDropped;

        // 属性
        public CardViewController CurrentOccupant => _currentOccupant;
        public bool IsEmpty => _currentOccupant == null;
        public bool IsValidTarget => _isValidTarget;
        public bool IsValidPlacement => _isValidPlacement;

        void Awake()
        {
            // 初始化状态
            SetIndicatorActive(validTargetHighlight, false);
            SetIndicatorActive(validPlacementHighlight, false);
            SetIndicatorActive(invalidTargetIndicator, false);
            SetIndicatorActive(tileEffectIndicator, false);
        }

        /// <summary>
        /// 放置单位
        /// </summary>
        public void PlaceUnit(CardViewController cardView)
        {
            if (_currentOccupant != null)
            {
                Debug.LogWarning($"TileSlotController: 格子{tileIndex}已有单位，先移除");
                RemoveUnit();
            }

            _currentOccupant = cardView;

            if (cardView != null)
            {
                // 设置父物体
                cardView.transform.SetParent(occupantHolder, false);

                // 重置位置和缩放
                var rectTransform = cardView.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = Vector2.zero;
                    rectTransform.localScale = Vector3.one;
                }
            }
        }

        /// <summary>
        /// 移除单位
        /// </summary>
        public CardViewController RemoveUnit()
        {
            var removed = _currentOccupant;
            _currentOccupant = null;
            return removed;
        }

        /// <summary>
        /// 获取当前单位
        /// </summary>
        public CardViewController GetOccupant()
        {
            return _currentOccupant;
        }

        /// <summary>
        /// 设置为有效放置目标
        /// </summary>
        public void SetValidPlacementTarget(bool valid)
        {
            _isValidPlacement = valid;
            SetIndicatorActive(validPlacementHighlight, valid);

            if (tileBackground != null)
            {
                tileBackground.color = valid ? highlightedColor : normalColor;
            }
        }

        /// <summary>
        /// 设置为有效攻击目标
        /// </summary>
        public void SetValidAttackTarget(bool valid)
        {
            _isValidTarget = valid;
            SetIndicatorActive(validTargetHighlight, valid);
        }

        /// <summary>
        /// 设置为无效目标
        /// </summary>
        public void SetInvalidTarget(bool invalid)
        {
            SetIndicatorActive(invalidTargetIndicator, invalid);

            if (tileBackground != null && invalid)
            {
                tileBackground.color = invalidColor;
            }
        }

        /// <summary>
        /// 清除所有高亮
        /// </summary>
        public void ClearHighlights()
        {
            _isValidTarget = false;
            _isValidPlacement = false;

            SetIndicatorActive(validTargetHighlight, false);
            SetIndicatorActive(validPlacementHighlight, false);
            SetIndicatorActive(invalidTargetIndicator, false);

            if (tileBackground != null)
            {
                tileBackground.color = normalColor;
            }
        }

        /// <summary>
        /// 显示格子效果（TileEffect类型）
        /// </summary>
        public void ShowTileEffect(TileEffect effect)
        {
            bool hasEffect = effect != null && effect.tileEffectType != TileEffectType.None && effect.remainingTurns != 0;
            SetIndicatorActive(tileEffectIndicator, hasEffect);

            if (hasEffect)
            {
                // 设置效果图标颜色（根据效果类型）
                if (tileEffectIcon != null)
                {
                    switch (effect.tileEffectType)
                    {
                        case TileEffectType.DownpourRain:
                            tileEffectIcon.color = new Color(0.3f, 0.5f, 0.9f, 0.8f); // 蓝色表示雨
                            break;
                        default:
                            tileEffectIcon.color = new Color(0.8f, 0.8f, 0.2f, 0.8f); // 默认黄色
                            break;
                    }
                }

                // 显示剩余回合数
                if (tileEffectDurationText != null)
                {
                    tileEffectDurationText.text = effect.remainingTurns > 0 ? effect.remainingTurns.ToString() : "";
                }

                Debug.Log($"TileSlotController: 格子{tileIndex}显示地格效果 - {effect.tileEffectType}, 剩余{effect.remainingTurns}回合");
            }
        }

        /// <summary>
        /// 根据TileState显示地格效果
        /// </summary>
        public void UpdateTileEffectDisplay(TileState tileState)
        {
            if (tileState == null)
            {
                SetIndicatorActive(tileEffectIndicator, false);
                return;
            }

            // 查找活跃的地格效果
            TileEffect activeEffect = null;
            if (tileState.effects != null)
            {
                foreach (var effect in tileState.effects)
                {
                    if (effect.tileEffectType != TileEffectType.None && effect.remainingTurns != 0)
                    {
                        activeEffect = effect;
                        break;
                    }
                }
            }

            ShowTileEffect(activeEffect);
        }

        /// <summary>
        /// 清除地格效果显示
        /// </summary>
        public void ClearTileEffectDisplay()
        {
            SetIndicatorActive(tileEffectIndicator, false);
        }

        /// <summary>
        /// 刷新显示
        /// </summary>
        public void RefreshDisplay()
        {
            if (_currentOccupant != null)
            {
                _currentOccupant.RefreshDisplay();
            }
        }

        private void SetIndicatorActive(GameObject indicator, bool active)
        {
            if (indicator != null)
            {
                indicator.SetActive(active);
            }
        }

        #region Event Handlers

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                OnTileClicked?.Invoke(this);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnTileRightClicked?.Invoke(this);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            OnTileHovered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            OnTileUnhovered?.Invoke(this);
        }

        public void OnDrop(PointerEventData eventData)
        {
            // 支持拖放卡牌
            var cardView = eventData.pointerDrag?.GetComponent<CardViewController>();
            if (cardView != null)
            {
                OnCardDropped?.Invoke(this, cardView);
            }
        }

        #endregion
    }
}
