using UnityEngine;
using TMPro;

public class FontManager : MonoBehaviour
{
    public static FontManager Instance { get; private set; }

    [Header("字体资源")]
    [Tooltip("中文 TMP 字体资源")]
    public TMP_FontAsset chineseFont;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupDefaultFont();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void SetupDefaultFont()
    {
        if (chineseFont != null)
        {
            Debug.Log($"[FontManager] 中文字体已加载: {chineseFont.name}");
            // 注意：TMP_Settings.defaultFontAsset 是只读的，
            // 需要通过编辑器工具设置，或在运行时使用 ApplyFontToAll
        }
        else
        {
            Debug.LogWarning("[FontManager] 未设置中文字体！请在 Inspector 中指定 chineseFont");
        }
    }

    void Start()
    {
        // 自动为场景中所有 Canvas 下的 TMP 组件应用字体
        if (chineseFont != null)
        {
            var allCanvases = FindObjectsOfType<Canvas>(true);
            foreach (var canvas in allCanvases)
            {
                ApplyFontToAll(canvas.gameObject);
            }
        }
    }

    /// <summary>
    /// 给指定 GameObject 及其所有子物体的 TMP 组件设置字体
    /// </summary>
    public void ApplyFontToAll(GameObject root)
    {
        if (chineseFont == null)
        {
            Debug.LogWarning("[FontManager] 无法应用字体：chineseFont 为空");
            return;
        }

        var tmpTexts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var tmp in tmpTexts)
        {
            tmp.font = chineseFont;
        }
        Debug.Log($"[FontManager] 已为 {tmpTexts.Length} 个 TMP 组件设置字体");
    }

    /// <summary>
    /// 给单个 TMP 组件设置字体
    /// </summary>
    public void ApplyFont(TextMeshProUGUI tmpText)
    {
        if (chineseFont != null && tmpText != null)
        {
            tmpText.font = chineseFont;
        }
    }
}
