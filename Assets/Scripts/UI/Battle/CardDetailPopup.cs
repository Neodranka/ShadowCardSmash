using System;
using System.Text;
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

        [Header("Condition Progress")]
        public TextMeshProUGUI conditionProgressText;

        [Header("Frame Colors")]
        public Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);
        public Color silverColor = new Color(0.75f, 0.75f, 0.75f);
        public Color goldColor = new Color(1f, 0.84f, 0f);
        public Color legendaryColor = new Color(1f, 0.41f, 0.71f);

        [Header("Keyword Colors")]
        public Color keywordColor = new Color(1f, 0.8f, 0.2f);
        public Color conditionMetColor = new Color(0.2f, 0.8f, 0.2f);
        public Color conditionNotMetColor = new Color(0.8f, 0.8f, 0.8f);

        // 当前显示的数据
        private CardData _currentCardData;
        private RuntimeCard _currentRuntimeCard;
        private PlayerState _currentPlayerState;

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
        public void Show(CardData cardData, PlayerState playerState = null)
        {
            if (cardData == null) return;

            _currentCardData = cardData;
            _currentRuntimeCard = null;
            _currentPlayerState = playerState;

            UpdateDisplay();
            ShowPopup();
        }

        /// <summary>
        /// 显示卡牌详情（运行时数据）
        /// </summary>
        public void Show(RuntimeCard runtimeCard, CardData baseData, PlayerState playerState = null)
        {
            if (runtimeCard == null || baseData == null) return;

            _currentCardData = baseData;
            _currentRuntimeCard = runtimeCard;
            _currentPlayerState = playerState;

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

            // 条件进度
            UpdateConditionProgress();
        }

        /// <summary>
        /// 更新条件进度显示
        /// </summary>
        private void UpdateConditionProgress()
        {
            if (conditionProgressText == null)
            {
                return;
            }

            if (_currentPlayerState == null || _currentCardData == null)
            {
                conditionProgressText.gameObject.SetActive(false);
                return;
            }

            var sb = new StringBuilder();
            var displayedConditions = new System.Collections.Generic.HashSet<string>();

            // 检查效果中的条件
            if (_currentCardData.effects != null)
            {
                foreach (var effect in _currentCardData.effects)
                {
                    AddConditionProgress(sb, effect.condition, displayedConditions);
                    // 也检查 parameters 中的条件（如 heal_if_condition:xxx）
                    ExtractConditionsFromParameters(sb, effect.parameters, displayedConditions);
                }
            }

            // 检查强化效果中的条件
            if (_currentCardData.enhanceEffects != null)
            {
                foreach (var effect in _currentCardData.enhanceEffects)
                {
                    AddConditionProgress(sb, effect.condition, displayedConditions);
                    ExtractConditionsFromParameters(sb, effect.parameters, displayedConditions);
                }
            }

            // 检查进化效果中的条件
            if (_currentCardData.evolveEffects != null)
            {
                foreach (var effect in _currentCardData.evolveEffects)
                {
                    AddConditionProgress(sb, effect.condition, displayedConditions);
                    ExtractConditionsFromParameters(sb, effect.parameters, displayedConditions);
                }
            }

            if (displayedConditions.Count > 0)
            {
                conditionProgressText.text = sb.ToString();
                conditionProgressText.gameObject.SetActive(true);
            }
            else
            {
                conditionProgressText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 添加条件进度到字符串（使用 HashSet 避免重复）
        /// </summary>
        private void AddConditionProgress(StringBuilder sb, string condition, System.Collections.Generic.HashSet<string> displayedConditions)
        {
            if (string.IsNullOrEmpty(condition)) return;

            // 提取条件的关键部分用于去重（如 "total_self_damage >= 15" 和 "total_self_damage < 15" 都算同一个条件类型）
            string conditionKey = ExtractConditionKey(condition);
            if (displayedConditions.Contains(conditionKey)) return;

            // 解析条件并获取当前进度
            string progressText = GetConditionProgressText(condition);
            if (!string.IsNullOrEmpty(progressText))
            {
                if (displayedConditions.Count > 0)
                {
                    sb.AppendLine();
                }
                sb.Append(progressText);
                displayedConditions.Add(conditionKey);
            }
        }

        /// <summary>
        /// 从参数列表中提取条件
        /// </summary>
        private void ExtractConditionsFromParameters(StringBuilder sb, System.Collections.Generic.List<string> parameters, System.Collections.Generic.HashSet<string> displayedConditions)
        {
            if (parameters == null) return;

            foreach (var param in parameters)
            {
                // 检查 heal_if_condition:xxx 格式
                if (param.StartsWith("heal_if_condition:"))
                {
                    string condition = param.Substring("heal_if_condition:".Length);
                    AddConditionProgress(sb, condition, displayedConditions);
                }
            }
        }

        /// <summary>
        /// 提取条件的关键部分（用于去重）
        /// </summary>
        private string ExtractConditionKey(string condition)
        {
            // 移除比较运算符和数值，只保留变量名
            string[] operators = { ">=", "<=", ">", "<", "==" };
            foreach (var op in operators)
            {
                if (condition.Contains(op))
                {
                    return condition.Split(new string[] { op }, StringSplitOptions.None)[0].Trim();
                }
            }
            return condition;
        }

        /// <summary>
        /// 获取条件进度文本
        /// </summary>
        private string GetConditionProgressText(string condition)
        {
            if (_currentPlayerState == null) return null;

            string colorHex;
            bool conditionMet;

            // 解析不同类型的条件
            if (condition.StartsWith("total_self_damage"))
            {
                int current = _currentPlayerState.totalSelfDamage;
                int threshold = ExtractThreshold(condition);
                conditionMet = EvaluateCondition(current, condition, threshold);
                colorHex = ColorUtility.ToHtmlStringRGB(conditionMet ? conditionMetColor : conditionNotMetColor);
                return $"<color=#{colorHex}>自伤累计: {current}/{threshold}</color>";
            }
            else if (condition.StartsWith("self_damage_count"))
            {
                int current = _currentPlayerState.selfDamageCount;
                int threshold = ExtractThreshold(condition);
                conditionMet = EvaluateCondition(current, condition, threshold);
                colorHex = ColorUtility.ToHtmlStringRGB(conditionMet ? conditionMetColor : conditionNotMetColor);
                return $"<color=#{colorHex}>自伤次数: {current}/{threshold}</color>";
            }
            else if (condition.StartsWith("self_damage_this_turn"))
            {
                int current = _currentPlayerState.selfDamageThisTurn;
                int threshold = ExtractThreshold(condition);
                conditionMet = EvaluateCondition(current, condition, threshold);
                colorHex = ColorUtility.ToHtmlStringRGB(conditionMet ? conditionMetColor : conditionNotMetColor);
                return $"<color=#{colorHex}>本回合自伤: {current}/{threshold}</color>";
            }
            else if (condition == "is_evolved")
            {
                conditionMet = _currentRuntimeCard != null && _currentRuntimeCard.isEvolved;
                colorHex = ColorUtility.ToHtmlStringRGB(conditionMet ? conditionMetColor : conditionNotMetColor);
                return $"<color=#{colorHex}>需要进化: {(conditionMet ? "是" : "否")}</color>";
            }
            else if (condition == "minion_destroyed_this_turn")
            {
                conditionMet = _currentPlayerState.minionDestroyedThisTurn;
                colorHex = ColorUtility.ToHtmlStringRGB(conditionMet ? conditionMetColor : conditionNotMetColor);
                return $"<color=#{colorHex}>本回合有随从被破坏: {(conditionMet ? "是" : "否")}</color>";
            }
            else if (condition.Contains("&&"))
            {
                // 复合条件，分开显示
                return null; // 复合条件的各部分会单独处理
            }

            return null;
        }

        /// <summary>
        /// 从条件中提取阈值
        /// </summary>
        private int ExtractThreshold(string condition)
        {
            // 支持 >=, >, <=, <
            string[] operators = { ">=", "<=", ">", "<" };
            foreach (var op in operators)
            {
                if (condition.Contains(op))
                {
                    var parts = condition.Split(new string[] { op }, StringSplitOptions.None);
                    if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int threshold))
                    {
                        return threshold;
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// 评估条件是否满足
        /// </summary>
        private bool EvaluateCondition(int current, string condition, int threshold)
        {
            if (condition.Contains(">="))
                return current >= threshold;
            if (condition.Contains("<="))
                return current <= threshold;
            if (condition.Contains(">"))
                return current > threshold;
            if (condition.Contains("<"))
                return current < threshold;
            return false;
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
