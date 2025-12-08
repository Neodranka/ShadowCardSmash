using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShadowCardSmash.UI.Battle
{
    /// <summary>
    /// 牌库显示组件
    /// </summary>
    public class DeckPileDisplay : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI countText;
        public Image deckImage;
        public Button clickArea;

        [Header("Visual Settings")]
        public Color normalColor = Color.white;
        public Color lowCardColor = new Color(1f, 0.5f, 0f);
        public Color emptyColor = Color.red;

        // 牌库内容（用于显示列表）
        private List<int> _deckContents = new List<int>();
        private int _count;

        // 事件
        public event Action OnDeckClicked;

        void Start()
        {
            if (clickArea != null)
            {
                clickArea.onClick.AddListener(() => OnDeckClicked?.Invoke());
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

                // 根据剩余数量改变颜色
                if (count == 0)
                {
                    countText.color = emptyColor;
                }
                else if (count <= 5)
                {
                    countText.color = lowCardColor;
                }
                else
                {
                    countText.color = normalColor;
                }
            }

            // 牌库为空时改变图片透明度
            if (deckImage != null)
            {
                var color = deckImage.color;
                color.a = count == 0 ? 0.3f : 1f;
                deckImage.color = color;
            }
        }

        /// <summary>
        /// 设置牌库内容（用于查看功能）
        /// </summary>
        public void SetDeckContents(List<int> cardIds)
        {
            _deckContents = new List<int>(cardIds);
            UpdateCount(_deckContents.Count);
        }

        /// <summary>
        /// 获取牌库内容
        /// </summary>
        public List<int> GetDeckContents()
        {
            return _deckContents;
        }

        /// <summary>
        /// 播放抽牌动画
        /// </summary>
        public void PlayDrawAnimation()
        {
            // TODO: 实现抽牌动画（卡牌从牌库飞出）
            Debug.Log("DeckPileDisplay: 播放抽牌动画");
        }
    }
}
