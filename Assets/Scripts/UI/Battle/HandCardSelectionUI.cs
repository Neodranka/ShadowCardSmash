using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Effects;

namespace ShadowCardSmash.UI.Battle
{
    /// <summary>
    /// 手牌选择UI - 用于需要选择手牌的效果（军需官、饥饿的捕食者等）
    /// </summary>
    public class HandCardSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject selectionPanel;
        public Transform cardContainer;
        public Button confirmButton;
        public Button cancelButton;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI instructionText;

        [Header("Prefabs")]
        public GameObject cardPrefab;

        private List<MulliganCardView> _cardViews = new List<MulliganCardView>();
        private int _selectedIndex = -1;
        private ICardDatabase _cardDatabase;
        private Func<RuntimeCard, bool> _filterFunc;
        private int _maxSelections = 1;

        public event Action<int> OnCardSelected;  // 返回选中的手牌索引
        public event Action OnCancelled;

        void Awake()
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        public void SetCardDatabase(ICardDatabase cardDatabase)
        {
            _cardDatabase = cardDatabase;
        }

        /// <summary>
        /// 显示手牌选择界面
        /// </summary>
        /// <param name="hand">手牌列表</param>
        /// <param name="title">标题</param>
        /// <param name="instruction">说明文字</param>
        /// <param name="filter">过滤函数（返回true表示可选）</param>
        /// <param name="excludeInstanceId">排除的卡牌instanceId（用于排除自己）</param>
        public void Show(List<RuntimeCard> hand, string title, string instruction,
            Func<RuntimeCard, bool> filter = null, int excludeInstanceId = -1)
        {
            if (selectionPanel == null)
            {
                Debug.LogError("HandCardSelectionUI: selectionPanel 为空!");
                return;
            }

            _filterFunc = filter;
            _selectedIndex = -1;

            selectionPanel.SetActive(true);

            // 设置文本
            if (titleText != null)
            {
                titleText.text = title;
            }
            if (instructionText != null)
            {
                instructionText.text = instruction;
            }

            // 清除旧的卡牌视图
            foreach (var view in _cardViews)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }
            _cardViews.Clear();

            Transform actualContainer = cardContainer != null ? cardContainer : selectionPanel.transform;

            // 创建手牌视图
            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];

                // 检查是否排除
                if (excludeInstanceId >= 0 && card.instanceId == excludeInstanceId)
                {
                    continue;
                }

                // 检查过滤条件
                bool isValid = true;
                if (filter != null)
                {
                    isValid = filter(card);
                }

                CardData cardData = _cardDatabase?.GetCardById(card.cardId);

                GameObject cardObj;
                if (cardPrefab != null)
                {
                    cardObj = Instantiate(cardPrefab, actualContainer);
                }
                else
                {
                    cardObj = CreateSimpleCardView(actualContainer);
                }

                int handIndex = i; // 闭包捕获

                // 检查是否有 CardViewController（使用 CardView prefab 的情况）
                var cardViewController = cardObj.GetComponent<CardViewController>();
                MulliganCardView cardView;

                if (cardViewController != null)
                {
                    // 使用 CardViewController，需要适配
                    cardView = SetupMulliganViewFromCardViewController(cardObj, cardViewController, cardData, handIndex);
                }
                else
                {
                    // 使用 MulliganCardView
                    cardView = cardObj.GetComponent<MulliganCardView>();
                    if (cardView == null)
                    {
                        cardView = cardObj.AddComponent<MulliganCardView>();
                        cardView.cardButton = cardObj.GetComponent<Button>();
                        cardView.selectedOverlay = cardObj.transform.Find("SelectedOverlay")?.gameObject;
                        cardView.nameText = cardObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                        cardView.costText = cardObj.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
                    }

                    cardView.OnClicked = null;
                    cardView.Setup(cardData, handIndex);
                }

                if (isValid)
                {
                    cardView.OnClicked += () => SelectCard(handIndex);
                }
                else
                {
                    // 不可选的牌变暗
                    var image = cardObj.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    }
                    var button = cardObj.GetComponent<Button>();
                    if (button != null)
                    {
                        button.interactable = false;
                    }
                }

                _cardViews.Add(cardView);
            }

            UpdateConfirmButton();
        }

        public void Hide()
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }
            _selectedIndex = -1;
        }

        public bool IsVisible()
        {
            return selectionPanel != null && selectionPanel.activeSelf;
        }

        private void SelectCard(int handIndex)
        {
            // 取消之前的选择
            foreach (var view in _cardViews)
            {
                view.SetSelected(false);
            }

            _selectedIndex = handIndex;

            // 找到对应的视图并选中
            foreach (var view in _cardViews)
            {
                if (view.HandIndex == handIndex)
                {
                    view.SetSelected(true);
                    break;
                }
            }

            Debug.Log($"HandCardSelectionUI: 选择了手牌 {handIndex}");
            UpdateConfirmButton();
        }

        private void UpdateConfirmButton()
        {
            if (confirmButton != null)
            {
                confirmButton.interactable = _selectedIndex >= 0;
            }
        }

        private void OnConfirmClicked()
        {
            if (_selectedIndex >= 0)
            {
                int selected = _selectedIndex;
                Hide();
                OnCardSelected?.Invoke(selected);
            }
        }

        private void OnCancelClicked()
        {
            Hide();
            OnCancelled?.Invoke();
        }

        /// <summary>
        /// 从 CardViewController 设置 MulliganCardView（兼容使用 CardView prefab）
        /// </summary>
        private MulliganCardView SetupMulliganViewFromCardViewController(GameObject cardObj, CardViewController cardViewController, CardData cardData, int index)
        {
            // 添加或获取 MulliganCardView 组件
            var mulliganView = cardObj.GetComponent<MulliganCardView>();
            if (mulliganView == null)
            {
                mulliganView = cardObj.AddComponent<MulliganCardView>();
            }

            // 从 CardViewController 复制引用到 MulliganCardView
            mulliganView.cardFrame = cardViewController.cardFrame;
            mulliganView.cardArt = cardViewController.cardArt;
            mulliganView.nameText = cardViewController.cardNameText;
            mulliganView.costText = cardViewController.costText;
            mulliganView.attackText = cardViewController.attackText;
            mulliganView.healthText = cardViewController.healthText;
            mulliganView.attackHealthGroup = cardViewController.attackHealthGroup;

            // 使用 CardViewController 的 selectionHighlight 作为 selectedOverlay
            mulliganView.selectedOverlay = cardViewController.selectionHighlight;

            // 如果没有 selectionHighlight，创建一个选择遮罩
            if (mulliganView.selectedOverlay == null)
            {
                var overlayObj = new GameObject("SelectedOverlay");
                overlayObj.transform.SetParent(cardObj.transform, false);
                var overlayRect = overlayObj.AddComponent<RectTransform>();
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.sizeDelta = Vector2.zero;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;
                var overlayImage = overlayObj.AddComponent<Image>();
                overlayImage.color = new Color(0f, 1f, 0f, 0.4f); // 绿色表示选中
                overlayImage.raycastTarget = false;
                overlayObj.SetActive(false);
                mulliganView.selectedOverlay = overlayObj;
            }

            // 确保有 Button 组件用于点击
            var button = cardObj.GetComponent<Button>();
            if (button == null)
            {
                button = cardObj.AddComponent<Button>();
                var image = cardObj.GetComponent<Image>();
                if (image != null)
                {
                    button.targetGraphic = image;
                }
            }
            mulliganView.cardButton = button;

            // 使用 CardViewController 显示卡牌数据
            cardViewController.SetCardData(cardData);

            // 手动设置索引
            var field = typeof(MulliganCardView).GetField("_handIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(mulliganView, index);
            }

            mulliganView.OnClicked = null;
            mulliganView.SetSelected(false);

            // 设置按钮点击事件
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => {
                mulliganView.OnClicked?.Invoke();
            });

            return mulliganView;
        }

        private GameObject CreateSimpleCardView(Transform parent)
        {
            var cardObj = new GameObject("SelectionCard");
            cardObj.transform.SetParent(parent, false);

            var rect = cardObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140, 200);

            var bg = cardObj.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.25f, 0.2f, 1f);
            bg.raycastTarget = true;

            var button = cardObj.AddComponent<Button>();
            button.targetGraphic = bg;

            // 选择遮罩
            var overlayObj = new GameObject("SelectedOverlay");
            overlayObj.transform.SetParent(cardObj.transform, false);
            var overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            var overlayImage = overlayObj.AddComponent<Image>();
            overlayImage.color = new Color(0f, 1f, 0f, 0.4f);
            overlayImage.raycastTarget = false;
            overlayObj.SetActive(false);

            // 名称文本
            var nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(cardObj.transform, false);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchoredPosition = new Vector2(0, -60);
            nameRect.sizeDelta = new Vector2(130, 30);
            var nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "卡牌";
            nameText.fontSize = 12;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
            nameText.raycastTarget = false;

            // 费用文本
            var costObj = new GameObject("CostText");
            costObj.transform.SetParent(cardObj.transform, false);
            var costRect = costObj.AddComponent<RectTransform>();
            costRect.anchoredPosition = new Vector2(-50, 80);
            costRect.sizeDelta = new Vector2(30, 30);
            var costText = costObj.AddComponent<TextMeshProUGUI>();
            costText.text = "0";
            costText.fontSize = 16;
            costText.alignment = TextAlignmentOptions.Center;
            costText.color = Color.white;
            costText.raycastTarget = false;

            var layout = cardObj.AddComponent<LayoutElement>();
            layout.preferredWidth = 140;
            layout.preferredHeight = 200;

            return cardObj;
        }
    }
}
