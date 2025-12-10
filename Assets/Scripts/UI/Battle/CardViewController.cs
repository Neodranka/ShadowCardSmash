using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using ShadowCardSmash.Core.Data;

namespace ShadowCardSmash.UI.Battle
{
    /// <summary>
    /// 卡牌视图控制器 - 管理单张卡牌的UI显示
    /// </summary>
    public class CardViewController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("UI References")]
        public Image cardFrame;
        public Image cardArt;
        public TextMeshProUGUI cardNameText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI attackText;
        public TextMeshProUGUI healthText;
        public GameObject attackHealthGroup;

        [Header("State Indicators")]
        public GameObject evolvedIndicator;
        public GameObject summoningSicknessIndicator;
        public GameObject canAttackGlow;
        public GameObject selectionHighlight;
        public GameObject playableHighlight;

        [Header("Frame Colors")]
        public Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);
        public Color silverColor = new Color(0.75f, 0.75f, 0.75f);
        public Color goldColor = new Color(1f, 0.84f, 0f);
        public Color legendaryColor = new Color(1f, 0.41f, 0.71f);

        [Header("Stat Colors")]
        public Color normalStatColor = Color.white;
        public Color buffedStatColor = Color.green;
        public Color debuffedStatColor = Color.red;

        // 数据
        private CardData _cardData;
        private RuntimeCard _runtimeCard;
        private int _handIndex = -1;
        private bool _isSelected;
        // ReSharper disable once NotAccessedField.Local
        private bool _isHovered; // 保留用于未来扩展（如悬停效果）
        private bool _isPlayable;

        // 事件
        public event Action<CardViewController> OnCardClicked;
        public event Action<CardViewController> OnCardHovered;
        public event Action<CardViewController> OnCardUnhovered;
        public event Action<CardViewController> OnCardRightClicked;

        // 属性
        public CardData CardData => _cardData;
        public RuntimeCard RuntimeCard => _runtimeCard;
        public int HandIndex { get => _handIndex; set => _handIndex = value; }
        public bool IsSelected => _isSelected;
        public bool IsPlayable => _isPlayable;

        void Awake()
        {
            // 确保指示器初始隐藏
            SetIndicatorActive(evolvedIndicator, false);
            SetIndicatorActive(summoningSicknessIndicator, false);
            SetIndicatorActive(canAttackGlow, false);
            SetIndicatorActive(selectionHighlight, false);
            SetIndicatorActive(playableHighlight, false);
        }

        /// <summary>
        /// 设置卡牌数据（用于手牌/收藏显示）
        /// </summary>
        public void SetCardData(CardData data)
        {
            _cardData = data;
            _runtimeCard = null;
            RefreshDisplay();
        }

        /// <summary>
        /// 设置运行时卡牌数据（用于战场显示）
        /// </summary>
        public void SetRuntimeCard(RuntimeCard runtime, CardData baseData)
        {
            _runtimeCard = runtime;
            _cardData = baseData;
            RefreshDisplay();
        }

        /// <summary>
        /// 更新显示
        /// </summary>
        public void RefreshDisplay()
        {
            if (_cardData == null) return;

            // 设置卡牌名称
            if (cardNameText != null)
            {
                cardNameText.text = _cardData.cardName;
            }

            // 设置费用
            if (costText != null)
            {
                costText.text = _cardData.cost.ToString();
            }

            // 设置边框颜色
            if (cardFrame != null)
            {
                cardFrame.color = GetRarityColor(_cardData.rarity);
            }

            // 根据卡牌类型设置攻击/生命值
            if (_cardData.cardType == CardType.Minion)
            {
                SetIndicatorActive(attackHealthGroup, true);

                if (_runtimeCard != null)
                {
                    // 运行时卡牌显示当前值
                    UpdateStatText(attackText, _runtimeCard.currentAttack, _cardData.attack);
                    UpdateStatText(healthText, _runtimeCard.currentHealth, _cardData.health);

                    // 更新状态指示器（只有在战场上才显示，手牌中不显示）
                    // 手牌中的卡牌 _handIndex >= 0，战场上的卡牌 _handIndex 保持默认值 -1
                    bool isInHand = _handIndex >= 0;
                    SetIndicatorActive(evolvedIndicator, !isInHand && _runtimeCard.isEvolved);
                    SetIndicatorActive(summoningSicknessIndicator, !isInHand && !_runtimeCard.canAttack && !_runtimeCard.isEvolved);
                    SetIndicatorActive(canAttackGlow, !isInHand && _runtimeCard.canAttack);
                }
                else
                {
                    // 静态卡牌显示基础值
                    if (attackText != null) attackText.text = _cardData.attack.ToString();
                    if (healthText != null) healthText.text = _cardData.health.ToString();
                }
            }
            else if (_cardData.cardType == CardType.Amulet)
            {
                // 护符显示倒计时
                SetIndicatorActive(attackHealthGroup, _cardData.countdown > 0);
                if (_cardData.countdown > 0)
                {
                    if (attackText != null)
                    {
                        attackText.text = "";
                        attackText.gameObject.SetActive(false);
                    }
                    if (healthText != null)
                    {
                        int countdown = _runtimeCard != null ? _runtimeCard.currentHealth : _cardData.countdown;
                        healthText.text = countdown.ToString();
                    }
                }
            }
            else
            {
                // 法术隐藏攻击/生命值
                SetIndicatorActive(attackHealthGroup, false);
            }
        }

        /// <summary>
        /// 更新属性文本（带颜色指示增减）
        /// </summary>
        private void UpdateStatText(TextMeshProUGUI text, int current, int baseValue)
        {
            if (text == null) return;

            text.text = current.ToString();

            if (current > baseValue)
            {
                text.color = buffedStatColor;
            }
            else if (current < baseValue)
            {
                text.color = debuffedStatColor;
            }
            else
            {
                text.color = normalStatColor;
            }
        }

        /// <summary>
        /// 获取稀有度对应的颜色
        /// </summary>
        private Color GetRarityColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Bronze: return bronzeColor;
                case Rarity.Silver: return silverColor;
                case Rarity.Gold: return goldColor;
                case Rarity.Legendary: return legendaryColor;
                default: return bronzeColor;
            }
        }

        /// <summary>
        /// 设置选中状态
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            SetIndicatorActive(selectionHighlight, selected);
        }

        /// <summary>
        /// 设置可攻击状态
        /// </summary>
        public void SetCanAttack(bool canAttack)
        {
            SetIndicatorActive(canAttackGlow, canAttack);
        }

        /// <summary>
        /// 设置可使用状态
        /// </summary>
        public void SetPlayable(bool playable)
        {
            _isPlayable = playable;
            SetIndicatorActive(playableHighlight, playable);
        }

        /// <summary>
        /// 播放进化动画
        /// </summary>
        public void PlayEvolveAnimation()
        {
            // TODO: 实现进化动画
            SetIndicatorActive(evolvedIndicator, true);
            Debug.Log($"CardViewController: 播放进化动画 - {_cardData?.cardName}");
        }

        /// <summary>
        /// 播放受伤动画
        /// </summary>
        public void PlayDamageAnimation(int damage)
        {
            // TODO: 实现受伤动画（闪红、数字飘动等）
            Debug.Log($"CardViewController: 播放受伤动画 - {_cardData?.cardName} -{damage}");
        }

        /// <summary>
        /// 播放治疗动画
        /// </summary>
        public void PlayHealAnimation(int amount)
        {
            // TODO: 实现治疗动画
            Debug.Log($"CardViewController: 播放治疗动画 - {_cardData?.cardName} +{amount}");
        }

        /// <summary>
        /// 播放死亡动画
        /// </summary>
        public void PlayDeathAnimation(Action onComplete = null)
        {
            // TODO: 实现死亡动画
            Debug.Log($"CardViewController: 播放死亡动画 - {_cardData?.cardName}");
            onComplete?.Invoke();
        }

        private void SetIndicatorActive(GameObject indicator, bool active)
        {
            if (indicator != null)
            {
                indicator.SetActive(active);
            }
        }

        #region Event Handlers

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            OnCardHovered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            OnCardUnhovered?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 检查是否在战场格子上（父级或祖父级有TileSlotController）
            var parentTile = GetComponentInParent<TileSlotController>();
            if (parentTile != null)
            {
                // 在战场上，将点击事件传递给格子
                // 使用ExecuteEvents将点击传递给格子
                ExecuteEvents.Execute(parentTile.gameObject, eventData, ExecuteEvents.pointerClickHandler);
                return;
            }

            // 在手牌中，正常处理点击
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                OnCardClicked?.Invoke(this);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnCardRightClicked?.Invoke(this);
            }
        }

        #endregion
    }
}
