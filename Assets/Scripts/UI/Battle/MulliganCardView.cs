using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShadowCardSmash.Core.Data;

namespace ShadowCardSmash.UI.Battle
{
    /// <summary>
    /// 换牌阶段卡牌视图
    /// </summary>
    public class MulliganCardView : MonoBehaviour
    {
        [Header("UI References")]
        public Image cardFrame;
        public Image cardArt;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI attackText;
        public TextMeshProUGUI healthText;
        public GameObject attackHealthGroup;
        public GameObject selectedOverlay;
        public Button cardButton;

        [Header("Frame Colors")]
        public Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);
        public Color silverColor = new Color(0.75f, 0.75f, 0.75f);
        public Color goldColor = new Color(1f, 0.84f, 0f);
        public Color legendaryColor = new Color(1f, 0.41f, 0.71f);

        // 使用普通委托而非 event，以便外部可以清除
        public System.Action OnClicked;

        private int _handIndex;
        private bool _isSelected;
        private CardData _cardData;

        public int HandIndex => _handIndex;
        public bool IsSelected => _isSelected;
        public CardData CardData => _cardData;

        /// <summary>
        /// 设置卡牌数据
        /// </summary>
        public void Setup(CardData cardData, int index)
        {
            _handIndex = index;
            _cardData = cardData;

            if (cardData != null)
            {
                if (nameText != null)
                    nameText.text = cardData.cardName;

                if (costText != null)
                    costText.text = cardData.cost.ToString();

                if (cardFrame != null)
                    cardFrame.color = GetRarityColor(cardData.rarity);

                // 根据卡牌类型显示攻击/生命
                if (cardData.cardType == CardType.Minion)
                {
                    if (attackHealthGroup != null)
                        attackHealthGroup.SetActive(true);

                    if (attackText != null)
                        attackText.text = cardData.attack.ToString();

                    if (healthText != null)
                        healthText.text = cardData.health.ToString();
                }
                else if (cardData.cardType == CardType.Amulet && cardData.countdown > 0)
                {
                    if (attackHealthGroup != null)
                        attackHealthGroup.SetActive(true);

                    if (attackText != null)
                    {
                        attackText.text = "";
                        attackText.gameObject.SetActive(false);
                    }

                    if (healthText != null)
                        healthText.text = cardData.countdown.ToString();
                }
                else
                {
                    if (attackHealthGroup != null)
                        attackHealthGroup.SetActive(false);
                }
            }

            SetSelected(false);

            // 只使用 Button 的 onClick，移除 IPointerClickHandler 避免重复触发
            if (cardButton != null)
            {
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(HandleClick);
            }
        }

        private void HandleClick()
        {
            OnClicked?.Invoke();
        }

        /// <summary>
        /// 设置选中状态
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            Debug.Log($"MulliganCardView: SetSelected({selected}), overlay={(selectedOverlay != null ? "存在" : "null")}");
            if (selectedOverlay != null)
            {
                selectedOverlay.SetActive(selected);
            }
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
    }
}
