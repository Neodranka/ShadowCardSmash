using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShadowCardSmash.UI.Battle
{
    /// <summary>
    /// 墓地显示组件
    /// </summary>
    public class GraveyardDisplay : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI countText;
        public Image graveyardImage;
        public Button clickArea;

        [Header("Visual Settings")]
        public Color normalColor = Color.white;
        public Color highlightColor = new Color(0.8f, 0.6f, 1f);

        // 墓地内容
        private List<int> _graveyardContents = new List<int>();
        private int _count;

        // 事件
        public event Action OnGraveyardClicked;

        void Start()
        {
            if (clickArea != null)
            {
                clickArea.onClick.AddListener(() => OnGraveyardClicked?.Invoke());
            }
        }

        /// <summary>
        /// 更新显示数量
        /// </summary>
        public void UpdateCount(int count)
        {
            _count = count;

            if (countText != null)
            {
                countText.text = count.ToString();
            }

            // 墓地为空时降低透明度
            if (graveyardImage != null)
            {
                var color = graveyardImage.color;
                color.a = count == 0 ? 0.3f : 1f;
                graveyardImage.color = color;
            }
        }

        /// <summary>
        /// 设置墓地内容
        /// </summary>
        public void SetGraveyardContents(List<int> cardIds)
        {
            _graveyardContents = new List<int>(cardIds);
            UpdateCount(_graveyardContents.Count);
        }

        /// <summary>
        /// 添加卡牌到墓地
        /// </summary>
        public void AddCard(int cardId)
        {
            _graveyardContents.Add(cardId);
            UpdateCount(_graveyardContents.Count);

            // 播放添加动画
            PlayAddAnimation();
        }

        /// <summary>
        /// 获取墓地内容
        /// </summary>
        public List<int> GetGraveyardContents()
        {
            return _graveyardContents;
        }

        /// <summary>
        /// 清空墓地
        /// </summary>
        public void Clear()
        {
            _graveyardContents.Clear();
            UpdateCount(0);
        }

        /// <summary>
        /// 播放添加动画
        /// </summary>
        private void PlayAddAnimation()
        {
            // TODO: 实现卡牌飞入墓地的动画
            Debug.Log("GraveyardDisplay: 播放添加卡牌动画");

            // 简单的高亮效果
            if (graveyardImage != null)
            {
                StartCoroutine(FlashHighlight());
            }
        }

        private System.Collections.IEnumerator FlashHighlight()
        {
            if (graveyardImage == null) yield break;

            Color originalColor = graveyardImage.color;
            graveyardImage.color = highlightColor;

            yield return new WaitForSeconds(0.2f);

            graveyardImage.color = originalColor;
        }
    }
}
