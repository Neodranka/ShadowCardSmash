using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using System.IO;

public class FixCorruptedFont
{
    private const string CHINESE_FONT_PATH = "Assets/Resources/Fonts/ChineseFont SDF.asset";
    private const string CHINESE_FONT_MATERIAL_PATH = "Assets/Resources/Fonts/ChineseFont SDF Material.mat";

    [MenuItem("Tools/CardGame/Create Chinese Font (创建中文字体)")]
    public static void CreateChineseFont()
    {
        // 查找源字体
        Font sourceFont = null;
        string sourceFontPath = null;
        string[] fontPaths = new string[]
        {
            "Assets/Fonts/SourceHanSansSC-Regular.otf",
            "Assets/Fonts/SourceHanSansSC-Regular.ttf"
        };

        foreach (var path in fontPaths)
        {
            sourceFont = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (sourceFont != null)
            {
                sourceFontPath = path;
                break;
            }
        }

        if (sourceFont == null)
        {
            EditorUtility.DisplayDialog("错误",
                "未找到思源黑体！\n\n" +
                "请确保以下文件存在:\n" +
                "Assets/Fonts/SourceHanSansSC-Regular.otf",
                "确定");
            return;
        }

        // 删除旧的损坏字体
        if (File.Exists(CHINESE_FONT_PATH))
        {
            AssetDatabase.DeleteAsset(CHINESE_FONT_PATH);
        }
        if (File.Exists(CHINESE_FONT_PATH + ".meta"))
        {
            File.Delete(CHINESE_FONT_PATH + ".meta");
        }

        // 确保目录存在
        string dir = Path.GetDirectoryName(CHINESE_FONT_PATH);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        AssetDatabase.Refresh();

        // 使用 TMPro_FontAssetCreatorWindow 的方式创建字体
        // 创建动态字体资源
        var fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,  // Sampling Point Size
            9,   // Padding
            GlyphRenderMode.SDFAA,
            1024, // Atlas Width
            1024  // Atlas Height
        );

        if (fontAsset == null)
        {
            EditorUtility.DisplayDialog("错误", "创建字体资源失败！", "确定");
            return;
        }

        // 设置为动态模式
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        // 保存字体资源
        AssetDatabase.CreateAsset(fontAsset, CHINESE_FONT_PATH);

        // 创建并保存材质
        Material material = new Material(Shader.Find("TextMeshPro/Distance Field"));
        material.SetTexture("_MainTex", fontAsset.atlasTexture);
        fontAsset.material = material;

        // 将材质作为子资源保存
        AssetDatabase.AddObjectToAsset(material, fontAsset);

        // 如果有 atlas texture，也保存它
        if (fontAsset.atlasTexture != null)
        {
            fontAsset.atlasTexture.name = "ChineseFont SDF Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 验证字体是否有效
        var loadedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CHINESE_FONT_PATH);
        if (loadedFont != null && loadedFont.atlasTexture != null)
        {
            // 设置为 TMP 默认字体
            SetAsDefaultFont(loadedFont);

            // 替换场景和 Prefab 中的字体
            ReplaceFontsInProject(loadedFont);

            EditorUtility.DisplayDialog("成功",
                $"中文字体创建成功！\n\n" +
                $"源字体: {sourceFont.name}\n" +
                $"保存位置: {CHINESE_FONT_PATH}\n\n" +
                "已自动替换场景和 Prefab 中的字体。",
                "确定");

            Selection.activeObject = loadedFont;
        }
        else
        {
            EditorUtility.DisplayDialog("警告",
                "字体创建可能不完整，请尝试手动创建:\n\n" +
                "Window → TextMeshPro → Font Asset Creator",
                "确定");
        }
    }

    static void SetAsDefaultFont(TMP_FontAsset fontAsset)
    {
        string settingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(settingsPath);

        if (settings != null)
        {
            var so = new SerializedObject(settings);
            var defaultFontProp = so.FindProperty("m_defaultFontAsset");

            if (defaultFontProp != null)
            {
                defaultFontProp.objectReferenceValue = fontAsset;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log($"[CreateChineseFont] 已设置 {fontAsset.name} 为 TMP 默认字体");
            }
        }
    }

    static void ReplaceFontsInProject(TMP_FontAsset newFont)
    {
        // 替换所有 Prefab 中的字体
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
        foreach (var guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                bool modified = false;
                var tmps = prefab.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                foreach (var tmp in tmps)
                {
                    if (tmp.font != newFont)
                    {
                        tmp.font = newFont;
                        modified = true;
                    }
                }

                if (modified)
                {
                    EditorUtility.SetDirty(prefab);
                    Debug.Log($"[CreateChineseFont] 已更新 Prefab: {path}");
                }
            }
        }

        // 替换当前场景中的字体
        var sceneTmps = Object.FindObjectsOfType<TMPro.TextMeshProUGUI>(true);
        foreach (var tmp in sceneTmps)
        {
            if (tmp.font != newFont)
            {
                tmp.font = newFont;
                EditorUtility.SetDirty(tmp);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[CreateChineseFont] 字体替换完成");
    }
}
