using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShadowCardSmash.Core.Data;

namespace ShadowCardSmash.UI.Battle
{
    /// <summary>
    /// 玩家信息面板 - 显示生命值、费用、进化点等
    /// </summary>
    public class PlayerInfoPanel : MonoBehaviour
    {
        [Header("UI References")]
        public Image portrait;
        public TextMeshProUGUI healthText;
        public TextMeshProUGUI manaText;
        public TextMeshProUGUI handCountText;
        public TextMeshProUGUI deckCountText;
        public Transform epContainer;

        [Header("EP Icons")]
        public GameObject epIconPrefab;
        public Color epAvailableColor = new Color(1f, 0.84f, 0f);
        public Color epUsedColor = new Color(0.3f, 0.3f, 0.3f);

        [Header("Animation Settings")]
        public float damageFlashDuration = 0.3f;
        public Color damageFlashColor = Color.red;

        [Header("Floating Text")]
        public GameObject floatingTextPrefab;
        public Transform floatingTextSpawnPoint;

        [Header("Attack Target Highlight")]
        public GameObject attackTargetHighlight;
        public Color attackTargetColor = new Color(1f, 0.3f, 0.3f, 0.8f);

        // 内部状态
        private List<Image> _epIcons = new List<Image>();
        private bool _isValidAttackTarget;
        private int _currentHealth;
        private int _maxHealth;
        private int _currentMana;
        private int _maxMana;
        private int _evolutionPoints;

        // 事件
        public event Action OnPortraitClicked;

        // 属性
        public bool IsValidAttackTarget => _isValidAttackTarget;

        void Start()
        {
            // 为头像添加点击事件
            if (portrait != null)
            {
                var button = portrait.GetComponent<Button>();
                if (button == null)
                {
                    button = portrait.gameObject.AddComponent<Button>();
                }
                button.onClick.AddListener(() => OnPortraitClicked?.Invoke());
            }
        }

        /// <summary>
        /// 更新全部显示
        /// </summary>
        public void UpdateDisplay(PlayerState playerState)
        {
            if (playerState == null) return;

            UpdateHealth(playerState.health, playerState.maxHealth, false);
            UpdateMana(playerState.mana, playerState.maxMana);
            UpdateHandCount(playerState.hand.Count);
            UpdateDeckCount(playerState.deck.Count);
            UpdateEvolutionPoints(playerState.evolutionPoints, PlayerState.SECOND_PLAYER_EP);
        }

        /// <summary>
        /// 更新生命值（带动画）
        /// </summary>
        public void UpdateHealth(int current, int max, bool animate = true)
        {
            int previousHealth = _currentHealth;
            _currentHealth = current;
            _maxHealth = max;

            if (healthText != null)
            {
                healthText.text = $"{current}/{max}";

                // 根据生命百分比改变颜色
                float healthPercent = (float)current / max;
                if (healthPercent <= 0.25f)
                {
                    healthText.color = Color.red;
                }
                else if (healthPercent <= 0.5f)
                {
                    healthText.color = new Color(1f, 0.5f, 0f); // 橙色
                }
                else
                {
                    healthText.color = Color.white;
                }
            }

            // 播放伤害/治疗动画
            if (animate && previousHealth != 0)
            {
                int diff = current - previousHealth;
                if (diff < 0)
                {
                    PlayDamageAnimation(-diff);
                }
                else if (diff > 0)
                {
                    PlayHealAnimation(diff);
                }
            }
        }

        /// <summary>
        /// 更新费用
        /// </summary>
        public void UpdateMana(int current, int max)
        {
            _currentMana = current;
            _maxMana = max;

            if (manaText != null)
            {
                manaText.text = $"{current}/{max}";

                // 费用耗尽时变暗
                manaText.color = current == 0 && max > 0 ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
            }
        }

        /// <summary>
        /// 更新手牌数
        /// </summary>
        public void UpdateHandCount(int count)
        {
            if (handCountText != null)
            {
                handCountText.text = count.ToString();

                // 手牌满时变红
                handCountText.color = count >= PlayerState.MAX_HAND_SIZE ? Color.red : Color.white;
            }
        }

        /// <summary>
        /// 更新牌库数
        /// </summary>
        public void UpdateDeckCount(int count)
        {
            if (deckCountText != null)
            {
                deckCountText.text = count.ToString();

                // 牌库快空时变橙色/红色
                if (count == 0)
                {
                    deckCountText.color = Color.red;
                }
                else if (count <= 5)
                {
                    deckCountText.color = new Color(1f, 0.5f, 0f);
                }
                else
                {
                    deckCountText.color = Color.white;
                }
            }
        }

        /// <summary>
        /// 更新进化点
        /// </summary>
        public void UpdateEvolutionPoints(int available, int total)
        {
            _evolutionPoints = available;

            // 清除现有图标
            foreach (var icon in _epIcons)
            {
                if (icon != null)
                {
                    Destroy(icon.gameObject);
                }
            }
            _epIcons.Clear();

            // 创建新图标
            if (epContainer != null && epIconPrefab != null)
            {
                for (int i = 0; i < total; i++)
                {
                    var iconObj = Instantiate(epIconPrefab, epContainer);
                    var iconImage = iconObj.GetComponent<Image>();
                    if (iconImage != null)
                    {
                        iconImage.color = i < available ? epAvailableColor : epUsedColor;
                        _epIcons.Add(iconImage);
                    }
                }
            }
        }

        /// <summary>
        /// 设置为有效攻击目标
        /// </summary>
        public void SetValidAttackTarget(bool valid)
        {
            _isValidAttackTarget = valid;

            if (attackTargetHighlight != null)
            {
                attackTargetHighlight.SetActive(valid);
            }

            // 给头像添加颜色效果
            if (portrait != null)
            {
                portrait.color = valid ? attackTargetColor : Color.white;
            }
        }

        /// <summary>
        /// 清除高亮
        /// </summary>
        public void ClearHighlight()
        {
            SetValidAttackTarget(false);
        }

        /// <summary>
        /// 受伤动画
        /// </summary>
        public void PlayDamageAnimation(int amount)
        {
            Debug.Log($"PlayerInfoPanel: 受到 {amount} 点伤害");

            // 显示飘字
            ShowFloatingText($"-{amount}", Color.red);

            // 头像闪红
            if (portrait != null)
            {
                StartCoroutine(FlashColor(portrait, damageFlashColor, damageFlashDuration));
            }
        }

        /// <summary>
        /// 治疗动画
        /// </summary>
        public void PlayHealAnimation(int amount)
        {
            Debug.Log($"PlayerInfoPanel: 恢复 {amount} 点生命");

            // 显示飘字
            ShowFloatingText($"+{amount}", Color.green);
        }

        /// <summary>
        /// 显示飘字
        /// </summary>
        private void ShowFloatingText(string text, Color color)
        {
            if (floatingTextPrefab == null) return;

            Transform spawnPoint = floatingTextSpawnPoint != null ? floatingTextSpawnPoint : transform;
            var floatingObj = Instantiate(floatingTextPrefab, spawnPoint.position, Quaternion.identity, transform.parent);

            var textComponent = floatingObj.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = text;
                textComponent.color = color;
            }

            // 简单的上浮动画
            StartCoroutine(FloatAndFade(floatingObj, 1f));
        }

        /// <summary>
        /// 上浮并淡出
        /// </summary>
        private System.Collections.IEnumerator FloatAndFade(GameObject obj, float duration)
        {
            var rectTransform = obj.GetComponent<RectTransform>();
            var text = obj.GetComponent<TextMeshProUGUI>();

            if (rectTransform == null || text == null)
            {
                Destroy(obj);
                yield break;
            }

            Vector2 startPos = rectTransform.anchoredPosition;
            Color startColor = text.color;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 上浮
                rectTransform.anchoredPosition = startPos + Vector2.up * (50f * t);

                // 淡出
                text.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

                yield return null;
            }

            Destroy(obj);
        }

        /// <summary>
        /// 颜色闪烁
        /// </summary>
        private System.Collections.IEnumerator FlashColor(Image image, Color flashColor, float duration)
        {
            Color originalColor = image.color;
            image.color = flashColor;

            yield return new WaitForSeconds(duration);

            image.color = originalColor;
        }
    }
}
