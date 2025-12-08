using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShadowCardSmash.Core.Data;

namespace ShadowCardSmash.UI.Battle
{
    /// <summary>
    /// 卡牌详情弹窗
    /// </summary>
    public class CardDetailPopup : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject popupRoot;
        public Image cardArt;
        public Image cardFrame;
        public TextMeshProUGUI cardNameText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI attackText;
        public TextMeshProUGUI healthText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI tagsText;
        public TextMeshProUGUI cardTypeText;
        public GameObject attackHealthGroup;
        public Button closeButton;
        public Button overlayButton;

        [Header("Frame Colors")]
        public Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);
        public Color silverColor = new Color(0.75f, 0.75f, 0.75f);
        public Color goldColor = new Color(1f, 0.84f, 0f);
        public Color legendaryColor = new Color(1f, 0.41f, 0.71f);

        [Header("Keyword Colors")]
        public Color keywordColor = new Color(1f, 0.8f, 0.2f);

        // 当前显示的数据
        private CardData _currentCardData;
        private RuntimeCard _currentRuntimeCard;

        void Awake()
        {
            // 初始隐藏
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }

            // 绑定关闭事件
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }
            if (overlayButton != null)
            {
                overlayButton.onClick.AddListener(Hide);
            }
        }

        /// <summary>
        /// 显示卡牌详情（静态数据）
        /// </summary>
        public void Show(CardData cardData)
        {
            if (cardData == null) return;

            _currentCardData = cardData;
            _currentRuntimeCard = null;

            UpdateDisplay();
            ShowPopup();
        }

        /// <summary>
        /// 显示卡牌详情（运行时数据）
        /// </summary>
        public void Show(RuntimeCard runtimeCard, CardData baseData)
        {
            if (runtimeCard == null || baseData == null) return;

            _currentCardData = baseData;
            _currentRuntimeCard = runtimeCard;

            UpdateDisplay();
            ShowPopup();
        }

        /// <summary>
        /// 隐藏弹窗
        /// </summary>
        public void Hide()
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
        }

        private void ShowPopup()
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(true);
            }
        }

        private void UpdateDisplay()
        {
            if (_currentCardData == null) return;

            // 卡牌名称
            if (cardNameText != null)
            {
                cardNameText.text = _currentCardData.cardName;
            }

            // 费用
            if (costText != null)
            {
                costText.text = _currentCardData.cost.ToString();
            }

            // 边框颜色
            if (cardFrame != null)
            {
                cardFrame.color = GetRarityColor(_currentCardData.rarity);
            }

            // 卡牌类型
            if (cardTypeText != null)
            {
                cardTypeText.text = GetCardTypeString(_currentCardData.cardType);
            }

            // 攻击/生命值
            if (_currentCardData.cardType == CardType.Minion)
            {
                if (attackHealthGroup != null)
                {
                    attackHealthGroup.SetActive(true);
                }

                if (_currentRuntimeCard != null)
                {
                    // 显示当前值和基础值的对比
                    if (attackText != null)
                    {
                        int currentAtk = _currentRuntimeCard.currentAttack;
                        int baseAtk = _currentCardData.attack;
                        attackText.text = currentAtk.ToString();
                        attackText.color = currentAtk > baseAtk ? Color.green : (currentAtk < baseAtk ? Color.red : Color.white);
                    }
                    if (healthText != null)
                    {
                        int currentHp = _currentRuntimeCard.currentHealth;
                        int baseHp = _currentCardData.health;
                        healthText.text = currentHp.ToString();
                        healthText.color = currentHp > baseHp ? Color.green : (currentHp < baseHp ? Color.red : Color.white);
                    }
                }
                else
                {
                    if (attackText != null)
                    {
                        attackText.text = _currentCardData.attack.ToString();
                        attackText.color = Color.white;
                    }
                    if (healthText != null)
                    {
                        healthText.text = _currentCardData.health.ToString();
                        healthText.color = Color.white;
                    }
                }
            }
            else if (_currentCardData.cardType == CardType.Amulet && _currentCardData.countdown > 0)
            {
                if (attackHealthGroup != null)
                {
                    attackHealthGroup.SetActive(true);
                }

                if (attackText != null)
                {
                    attackText.gameObject.SetActive(false);
                }
                if (healthText != null)
                {
                    healthText.gameObject.SetActive(true);
                    int countdown = _currentRuntimeCard != null ? _currentRuntimeCard.currentHealth : _currentCardData.countdown;
                    healthText.text = countdown.ToString();
                    healthText.color = Color.white;
                }
            }
            else
            {
                if (attackHealthGroup != null)
                {
                    attackHealthGroup.SetActive(false);
                }
            }

            // 描述文本（带关键词高亮）
            if (descriptionText != null)
            {
                descriptionText.text = FormatDescription(_currentCardData.description);
            }

            // 标签
            if (tagsText != null)
            {
                if (_currentCardData.tags != null && _currentCardData.tags.Count > 0)
                {
                    tagsText.text = string.Join(" | ", _currentCardData.tags);
                    tagsText.gameObject.SetActive(true);
                }
                else
                {
                    tagsText.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 格式化描述文本（处理关键词高亮）
        /// </summary>
        private string FormatDescription(string description)
        {
            if (string.IsNullOrEmpty(description)) return "";

            // 定义需要高亮的关键词
            string[] keywords = { "守护", "突进", "疾驰", "开幕", "谢幕", "攻击时", "进化时", "启动" };

            string formatted = description;

            // 使用富文本高亮关键词
            string colorHex = ColorUtility.ToHtmlStringRGB(keywordColor);
            foreach (var keyword in keywords)
            {
                formatted = formatted.Replace(keyword, $"<color=#{colorHex}>{keyword}</color>");
            }

            return formatted;
        }

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

        private string GetCardTypeString(CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Minion: return "随从";
                case CardType.Spell: return "法术";
                case CardType.Amulet: return "护符";
                default: return "";
            }
        }
    }
}
