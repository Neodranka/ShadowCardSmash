using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Effects;

namespace ShadowCardSmash.UI.Battle
{
    /// <summary>
    /// 换牌阶段UI管理器
    /// </summary>
    public class MulliganUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject mulliganPanel;
        public Transform cardContainer;
        public Button confirmButton;
        public TextMeshProUGUI instructionText;
        public TextMeshProUGUI selectedCountText;

        [Header("Prefabs")]
        public GameObject mulliganCardPrefab;

        private List<MulliganCardView> _cardViews = new List<MulliganCardView>();
        private HashSet<int> _selectedIndices = new HashSet<int>();
        private ICardDatabase _cardDatabase;
        private int _currentPlayerId;
        private bool _isShowing = false; // 追踪是否正在显示

        public event System.Action<int> OnCardToggled;      // 参数：手牌索引
        public event System.Action OnConfirmClicked;

        void Awake()
        {
            // 在 Awake 中隐藏面板（比 Start 更早执行）
            if (mulliganPanel != null && mulliganPanel != gameObject)
            {
                mulliganPanel.SetActive(false);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmButtonClicked);
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
        /// 显示换牌界面
        /// </summary>
        public void Show(List<RuntimeCard> hand, int playerId, bool isSecondPlayer)
        {
            Debug.Log($"MulliganUI.Show: mulliganPanel={mulliganPanel != null}, cardContainer={cardContainer != null}, hand.Count={hand?.Count ?? 0}");

            if (mulliganPanel == null)
            {
                Debug.LogError("MulliganUI.Show: mulliganPanel 为空!");
                return;
            }

            _currentPlayerId = playerId;
            mulliganPanel.SetActive(true);

            // 输出面板的位置和大小信息，并自动修复大小为0的问题
            var panelRect = mulliganPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                Debug.Log($"MulliganUI.Show: Panel位置={panelRect.anchoredPosition}, 大小={panelRect.sizeDelta}, anchorMin={panelRect.anchorMin}, anchorMax={panelRect.anchorMax}, active={mulliganPanel.activeInHierarchy}");

                // 如果大小为0，自动设置为拉伸填充父容器
                if (panelRect.sizeDelta == Vector2.zero)
                {
                    Debug.Log("MulliganUI.Show: 检测到面板大小为0，自动拉伸填充");
                    panelRect.anchorMin = Vector2.zero;
                    panelRect.anchorMax = Vector2.one;
                    panelRect.offsetMin = Vector2.zero;
                    panelRect.offsetMax = Vector2.zero;
                    Debug.Log($"MulliganUI.Show: 修复后大小={panelRect.rect.size}");
                }
            }

            _selectedIndices.Clear();

            // 清除旧的卡牌视图
            foreach (var view in _cardViews)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }
            _cardViews.Clear();

            // 如果 cardContainer 为空，尝试使用 mulliganPanel 自身
            Transform actualContainer = cardContainer != null ? cardContainer : mulliganPanel.transform;
            Debug.Log($"MulliganUI.Show: 使用容器={actualContainer.name}");

            // 创建手牌视图
            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                CardData cardData = _cardDatabase?.GetCardById(card.cardId);

                GameObject cardObj;
                if (mulliganCardPrefab != null)
                {
                    cardObj = Instantiate(mulliganCardPrefab, actualContainer);
                }
                else
                {
                    // 如果没有预制体，创建简单的占位符
                    cardObj = CreateSimpleCardView(actualContainer);
                }

                int index = i; // 闭包捕获

                // 检查是否有 CardViewController（使用 CardView prefab 的情况）
                var cardViewController = cardObj.GetComponent<CardViewController>();
                if (cardViewController != null)
                {
                    // 使用 CardViewController，需要适配
                    var mulliganView = SetupMulliganViewFromCardViewController(cardObj, cardViewController, cardData, index);
                    _cardViews.Add(mulliganView);
                    Debug.Log($"MulliganUI: 从CardViewController创建卡牌视图 {i}, cardData={cardData?.cardName ?? "null"}");
                    continue;
                }

                // 使用 MulliganCardView
                var cardView = cardObj.GetComponent<MulliganCardView>();
                if (cardView == null)
                {
                    cardView = cardObj.AddComponent<MulliganCardView>();

                    // 设置引用
                    cardView.cardButton = cardObj.GetComponent<Button>();
                    cardView.selectedOverlay = cardObj.transform.Find("SelectedOverlay")?.gameObject;
                    cardView.nameText = cardObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                    cardView.costText = cardObj.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
                }

                // 先清除旧的事件订阅，再设置
                cardView.OnClicked = null;
                cardView.Setup(cardData, index);
                cardView.OnClicked += () => {
                    Debug.Log($"MulliganUI: 卡牌 {index} 被点击 (来自事件)");
                    ToggleCard(index);
                };

                _cardViews.Add(cardView);
                Debug.Log($"MulliganUI: 创建卡牌视图 {i}, cardData={cardData?.cardName ?? "null"}, overlay={cardView.selectedOverlay != null}");
            }

            UpdateUI();

            string playerText = isSecondPlayer ? "后手" : "先手";
            if (instructionText != null)
            {
                instructionText.text = $"你是{playerText}，点击要换掉的牌，然后确认";
            }

            SetConfirmButtonInteractable(true);
        }

        /// <summary>
        /// 隐藏换牌界面
        /// </summary>
        public void Hide()
        {
            if (mulliganPanel != null)
            {
                mulliganPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 检查是否显示中
        /// </summary>
        public bool IsVisible()
        {
            return mulliganPanel != null && mulliganPanel.activeSelf;
        }

        private void ToggleCard(int index)
        {
            Debug.Log($"MulliganUI.ToggleCard: index={index}, _cardViews.Count={_cardViews.Count}");

            if (index < 0 || index >= _cardViews.Count)
            {
                Debug.LogWarning($"MulliganUI.ToggleCard: 索引无效 {index}");
                return;
            }

            bool wasSelected = _selectedIndices.Contains(index);
            Debug.Log($"MulliganUI.ToggleCard: wasSelected={wasSelected}");

            if (wasSelected)
            {
                _selectedIndices.Remove(index);
                _cardViews[index].SetSelected(false);
                Debug.Log($"MulliganUI.ToggleCard: 取消选择卡牌 {index}");
            }
            else
            {
                _selectedIndices.Add(index);
                _cardViews[index].SetSelected(true);
                Debug.Log($"MulliganUI.ToggleCard: 选择卡牌 {index}");
            }

            Debug.Log($"MulliganUI.ToggleCard: 当前选择数量={_selectedIndices.Count}");
            UpdateUI();
            OnCardToggled?.Invoke(index);
        }

        private void UpdateUI()
        {
            if (selectedCountText != null)
            {
                selectedCountText.text = $"已选择 {_selectedIndices.Count} 张";
            }
        }

        public void OnConfirmButtonClicked()
        {
            OnConfirmClicked?.Invoke();
        }

        public void SetConfirmButtonInteractable(bool interactable)
        {
            if (confirmButton != null)
            {
                confirmButton.interactable = interactable;
            }
        }

        /// <summary>
        /// 获取当前选择的卡牌索引
        /// </summary>
        public List<int> GetSelectedIndices()
        {
            return new List<int>(_selectedIndices);
        }

        /// <summary>
        /// 获取当前玩家ID
        /// </summary>
        public int GetCurrentPlayerId()
        {
            return _currentPlayerId;
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
                overlayImage.color = new Color(1f, 0.5f, 0f, 0.5f);
                overlayImage.raycastTarget = false;
                overlayObj.SetActive(false);
                mulliganView.selectedOverlay = overlayObj;
            }

            // 确保有 Button 组件用于点击
            var button = cardObj.GetComponent<Button>();
            if (button == null)
            {
                button = cardObj.AddComponent<Button>();
                // 设置 Button 的 targetGraphic
                var image = cardObj.GetComponent<Image>();
                if (image != null)
                {
                    button.targetGraphic = image;
                }
            }
            mulliganView.cardButton = button;

            // 使用 CardViewController 显示卡牌数据
            cardViewController.SetCardData(cardData);

            // 设置 MulliganCardView（不会重复设置已由 CardViewController 设置的文本）
            mulliganView.OnClicked = null;

            // 手动设置索引和选择状态（不调用 Setup 避免覆盖 CardViewController 的显示）
            // 使用反射或直接访问字段
            SetMulliganViewIndex(mulliganView, index);
            mulliganView.SetSelected(false);

            // 设置按钮点击事件
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => {
                Debug.Log($"MulliganUI: CardViewController卡牌 {index} 被点击");
                mulliganView.OnClicked?.Invoke();
            });

            // 订阅点击事件
            mulliganView.OnClicked += () => {
                Debug.Log($"MulliganUI: 卡牌 {index} 被点击 (来自CardViewController)");
                ToggleCard(index);
            };

            return mulliganView;
        }

        /// <summary>
        /// 设置 MulliganCardView 的索引（通过反射访问私有字段）
        /// </summary>
        private void SetMulliganViewIndex(MulliganCardView view, int index)
        {
            var field = typeof(MulliganCardView).GetField("_handIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(view, index);
            }
        }

        /// <summary>
        /// 创建简单的卡牌视图（当没有预制体时）
        /// </summary>
        private GameObject CreateSimpleCardView(Transform parent)
        {
            var cardObj = new GameObject("MulliganCard");
            cardObj.transform.SetParent(parent, false);

            var rect = cardObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 220);

            // 背景 - 确保 raycastTarget 启用
            var bg = cardObj.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.25f, 0.2f, 1f);
            bg.raycastTarget = true;

            // 按钮
            var button = cardObj.AddComponent<Button>();
            button.targetGraphic = bg;

            // 选择遮罩 - 不阻挡点击
            var overlayObj = new GameObject("SelectedOverlay");
            overlayObj.transform.SetParent(cardObj.transform, false);
            var overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            var overlayImage = overlayObj.AddComponent<Image>();
            overlayImage.color = new Color(1f, 0.5f, 0f, 0.5f);
            overlayImage.raycastTarget = false; // 不阻挡点击
            overlayObj.SetActive(false);

            // 名称文本 - 不阻挡点击
            var nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(cardObj.transform, false);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchoredPosition = new Vector2(0, -70);
            nameRect.sizeDelta = new Vector2(150, 30);
            var nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "卡牌";
            nameText.fontSize = 14;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
            nameText.raycastTarget = false; // 不阻挡点击

            // 费用文本 - 不阻挡点击
            var costObj = new GameObject("CostText");
            costObj.transform.SetParent(cardObj.transform, false);
            var costRect = costObj.AddComponent<RectTransform>();
            costRect.anchoredPosition = new Vector2(-60, 90);
            costRect.sizeDelta = new Vector2(30, 30);
            var costText = costObj.AddComponent<TextMeshProUGUI>();
            costText.text = "0";
            costText.fontSize = 18;
            costText.alignment = TextAlignmentOptions.Center;
            costText.color = Color.white;
            costText.raycastTarget = false; // 不阻挡点击

            // 添加 LayoutElement
            var layout = cardObj.AddComponent<LayoutElement>();
            layout.preferredWidth = 160;
            layout.preferredHeight = 220;

            // 注意：不在这里添加 MulliganCardView，让调用者处理
            // 因为 MulliganUI.Show() 会检查并添加组件

            return cardObj;
        }
    }
}
