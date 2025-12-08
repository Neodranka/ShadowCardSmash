using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Effects;

namespace ShadowCardSmash.UI.Battle
{
    /// <summary>
    /// 卡牌列表弹窗 - 用于显示牌库/墓地列表
    /// </summary>
    public class CardListPopup : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject popupRoot;
        public TextMeshProUGUI titleText;
        public Transform contentContainer;
        public TextMeshProUGUI cardCountText;
        public Button closeButton;
        public Button overlayButton;
        public ScrollRect scrollRect;

        [Header("Prefabs")]
        public GameObject miniCardPrefab;

        // 卡牌数据库引用
        private ICardDatabase _cardDatabase;

        // 当前显示的卡牌
        private List<GameObject> _cardItems = new List<GameObject>();

        // 事件
        public event Action<int> OnCardClicked; // 参数：卡牌ID

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
        /// 设置卡牌数据库引用
        /// </summary>
        public void SetCardDatabase(ICardDatabase cardDatabase)
        {
            _cardDatabase = cardDatabase;
        }

        /// <summary>
        /// 显示牌库列表
        /// </summary>
        public void ShowDeck(List<int> cardIds)
        {
            if (titleText != null)
            {
                titleText.text = "牌库";
            }

            ShowCardList(cardIds);
        }

        /// <summary>
        /// 显示墓地列表
        /// </summary>
        public void ShowGraveyard(List<int> cardIds)
        {
            if (titleText != null)
            {
                titleText.text = "墓地";
            }

            ShowCardList(cardIds);
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

        private void ShowCardList(List<int> cardIds)
        {
            // 清除现有内容
            ClearContent();

            // 更新计数
            if (cardCountText != null)
            {
                cardCountText.text = $"共 {cardIds.Count} 张";
            }

            // 统计每种卡牌的数量
            var cardCounts = new Dictionary<int, int>();
            foreach (var cardId in cardIds)
            {
                if (!cardCounts.ContainsKey(cardId))
                {
                    cardCounts[cardId] = 0;
                }
                cardCounts[cardId]++;
            }

            // 按费用排序显示
            var sortedCards = new List<KeyValuePair<int, int>>(cardCounts);
            sortedCards.Sort((a, b) =>
            {
                var cardA = _cardDatabase?.GetCardById(a.Key);
                var cardB = _cardDatabase?.GetCardById(b.Key);
                if (cardA == null || cardB == null) return 0;
                return cardA.cost.CompareTo(cardB.cost);
            });

            // 创建卡牌项
            foreach (var kvp in sortedCards)
            {
                CreateCardItem(kvp.Key, kvp.Value);
            }

            // 重置滚动位置
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }

            // 显示弹窗
            if (popupRoot != null)
            {
                popupRoot.SetActive(true);
            }
        }

        private void CreateCardItem(int cardId, int count)
        {
            if (contentContainer == null) return;

            CardData cardData = _cardDatabase?.GetCardById(cardId);
            if (cardData == null) return;

            // 如果有预制体，使用预制体
            GameObject itemObj;
            if (miniCardPrefab != null)
            {
                itemObj = Instantiate(miniCardPrefab, contentContainer);
                var cardView = itemObj.GetComponent<CardViewController>();
                if (cardView != null)
                {
                    cardView.SetCardData(cardData);
                }
            }
            else
            {
                // 否则创建简单的文本项
                itemObj = CreateSimpleCardItem(cardData, count);
            }

            // 添加点击事件
            var button = itemObj.GetComponent<Button>();
            if (button == null)
            {
                button = itemObj.AddComponent<Button>();
            }
            int capturedId = cardId;
            button.onClick.AddListener(() => OnCardClicked?.Invoke(capturedId));

            _cardItems.Add(itemObj);
        }

        private GameObject CreateSimpleCardItem(CardData cardData, int count)
        {
            var itemObj = new GameObject($"CardItem_{cardData.cardId}");
            itemObj.transform.SetParent(contentContainer, false);

            // 添加布局元素
            var layoutElement = itemObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 40f;
            layoutElement.flexibleWidth = 1f;

            // 添加水平布局
            var hlg = itemObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.padding = new RectOffset(10, 10, 5, 5);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;

            // 添加背景
            var bgImage = itemObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            // 费用文本
            CreateTextChild(itemObj, cardData.cost.ToString(), 30f, Color.cyan);

            // 名称文本
            CreateTextChild(itemObj, cardData.cardName, 200f, Color.white);

            // 数量文本
            CreateTextChild(itemObj, $"x{count}", 40f, Color.yellow);

            // 攻击/生命（如果是随从）
            if (cardData.cardType == CardType.Minion)
            {
                CreateTextChild(itemObj, $"{cardData.attack}/{cardData.health}", 50f, Color.gray);
            }

            return itemObj;
        }

        private void CreateTextChild(GameObject parent, string text, float width, Color color)
        {
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(parent.transform, false);

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 16;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            var layoutElement = textObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
        }

        private void ClearContent()
        {
            foreach (var item in _cardItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            _cardItems.Clear();
        }

        void OnDestroy()
        {
            ClearContent();
        }
    }
}
