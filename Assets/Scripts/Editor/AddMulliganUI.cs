using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using ShadowCardSmash.UI.Battle;
using ShadowCardSmash.Tests;

namespace ShadowCardSmash.Editor
{
    /// <summary>
    /// 向现有场景添加 Mulligan UI
    /// </summary>
    public class AddMulliganUI : EditorWindow
    {
        [MenuItem("Tools/CardGame/Add Mulligan UI to Scene")]
        public static void AddMulliganUIToScene()
        {
            // 查找 BattleCanvas
            var canvas = GameObject.Find("BattleCanvas");
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到 BattleCanvas，请确保在 Battle 场景中运行此工具", "确定");
                return;
            }

            // 查找 Popups
            var popups = canvas.transform.Find("Popups");
            if (popups == null)
            {
                // 创建 Popups
                var popupsObj = new GameObject("Popups");
                popupsObj.transform.SetParent(canvas.transform, false);
                var popupsRect = popupsObj.AddComponent<RectTransform>();
                popupsRect.anchorMin = Vector2.zero;
                popupsRect.anchorMax = Vector2.one;
                popupsRect.sizeDelta = Vector2.zero;
                popups = popupsObj.transform;
            }

            // 检查是否已存在 MulliganPanel
            var existingPanel = popups.Find("MulliganPanel");
            if (existingPanel != null)
            {
                if (!EditorUtility.DisplayDialog("确认", "MulliganPanel 已存在，是否删除并重新创建？", "是", "否"))
                {
                    return;
                }
                DestroyImmediate(existingPanel.gameObject);
            }

            // 创建 MulliganPanel (容器，不会被禁用)
            var panelRoot = CreateUIElement("MulliganPanel", popups, Vector2.zero, Vector2.zero);
            SetAnchors(panelRoot, Vector2.zero, Vector2.one, Vector2.zero);
            panelRoot.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

            var mulliganUI = panelRoot.AddComponent<MulliganUI>();

            // 创建实际的面板内容 (这个会被显示/隐藏)
            var panel = CreateUIElement("Panel", panelRoot.transform, Vector2.zero, Vector2.zero);
            SetAnchors(panel, Vector2.zero, Vector2.one, Vector2.zero);
            panel.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            mulliganUI.mulliganPanel = panel;

            // Background (半透明遮罩)
            var bg = CreateUIElement("Background", panel.transform, Vector2.zero, Vector2.zero);
            SetAnchors(bg, Vector2.zero, Vector2.one, Vector2.zero);
            bg.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.85f);

            // ContentPanel (居中面板)
            var content = CreateUIElement("ContentPanel", panel.transform, Vector2.zero, new Vector2(1200, 500));
            var contentImage = content.AddComponent<Image>();
            contentImage.color = new Color(0.2f, 0.15f, 0.1f, 1f);

            // InstructionText
            var instruction = CreateTextElement("InstructionText", content.transform, new Vector2(0, 200), new Vector2(1000, 40), "点击要换掉的牌，然后确认", 24);
            mulliganUI.instructionText = instruction.GetComponent<TextMeshProUGUI>();

            // CardContainer (Horizontal Layout)
            var cardContainer = CreateUIElement("CardContainer", content.transform, Vector2.zero, new Vector2(1100, 280));
            var hlg = cardContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            mulliganUI.cardContainer = cardContainer.transform;

            // SelectedCountText
            var selectedCount = CreateTextElement("SelectedCountText", content.transform, new Vector2(0, -160), new Vector2(200, 30), "已选择 0 张", 18);
            mulliganUI.selectedCountText = selectedCount.GetComponent<TextMeshProUGUI>();

            // ConfirmButton
            var confirmBtn = CreateButton("ConfirmButton", content.transform, "确认换牌", new Vector2(200, 60));
            confirmBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -210);
            mulliganUI.confirmButton = confirmBtn.GetComponent<Button>();

            // 初始隐藏内容面板
            panel.SetActive(false);

            // 连接到 HotSeatGameManager
            var hotSeatManager = Object.FindObjectOfType<HotSeatGameManager>();
            if (hotSeatManager != null)
            {
                hotSeatManager.mulliganUI = mulliganUI;
                EditorUtility.SetDirty(hotSeatManager);
                Debug.Log("AddMulliganUI: 已连接到 HotSeatGameManager");
            }
            else
            {
                Debug.LogWarning("AddMulliganUI: 未找到 HotSeatGameManager，请手动设置 mulliganUI 引用");
            }

            // 标记场景已修改
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("完成", "MulliganUI 已添加到场景！\n\n请保存场景。", "确定");
        }

        private static GameObject CreateUIElement(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rectTransform = obj.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;

            return obj;
        }

        private static GameObject CreateTextElement(string name, Transform parent, Vector2 position, Vector2 size, string text, int fontSize)
        {
            var obj = CreateUIElement(name, parent, position, size);

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            var layout = obj.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;

            return obj;
        }

        private static GameObject CreateButton(string name, Transform parent, string text, Vector2 size)
        {
            var obj = CreateUIElement(name, parent, Vector2.zero, size);

            var image = obj.AddComponent<Image>();
            image.color = new Color(0.4f, 0.3f, 0.2f, 1f);

            var button = obj.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.highlightedColor = new Color(0.5f, 0.4f, 0.3f, 1f);
            colors.pressedColor = new Color(0.3f, 0.2f, 0.1f, 1f);
            button.colors = colors;

            var textObj = CreateTextElement("Text", obj.transform, Vector2.zero, size, text, 16);

            var layout = obj.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;

            return obj;
        }

        private static void SetAnchors(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition)
        {
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
        }
    }
}
