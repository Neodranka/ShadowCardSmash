using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;
using ShadowCardSmash.UI.Battle;
using ShadowCardSmash.Tests;
using ShadowCardSmash.Managers;

namespace ShadowCardSmash.Editor
{
    /// <summary>
    /// 战斗界面UI自动生成器
    /// </summary>
    public class BattleUIGenerator : EditorWindow
    {
        // UI 尺寸常量
        private const int CANVAS_WIDTH = 1920;
        private const int CANVAS_HEIGHT = 1080;
        private const int TOP_PANEL_HEIGHT = 80;
        private const int BOTTOM_PANEL_HEIGHT = 80;
        private const int OPPONENT_HAND_HEIGHT = 150;
        private const int MY_HAND_HEIGHT = 200;
        private const int SIDE_PANEL_WIDTH = 120;
        private const int TILE_WIDTH = 180;
        private const int TILE_HEIGHT = 250;
        private const int CARD_WIDTH = 160;
        private const int CARD_HEIGHT = 220;
        private const int TILE_COUNT = 6;

        // 颜色常量
        private static readonly Color PANEL_BG_COLOR = new Color(0.24f, 0.16f, 0.09f, 1f); // #3D2817
        private static readonly Color HAND_BG_COLOR = new Color(0f, 0f, 0f, 0.5f);
        private static readonly Color BATTLEFIELD_BG_COLOR = new Color(0.1f, 0.18f, 0.1f, 1f); // #1A2F1A
        private static readonly Color TILE_BG_COLOR = new Color(0.55f, 0.45f, 0.33f, 0.5f); // #8B7355
        private static readonly Color BUTTON_COLOR = new Color(0.4f, 0.3f, 0.2f, 1f);
        private static readonly Color BRONZE_COLOR = new Color(0.8f, 0.5f, 0.2f, 1f);

        // 路径常量
        private const string PREFAB_PATH = "Assets/Prefabs/UI";
        private const string SCENE_PATH = "Assets/Scenes";
        private const string CHINESE_FONT_PATH = "Assets/Resources/Fonts/ChineseFont SDF.asset";

        // 生成的引用
        private static GameObject _canvas;
        private static TMP_FontAsset _chineseFont;
        private static BattleUIController _battleUIController;
        private static HotSeatGameManager _hotSeatManager;

        [MenuItem("Tools/CardGame/Generate Battle UI")]
        public static void GenerateBattleUI()
        {
            if (!EditorUtility.DisplayDialog("生成战斗UI",
                "这将创建新的Battle场景和所有UI Prefab。\n\n确定要继续吗？",
                "确定", "取消"))
            {
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("生成战斗UI", "准备中...", 0f);

                // 加载中文字体
                LoadChineseFont();

                // 确保目录存在
                EnsureDirectoriesExist();

                // 创建新场景
                EditorUtility.DisplayProgressBar("生成战斗UI", "创建场景...", 0.1f);
                CreateNewScene();

                // 创建Prefabs
                EditorUtility.DisplayProgressBar("生成战斗UI", "创建Prefabs...", 0.2f);
                CreatePrefabs();

                // 创建Canvas和UI结构
                EditorUtility.DisplayProgressBar("生成战斗UI", "创建UI结构...", 0.4f);
                CreateCanvasStructure();

                // 连接引用
                EditorUtility.DisplayProgressBar("生成战斗UI", "连接引用...", 0.8f);
                ConnectReferences();

                // 保存场景
                EditorUtility.DisplayProgressBar("生成战斗UI", "保存场景...", 0.9f);
                SaveScene();

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("完成", "战斗UI生成完成！\n\n场景已保存到：Assets/Scenes/Battle.unity", "确定");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("错误", $"生成UI时发生错误：\n{e.Message}", "确定");
                Debug.LogError($"BattleUIGenerator Error: {e}");
            }
        }

        private static void LoadChineseFont()
        {
            // 首先尝试使用 TMP 内置的 LiberationSans（最稳定）
            var liberationSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (liberationSans != null && IsFontAssetValid(liberationSans))
            {
                _chineseFont = liberationSans;
                Debug.Log($"[BattleUIGenerator] 使用内置字体: {_chineseFont.name}");
                return;
            }

            // 尝试加载中文字体
            _chineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CHINESE_FONT_PATH);

            // 检查字体是否损坏（atlas texture 是否有效）
            if (_chineseFont != null && !IsFontAssetValid(_chineseFont))
            {
                Debug.LogWarning("[BattleUIGenerator] ChineseFont SDF 已损坏，使用备用字体");
                _chineseFont = null;
            }

            if (_chineseFont == null)
            {
                // 尝试从其他位置加载有效的字体
                string[] fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
                foreach (var guid in fontGuids)
                {
                    string fontPath = AssetDatabase.GUIDToAssetPath(guid);
                    var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
                    if (font != null && IsFontAssetValid(font))
                    {
                        _chineseFont = font;
                        Debug.Log($"[BattleUIGenerator] 找到有效字体: {font.name}");
                        break;
                    }
                }
            }

            if (_chineseFont != null)
            {
                Debug.Log($"[BattleUIGenerator] 已加载字体: {_chineseFont.name}");
            }
            else
            {
                Debug.LogError("[BattleUIGenerator] 无法加载任何有效字体！请确保 TextMesh Pro 已正确导入。");
            }
        }

        private static bool IsFontAssetValid(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null) return false;

            try
            {
                // 检查 atlas texture 是否有效
                if (fontAsset.atlasTexture == null) return false;

                // 尝试访问纹理属性，如果损坏会抛出异常
                var width = fontAsset.atlasTexture.width;
                return width > 0;
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(PREFAB_PATH))
            {
                Directory.CreateDirectory(PREFAB_PATH);
            }
            if (!Directory.Exists(SCENE_PATH))
            {
                Directory.CreateDirectory(SCENE_PATH);
            }
            AssetDatabase.Refresh();
        }

        private static void CreateNewScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 设置相机背景色
            var camera = Camera.main;
            if (camera != null)
            {
                camera.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            }
        }

        #region Prefab Creation

        private static void CreatePrefabs()
        {
            CreateCardViewPrefab();
            CreateCardBackPrefab();
            CreateTileSlotPrefab();
            CreateEPIconPrefab();
            CreateFloatingTextPrefab();
        }

        private static void CreateCardViewPrefab()
        {
            var cardView = new GameObject("CardView");
            var rectTransform = cardView.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(CARD_WIDTH, CARD_HEIGHT);

            // 添加CardViewController组件
            var controller = cardView.AddComponent<CardViewController>();

            // CardFrame (边框背景)
            var frame = CreateUIElement("CardFrame", cardView.transform, Vector2.zero, new Vector2(CARD_WIDTH, CARD_HEIGHT));
            var frameImage = frame.AddComponent<Image>();
            frameImage.color = BRONZE_COLOR;
            controller.cardFrame = frameImage;

            // CardArt (卡牌插画)
            var art = CreateUIElement("CardArt", cardView.transform, new Vector2(0, 15), new Vector2(CARD_WIDTH - 20, CARD_HEIGHT - 70));
            var artImage = art.AddComponent<Image>();
            artImage.color = new Color(0.3f, 0.3f, 0.4f, 1f);
            controller.cardArt = artImage;

            // CardNameBG (名称背景)
            var nameBG = CreateUIElement("CardNameBG", cardView.transform, new Vector2(0, -65), new Vector2(CARD_WIDTH - 10, 30));
            var nameBGImage = nameBG.AddComponent<Image>();
            nameBGImage.color = new Color(0.2f, 0.15f, 0.1f, 0.9f);

            // CardNameText
            var nameText = CreateTextElement("CardNameText", nameBG.transform, Vector2.zero, new Vector2(CARD_WIDTH - 20, 26), "卡牌名称", 14);
            controller.cardNameText = nameText.GetComponent<TextMeshProUGUI>();

            // CostGem (费用宝石)
            var costGem = CreateUIElement("CostGem", cardView.transform, new Vector2(-60, 95), new Vector2(36, 36));
            var costGemImage = costGem.AddComponent<Image>();
            costGemImage.color = new Color(0.2f, 0.4f, 0.8f, 1f);
            MakeCircle(costGem);

            // CostText
            var costText = CreateTextElement("CostText", costGem.transform, Vector2.zero, new Vector2(30, 30), "0", 20);
            controller.costText = costText.GetComponent<TextMeshProUGUI>();

            // AttackHealthGroup
            var atkHpGroup = CreateUIElement("AttackHealthGroup", cardView.transform, new Vector2(0, -95), new Vector2(CARD_WIDTH, 30));
            controller.attackHealthGroup = atkHpGroup;

            // AttackIcon
            var atkIcon = CreateUIElement("AttackIcon", atkHpGroup.transform, new Vector2(-50, 0), new Vector2(40, 40));
            var atkIconImage = atkIcon.AddComponent<Image>();
            atkIconImage.color = new Color(0.8f, 0.6f, 0.2f, 1f);

            // AttackText
            var atkText = CreateTextElement("AttackText", atkIcon.transform, Vector2.zero, new Vector2(36, 36), "0", 18);
            controller.attackText = atkText.GetComponent<TextMeshProUGUI>();

            // HealthIcon
            var hpIcon = CreateUIElement("HealthIcon", atkHpGroup.transform, new Vector2(50, 0), new Vector2(40, 40));
            var hpIconImage = hpIcon.AddComponent<Image>();
            hpIconImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            // HealthText
            var hpText = CreateTextElement("HealthText", hpIcon.transform, Vector2.zero, new Vector2(36, 36), "0", 18);
            controller.healthText = hpText.GetComponent<TextMeshProUGUI>();

            // State Indicators
            controller.evolvedIndicator = CreateIndicator("EvolvedIndicator", cardView.transform, new Color(1f, 0.8f, 0f, 0.8f));
            controller.summoningSicknessIndicator = CreateIndicator("SummoningSicknessIndicator", cardView.transform, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            controller.canAttackGlow = CreateGlowIndicator("CanAttackGlow", cardView.transform, new Color(0f, 1f, 0f, 0.5f));
            controller.selectionHighlight = CreateGlowIndicator("SelectionHighlight", cardView.transform, new Color(1f, 1f, 0f, 0.6f));
            controller.playableHighlight = CreateGlowIndicator("PlayableHighlight", cardView.transform, new Color(0.5f, 1f, 0.5f, 0.4f));

            // 保存Prefab
            SavePrefab(cardView, "CardView");
            Object.DestroyImmediate(cardView);
        }

        private static void CreateCardBackPrefab()
        {
            var cardBack = new GameObject("CardBack");
            var rectTransform = cardBack.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(CARD_WIDTH, CARD_HEIGHT);

            // 背景
            var bg = cardBack.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.1f, 0.25f, 1f);

            // 装饰图案
            var pattern = CreateUIElement("Pattern", cardBack.transform, Vector2.zero, new Vector2(CARD_WIDTH - 20, CARD_HEIGHT - 20));
            var patternImage = pattern.AddComponent<Image>();
            patternImage.color = new Color(0.25f, 0.15f, 0.35f, 1f);

            // 边框
            var border = CreateUIElement("Border", cardBack.transform, Vector2.zero, new Vector2(CARD_WIDTH, CARD_HEIGHT));
            var borderImage = border.AddComponent<Image>();
            borderImage.color = new Color(0.4f, 0.3f, 0.5f, 1f);
            // 设置为只显示边框
            border.transform.SetAsFirstSibling();

            SavePrefab(cardBack, "CardBack");
            Object.DestroyImmediate(cardBack);
        }

        private static void CreateTileSlotPrefab()
        {
            var tileSlot = new GameObject("TileSlot");
            var rectTransform = tileSlot.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(TILE_WIDTH, TILE_HEIGHT);

            var controller = tileSlot.AddComponent<TileSlotController>();

            // TileBackground
            var bg = CreateUIElement("TileBackground", tileSlot.transform, Vector2.zero, new Vector2(TILE_WIDTH, TILE_HEIGHT));
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = TILE_BG_COLOR;
            controller.tileBackground = bgImage;

            // OccupantHolder
            var holder = CreateUIElement("OccupantHolder", tileSlot.transform, Vector2.zero, new Vector2(CARD_WIDTH, CARD_HEIGHT));
            controller.occupantHolder = holder.transform;

            // ValidTargetHighlight
            controller.validTargetHighlight = CreateGlowIndicator("ValidTargetHighlight", tileSlot.transform, new Color(1f, 0.3f, 0.3f, 0.5f), TILE_WIDTH, TILE_HEIGHT);

            // ValidPlacementHighlight
            controller.validPlacementHighlight = CreateGlowIndicator("ValidPlacementHighlight", tileSlot.transform, new Color(0.3f, 1f, 0.3f, 0.5f), TILE_WIDTH, TILE_HEIGHT);

            // TileEffectIndicator
            var effectIndicator = CreateUIElement("TileEffectIndicator", tileSlot.transform, new Vector2(0, -100), new Vector2(30, 30));
            var effectImage = effectIndicator.AddComponent<Image>();
            effectImage.color = new Color(0.5f, 0.5f, 1f, 0.8f);
            effectIndicator.SetActive(false);
            controller.tileEffectIndicator = effectIndicator;

            SavePrefab(tileSlot, "TileSlot");
            Object.DestroyImmediate(tileSlot);
        }

        private static void CreateEPIconPrefab()
        {
            var epIcon = new GameObject("EPIcon");
            var rectTransform = epIcon.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(24, 24);

            var image = epIcon.AddComponent<Image>();
            image.color = new Color(1f, 0.84f, 0f, 1f);
            MakeCircle(epIcon);

            SavePrefab(epIcon, "EPIcon");
            Object.DestroyImmediate(epIcon);
        }

        private static void CreateFloatingTextPrefab()
        {
            var floatingText = new GameObject("FloatingText");
            var rectTransform = floatingText.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 40);

            var tmp = floatingText.AddComponent<TextMeshProUGUI>();
            tmp.text = "+0";
            tmp.fontSize = 24;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            // 设置中文字体
            if (_chineseFont != null)
            {
                tmp.font = _chineseFont;
            }

            SavePrefab(floatingText, "FloatingText");
            Object.DestroyImmediate(floatingText);
        }

        #endregion

        #region Canvas Structure Creation

        private static void CreateCanvasStructure()
        {
            // 创建Canvas
            _canvas = CreateCanvas();

            // 创建各层级
            CreateBackground();
            CreateTopPanel();
            CreateOpponentHandArea();
            CreateBattlefieldArea();
            CreateMyHandArea();
            CreateBottomPanel();
            CreatePopups();
            CreateGameManager();
        }

        private static GameObject CreateCanvas()
        {
            var canvasObj = new GameObject("BattleCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CANVAS_WIDTH, CANVAS_HEIGHT);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            return canvasObj;
        }

        private static void CreateBackground()
        {
            var bg = CreateUIElement("Background", _canvas.transform, Vector2.zero, new Vector2(CANVAS_WIDTH, CANVAS_HEIGHT));
            bg.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bg.GetComponent<RectTransform>().anchorMax = Vector2.one;
            bg.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            bg.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            var image = bg.AddComponent<Image>();
            image.color = new Color(0.15f, 0.12f, 0.1f, 1f);
        }

        private static void CreateTopPanel()
        {
            var panel = CreateUIElement("TopPanel", _canvas.transform, Vector2.zero, new Vector2(0, TOP_PANEL_HEIGHT));
            SetAnchors(panel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -TOP_PANEL_HEIGHT / 2));
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(0, TOP_PANEL_HEIGHT);

            var image = panel.AddComponent<Image>();
            image.color = PANEL_BG_COLOR;

            // 添加HorizontalLayoutGroup
            var hlg = panel.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(20, 20, 10, 10);
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // 创建PlayerInfoPanel组件
            var infoPanel = panel.AddComponent<PlayerInfoPanel>();

            // OpponentPortrait
            var portrait = CreateUIElement("OpponentPortrait", panel.transform, Vector2.zero, new Vector2(60, 60));
            var portraitImage = portrait.AddComponent<Image>();
            portraitImage.color = new Color(0.5f, 0.4f, 0.3f, 1f);
            infoPanel.portrait = portraitImage;

            // HealthText
            var healthText = CreateTextElement("HealthText", panel.transform, Vector2.zero, new Vector2(100, 40), "40/40", 20);
            infoPanel.healthText = healthText.GetComponent<TextMeshProUGUI>();

            // ManaText
            var manaText = CreateTextElement("ManaText", panel.transform, Vector2.zero, new Vector2(80, 40), "0/0", 18);
            infoPanel.manaText = manaText.GetComponent<TextMeshProUGUI>();

            // Spacer
            var spacer = CreateUIElement("Spacer", panel.transform, Vector2.zero, new Vector2(200, 40));
            var spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.flexibleWidth = 1;

            // HandCountText
            var handCount = CreateTextElement("HandCountText", panel.transform, Vector2.zero, new Vector2(60, 40), "手牌:0", 16);
            infoPanel.handCountText = handCount.GetComponent<TextMeshProUGUI>();

            // DeckCountText
            var deckCount = CreateTextElement("DeckCountText", panel.transform, Vector2.zero, new Vector2(60, 40), "牌库:0", 16);
            infoPanel.deckCountText = deckCount.GetComponent<TextMeshProUGUI>();

            // EPContainer
            var epContainer = CreateUIElement("EPContainer", panel.transform, Vector2.zero, new Vector2(100, 40));
            var epHlg = epContainer.AddComponent<HorizontalLayoutGroup>();
            epHlg.spacing = 5;
            epHlg.childAlignment = TextAnchor.MiddleCenter;
            infoPanel.epContainer = epContainer.transform;

            // 加载EP图标预制体
            var epIconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_PATH}/EPIcon.prefab");
            infoPanel.epIconPrefab = epIconPrefab;
        }

        private static void CreateOpponentHandArea()
        {
            var area = CreateUIElement("OpponentHandArea", _canvas.transform, Vector2.zero, new Vector2(0, OPPONENT_HAND_HEIGHT));
            SetAnchors(area, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -TOP_PANEL_HEIGHT - OPPONENT_HAND_HEIGHT / 2));
            area.GetComponent<RectTransform>().sizeDelta = new Vector2(0, OPPONENT_HAND_HEIGHT);

            var image = area.AddComponent<Image>();
            image.color = HAND_BG_COLOR;

            // HandContainer
            var container = CreateUIElement("OpponentHandContainer", area.transform, Vector2.zero, new Vector2(1200, OPPONENT_HAND_HEIGHT - 20));
            var hlg = container.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // 添加HandAreaController
            var handController = area.AddComponent<HandAreaController>();
            handController.isOpponentHand = true;
            handController.handContainer = container.transform;

            // 加载预制体
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_PATH}/CardView.prefab");
            var cardBackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_PATH}/CardBack.prefab");
            handController.cardPrefab = cardPrefab;
            handController.cardBackPrefab = cardBackPrefab;
        }

        private static void CreateBattlefieldArea()
        {
            float topOffset = TOP_PANEL_HEIGHT + OPPONENT_HAND_HEIGHT;
            float bottomOffset = BOTTOM_PANEL_HEIGHT + MY_HAND_HEIGHT;
            float centerY = (CANVAS_HEIGHT - topOffset - bottomOffset) / 2 + bottomOffset - CANVAS_HEIGHT / 2;

            var area = CreateUIElement("BattlefieldArea", _canvas.transform, new Vector2(0, centerY),
                new Vector2(CANVAS_WIDTH, CANVAS_HEIGHT - topOffset - bottomOffset));
            SetAnchors(area, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, (bottomOffset - topOffset) / 2));
            area.GetComponent<RectTransform>().offsetMin = new Vector2(0, bottomOffset);
            area.GetComponent<RectTransform>().offsetMax = new Vector2(0, -topOffset);

            var image = area.AddComponent<Image>();
            image.color = BATTLEFIELD_BG_COLOR;

            // LeftSidePanel
            CreateSidePanel("LeftSidePanel", area.transform, true);

            // RightSidePanel
            CreateSidePanel("RightSidePanel", area.transform, false);

            // CenterBattlefield
            CreateCenterBattlefield(area.transform);
        }

        private static void CreateSidePanel(string name, Transform parent, bool isLeft)
        {
            var panel = CreateUIElement(name, parent, Vector2.zero, new Vector2(SIDE_PANEL_WIDTH, 0));

            if (isLeft)
            {
                SetAnchors(panel, new Vector2(0, 0), new Vector2(0, 1), new Vector2(SIDE_PANEL_WIDTH / 2, 0));
            }
            else
            {
                SetAnchors(panel, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-SIDE_PANEL_WIDTH / 2, 0));
            }
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(SIDE_PANEL_WIDTH, 0);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 20, 20);
            vlg.spacing = 40;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandHeight = false;

            // 对手牌库/墓地
            string opponentLabel = isLeft ? "OpponentDeckPile" : "OpponentGraveyard";
            CreateDeckOrGraveyard(opponentLabel, panel.transform, isLeft);

            // 添加间隔
            var spacer = CreateUIElement("Spacer", panel.transform, Vector2.zero, new Vector2(100, 50));
            var spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.flexibleHeight = 1;

            // 我方牌库/墓地
            string myLabel = isLeft ? "MyDeckPile" : "MyGraveyard";
            CreateDeckOrGraveyard(myLabel, panel.transform, isLeft);
        }

        private static void CreateDeckOrGraveyard(string name, Transform parent, bool isDeck)
        {
            var obj = CreateUIElement(name, parent, Vector2.zero, new Vector2(100, 140));

            var image = obj.AddComponent<Image>();
            image.color = isDeck ? new Color(0.2f, 0.15f, 0.3f, 1f) : new Color(0.3f, 0.2f, 0.2f, 1f);

            var button = obj.AddComponent<Button>();
            button.targetGraphic = image;

            // CountText
            var countText = CreateTextElement("CountText", obj.transform, new Vector2(0, -50), new Vector2(80, 30), "0", 18);

            // Label
            var labelText = CreateTextElement("Label", obj.transform, new Vector2(0, 50), new Vector2(80, 20), isDeck ? "牌库" : "墓地", 12);

            if (isDeck)
            {
                var deckDisplay = obj.AddComponent<DeckPileDisplay>();
                deckDisplay.countText = countText.GetComponent<TextMeshProUGUI>();
                deckDisplay.deckImage = image;
                deckDisplay.clickArea = button;
            }
            else
            {
                var graveyardDisplay = obj.AddComponent<GraveyardDisplay>();
                graveyardDisplay.countText = countText.GetComponent<TextMeshProUGUI>();
                graveyardDisplay.graveyardImage = image;
                graveyardDisplay.clickArea = button;
            }
        }

        private static void CreateCenterBattlefield(Transform parent)
        {
            var center = CreateUIElement("CenterBattlefield", parent, Vector2.zero, new Vector2(0, 0));
            SetAnchors(center, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero);
            center.GetComponent<RectTransform>().offsetMin = new Vector2(SIDE_PANEL_WIDTH, 0);
            center.GetComponent<RectTransform>().offsetMax = new Vector2(-SIDE_PANEL_WIDTH, 0);

            // 不使用 VerticalLayoutGroup，让子元素各自定位

            // OpponentField - 在中心偏上
            CreateFieldRow("OpponentField", center.transform, true, 70f);

            // MyField - 在中心偏下
            CreateFieldRow("MyField", center.transform, false, -70f);
        }

        private static void CreateFieldRow(string name, Transform parent, bool isOpponent, float yOffset)
        {
            var row = CreateUIElement(name, parent, Vector2.zero, new Vector2(0, TILE_HEIGHT));
            var rect = row.GetComponent<RectTransform>();

            // 中心锚点
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, yOffset);

            // 计算宽度：6个格子 + 5个间距
            float totalWidth = TILE_COUNT * TILE_WIDTH + (TILE_COUNT - 1) * 15;
            rect.sizeDelta = new Vector2(totalWidth, TILE_HEIGHT);

            // 添加 HorizontalLayoutGroup
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            // 加载TileSlot预制体
            var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_PATH}/TileSlot.prefab");

            // 创建6个格子
            for (int i = 0; i < TILE_COUNT; i++)
            {
                GameObject tile;
                if (tilePrefab != null)
                {
                    tile = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab);
                    tile.transform.SetParent(row.transform, false);
                }
                else
                {
                    tile = CreateUIElement($"TileSlot_{i}", row.transform, Vector2.zero, new Vector2(TILE_WIDTH, TILE_HEIGHT));
                    var tileImage = tile.AddComponent<Image>();
                    tileImage.color = TILE_BG_COLOR;
                    tile.AddComponent<TileSlotController>();
                }
                tile.name = $"TileSlot_{i}";

                var controller = tile.GetComponent<TileSlotController>();
                if (controller != null)
                {
                    controller.tileIndex = i;
                    controller.isOpponentTile = isOpponent;
                }
            }
        }

        private static void CreateMyHandArea()
        {
            var area = CreateUIElement("MyHandArea", _canvas.transform, Vector2.zero, new Vector2(0, MY_HAND_HEIGHT));
            SetAnchors(area, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, BOTTOM_PANEL_HEIGHT + MY_HAND_HEIGHT / 2));
            area.GetComponent<RectTransform>().sizeDelta = new Vector2(0, MY_HAND_HEIGHT);

            var image = area.AddComponent<Image>();
            image.color = HAND_BG_COLOR;

            // HandContainer
            var container = CreateUIElement("MyHandContainer", area.transform, Vector2.zero, new Vector2(1400, MY_HAND_HEIGHT - 20));
            var hlg = container.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // 添加HandAreaController
            var handController = area.AddComponent<HandAreaController>();
            handController.isOpponentHand = false;
            handController.handContainer = container.transform;

            // 加载预制体
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_PATH}/CardView.prefab");
            var cardBackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_PATH}/CardBack.prefab");
            handController.cardPrefab = cardPrefab;
            handController.cardBackPrefab = cardBackPrefab;
        }

        private static void CreateBottomPanel()
        {
            var panel = CreateUIElement("BottomPanel", _canvas.transform, Vector2.zero, new Vector2(0, BOTTOM_PANEL_HEIGHT));
            SetAnchors(panel, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, BOTTOM_PANEL_HEIGHT / 2));
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(0, BOTTOM_PANEL_HEIGHT);

            var image = panel.AddComponent<Image>();
            image.color = PANEL_BG_COLOR;

            var hlg = panel.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(20, 20, 10, 10);
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // 创建PlayerInfoPanel组件
            var infoPanel = panel.AddComponent<PlayerInfoPanel>();

            // MyPortrait
            var portrait = CreateUIElement("MyPortrait", panel.transform, Vector2.zero, new Vector2(60, 60));
            var portraitImage = portrait.AddComponent<Image>();
            portraitImage.color = new Color(0.4f, 0.5f, 0.3f, 1f);
            infoPanel.portrait = portraitImage;

            // HealthText
            var healthText = CreateTextElement("HealthText", panel.transform, Vector2.zero, new Vector2(100, 40), "40/40", 20);
            infoPanel.healthText = healthText.GetComponent<TextMeshProUGUI>();

            // ManaText
            var manaText = CreateTextElement("ManaText", panel.transform, Vector2.zero, new Vector2(80, 40), "0/0", 18);
            infoPanel.manaText = manaText.GetComponent<TextMeshProUGUI>();

            // Spacer
            var spacer = CreateUIElement("Spacer", panel.transform, Vector2.zero, new Vector2(100, 40));
            var spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.flexibleWidth = 1;

            // HandCountText
            var handCount = CreateTextElement("HandCountText", panel.transform, Vector2.zero, new Vector2(60, 40), "手牌:0", 16);
            infoPanel.handCountText = handCount.GetComponent<TextMeshProUGUI>();

            // EPContainer
            var epContainer = CreateUIElement("EPContainer", panel.transform, Vector2.zero, new Vector2(100, 40));
            var epHlg = epContainer.AddComponent<HorizontalLayoutGroup>();
            epHlg.spacing = 5;
            epHlg.childAlignment = TextAnchor.MiddleCenter;
            infoPanel.epContainer = epContainer.transform;

            var epIconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_PATH}/EPIcon.prefab");
            infoPanel.epIconPrefab = epIconPrefab;

            // EvolveButton
            var evolveBtn = CreateButton("EvolveButton", panel.transform, "进化", new Vector2(100, 50));

            // EndTurnButton
            var endTurnBtn = CreateButton("EndTurnButton", panel.transform, "结束回合", new Vector2(120, 50));
        }

        private static void CreatePopups()
        {
            var popups = CreateUIElement("Popups", _canvas.transform, Vector2.zero, Vector2.zero);
            SetAnchors(popups, Vector2.zero, Vector2.one, Vector2.zero);
            popups.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

            CreateCardDetailPopup(popups.transform);
            CreateCardListPopup(popups.transform);
            CreatePlayerSwitchPrompt(popups.transform);
            CreateGameOverPanel(popups.transform);
        }

        private static void CreateCardDetailPopup(Transform parent)
        {
            var popup = CreateUIElement("CardDetailPopup", parent, Vector2.zero, Vector2.zero);
            SetAnchors(popup, Vector2.zero, Vector2.one, Vector2.zero);
            popup.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

            var controller = popup.AddComponent<CardDetailPopup>();
            controller.popupRoot = popup;

            // DarkOverlay
            var overlay = CreateUIElement("DarkOverlay", popup.transform, Vector2.zero, Vector2.zero);
            SetAnchors(overlay, Vector2.zero, Vector2.one, Vector2.zero);
            overlay.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.7f);
            var overlayBtn = overlay.AddComponent<Button>();
            overlayBtn.targetGraphic = overlayImage;
            controller.overlayButton = overlayBtn;

            // PopupPanel
            var panel = CreateUIElement("PopupPanel", popup.transform, Vector2.zero, new Vector2(400, 500));
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.2f, 0.15f, 0.1f, 0.95f);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 15;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // CardFrame
            var cardFrame = CreateUIElement("CardFrame", panel.transform, Vector2.zero, new Vector2(240, 330));
            var cardFrameImage = cardFrame.AddComponent<Image>();
            cardFrameImage.color = BRONZE_COLOR;
            controller.cardFrame = cardFrameImage;
            var cardFrameLayout = cardFrame.AddComponent<LayoutElement>();
            cardFrameLayout.preferredWidth = 240;
            cardFrameLayout.preferredHeight = 330;

            // CardArt inside frame
            var cardArt = CreateUIElement("CardArt", cardFrame.transform, new Vector2(0, 20), new Vector2(200, 200));
            var cardArtImage = cardArt.AddComponent<Image>();
            cardArtImage.color = new Color(0.3f, 0.3f, 0.4f, 1f);
            controller.cardArt = cardArtImage;

            // CardName
            var cardName = CreateTextElement("CardNameText", panel.transform, Vector2.zero, new Vector2(360, 30), "卡牌名称", 22);
            controller.cardNameText = cardName.GetComponent<TextMeshProUGUI>();

            // CardType
            var cardType = CreateTextElement("CardTypeText", panel.transform, Vector2.zero, new Vector2(360, 24), "随从", 16);
            controller.cardTypeText = cardType.GetComponent<TextMeshProUGUI>();

            // Description
            var desc = CreateTextElement("DescriptionText", panel.transform, Vector2.zero, new Vector2(360, 60), "卡牌描述文本", 14);
            desc.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
            controller.descriptionText = desc.GetComponent<TextMeshProUGUI>();

            // CloseButton
            var closeBtn = CreateButton("CloseButton", panel.transform, "关闭", new Vector2(100, 40));
            controller.closeButton = closeBtn.GetComponent<Button>();

            popup.SetActive(false);
        }

        private static void CreateCardListPopup(Transform parent)
        {
            var popup = CreateUIElement("CardListPopup", parent, Vector2.zero, Vector2.zero);
            SetAnchors(popup, Vector2.zero, Vector2.one, Vector2.zero);
            popup.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

            var controller = popup.AddComponent<CardListPopup>();
            controller.popupRoot = popup;

            // DarkOverlay
            var overlay = CreateUIElement("DarkOverlay", popup.transform, Vector2.zero, Vector2.zero);
            SetAnchors(overlay, Vector2.zero, Vector2.one, Vector2.zero);
            overlay.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.7f);
            var overlayBtn = overlay.AddComponent<Button>();
            overlayBtn.targetGraphic = overlayImage;
            controller.overlayButton = overlayBtn;

            // PopupPanel
            var panel = CreateUIElement("PopupPanel", popup.transform, Vector2.zero, new Vector2(600, 700));
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.2f, 0.15f, 0.1f, 0.95f);

            // Title
            var title = CreateTextElement("TitleText", panel.transform, new Vector2(0, 320), new Vector2(560, 40), "牌库", 24);
            controller.titleText = title.GetComponent<TextMeshProUGUI>();

            // ScrollView
            var scrollView = CreateUIElement("ScrollView", panel.transform, new Vector2(0, -20), new Vector2(560, 550));
            var scrollRect = scrollView.AddComponent<ScrollRect>();
            var scrollImage = scrollView.AddComponent<Image>();
            scrollImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            var scrollMask = scrollView.AddComponent<Mask>();
            scrollMask.showMaskGraphic = true;
            controller.scrollRect = scrollRect;

            // Content
            var content = CreateUIElement("Content", scrollView.transform, Vector2.zero, new Vector2(540, 100));
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 5;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            scrollRect.viewport = scrollView.GetComponent<RectTransform>();
            controller.contentContainer = content.transform;

            // CardCount
            var cardCount = CreateTextElement("CardCountText", panel.transform, new Vector2(0, -310), new Vector2(200, 30), "共 0 张", 16);
            controller.cardCountText = cardCount.GetComponent<TextMeshProUGUI>();

            // CloseButton
            var closeBtn = CreateButton("CloseButton", panel.transform, "关闭", new Vector2(100, 40));
            closeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -340);
            controller.closeButton = closeBtn.GetComponent<Button>();

            popup.SetActive(false);
        }

        private static void CreatePlayerSwitchPrompt(Transform parent)
        {
            var prompt = CreateUIElement("PlayerSwitchPrompt", parent, Vector2.zero, Vector2.zero);
            SetAnchors(prompt, Vector2.zero, Vector2.one, Vector2.zero);
            prompt.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

            // FullScreenBlocker
            var blocker = CreateUIElement("FullScreenBlocker", prompt.transform, Vector2.zero, Vector2.zero);
            SetAnchors(blocker, Vector2.zero, Vector2.one, Vector2.zero);
            blocker.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            var blockerImage = blocker.AddComponent<Image>();
            blockerImage.color = new Color(0, 0, 0, 0.9f);

            // PromptPanel
            var panel = CreateUIElement("PromptPanel", prompt.transform, Vector2.zero, new Vector2(500, 300));
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.25f, 0.2f, 0.15f, 1f);

            // MessageText
            var message = CreateTextElement("MessageText", panel.transform, new Vector2(0, 60), new Vector2(450, 80), "回合结束！\n请将设备交给对方玩家", 24);

            // PlayerIndicator
            var indicator = CreateTextElement("PlayerIndicator", panel.transform, new Vector2(0, 0), new Vector2(300, 40), "轮到：玩家 2", 28);
            indicator.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.8f, 0.2f, 1f);

            // ConfirmButton
            var confirmBtn = CreateButton("ConfirmButton", panel.transform, "准备好了", new Vector2(200, 60));
            confirmBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -80);

            prompt.SetActive(false);
        }

        private static void CreateGameOverPanel(Transform parent)
        {
            var panel = CreateUIElement("GameOverPanel", parent, Vector2.zero, Vector2.zero);
            SetAnchors(panel, Vector2.zero, Vector2.one, Vector2.zero);
            panel.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

            // Background
            var bg = CreateUIElement("Background", panel.transform, Vector2.zero, Vector2.zero);
            SetAnchors(bg, Vector2.zero, Vector2.one, Vector2.zero);
            bg.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.85f);

            // ContentPanel
            var content = CreateUIElement("ContentPanel", panel.transform, Vector2.zero, new Vector2(500, 350));
            var contentImage = content.AddComponent<Image>();
            contentImage.color = new Color(0.2f, 0.15f, 0.1f, 1f);

            // GameOverText
            var text = CreateTextElement("GameOverText", content.transform, new Vector2(0, 60), new Vector2(450, 150), "游戏结束！\n\n胜者：玩家 1", 24);

            // RestartButton
            var restartBtn = CreateButton("RestartButton", content.transform, "重新开始", new Vector2(160, 50));
            restartBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-90, -100);

            // QuitButton
            var quitBtn = CreateButton("QuitButton", content.transform, "退出", new Vector2(120, 50));
            quitBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(90, -100);

            panel.SetActive(false);
        }

        private static void CreateGameManager()
        {
            var manager = new GameObject("GameManager");
            manager.transform.SetParent(_canvas.transform, false);

            _hotSeatManager = manager.AddComponent<HotSeatGameManager>();
            _battleUIController = manager.AddComponent<BattleUIController>();

            // GameController会在HotSeatGameManager中动态创建
        }

        #endregion

        #region Connect References

        private static void ConnectReferences()
        {
            if (_battleUIController == null || _canvas == null) return;

            // 连接PlayerInfoPanels
            var topPanel = _canvas.transform.Find("TopPanel");
            var bottomPanel = _canvas.transform.Find("BottomPanel");
            _battleUIController.opponentInfoPanel = topPanel?.GetComponent<PlayerInfoPanel>();
            _battleUIController.myInfoPanel = bottomPanel?.GetComponent<PlayerInfoPanel>();

            // 连接HandAreas
            var opponentHand = _canvas.transform.Find("OpponentHandArea");
            var myHand = _canvas.transform.Find("MyHandArea");
            _battleUIController.opponentHandArea = opponentHand?.GetComponent<HandAreaController>();
            _battleUIController.myHandArea = myHand?.GetComponent<HandAreaController>();

            // 连接Tiles
            var battlefield = _canvas.transform.Find("BattlefieldArea/CenterBattlefield");
            if (battlefield != null)
            {
                var opponentField = battlefield.Find("OpponentField");
                var myField = battlefield.Find("MyField");

                if (opponentField != null)
                {
                    _battleUIController.opponentTiles = new TileSlotController[TILE_COUNT];
                    for (int i = 0; i < TILE_COUNT; i++)
                    {
                        var tile = opponentField.Find($"TileSlot_{i}");
                        _battleUIController.opponentTiles[i] = tile?.GetComponent<TileSlotController>();
                    }
                }

                if (myField != null)
                {
                    _battleUIController.myTiles = new TileSlotController[TILE_COUNT];
                    for (int i = 0; i < TILE_COUNT; i++)
                    {
                        var tile = myField.Find($"TileSlot_{i}");
                        _battleUIController.myTiles[i] = tile?.GetComponent<TileSlotController>();
                    }
                }
            }

            // 连接Deck/Graveyard
            var leftPanel = _canvas.transform.Find("BattlefieldArea/LeftSidePanel");
            var rightPanel = _canvas.transform.Find("BattlefieldArea/RightSidePanel");

            if (leftPanel != null)
            {
                _battleUIController.opponentDeckPile = leftPanel.Find("OpponentDeckPile")?.GetComponent<DeckPileDisplay>();
                _battleUIController.myDeckPile = leftPanel.Find("MyDeckPile")?.GetComponent<DeckPileDisplay>();
            }
            if (rightPanel != null)
            {
                _battleUIController.opponentGraveyard = rightPanel.Find("OpponentGraveyard")?.GetComponent<GraveyardDisplay>();
                _battleUIController.myGraveyard = rightPanel.Find("MyGraveyard")?.GetComponent<GraveyardDisplay>();
            }

            // 连接Buttons
            if (bottomPanel != null)
            {
                var endTurnBtn = bottomPanel.Find("EndTurnButton");
                var evolveBtn = bottomPanel.Find("EvolveButton");
                _battleUIController.endTurnButton = endTurnBtn?.GetComponent<Button>();
                _battleUIController.evolveButton = evolveBtn?.GetComponent<Button>();
                _battleUIController.endTurnButtonText = endTurnBtn?.GetComponentInChildren<TextMeshProUGUI>();
                _battleUIController.evolveButtonText = evolveBtn?.GetComponentInChildren<TextMeshProUGUI>();
            }

            // 连接Popups
            var popups = _canvas.transform.Find("Popups");
            if (popups != null)
            {
                _battleUIController.cardDetailPopup = popups.Find("CardDetailPopup")?.GetComponent<CardDetailPopup>();
                _battleUIController.cardListPopup = popups.Find("CardListPopup")?.GetComponent<CardListPopup>();
            }

            // 连接Turn Indicator (使用TopPanel中的某个元素或创建新的)
            var turnIndicator = CreateUIElement("TurnIndicator", _canvas.transform, new Vector2(0, 0), new Vector2(200, 50));
            var turnIndicatorImage = turnIndicator.AddComponent<Image>();
            turnIndicatorImage.color = new Color(0, 0.5f, 0, 0.8f);
            var turnText = CreateTextElement("TurnText", turnIndicator.transform, Vector2.zero, new Vector2(180, 40), "你的回合", 20);
            _battleUIController.myTurnIndicator = turnIndicator;
            _battleUIController.turnNumberText = CreateTextElement("TurnNumber", _canvas.transform, new Vector2(-850, 500), new Vector2(100, 30), "回合 1", 14).GetComponent<TextMeshProUGUI>();

            // 连接HotSeatManager
            if (_hotSeatManager != null)
            {
                _hotSeatManager.battleUI = _battleUIController;

                var switchPrompt = popups?.Find("PlayerSwitchPrompt");
                if (switchPrompt != null)
                {
                    _hotSeatManager.playerSwitchPrompt = switchPrompt.gameObject;
                    _hotSeatManager.switchMessageText = switchPrompt.Find("PromptPanel/MessageText")?.GetComponent<TextMeshProUGUI>();
                    _hotSeatManager.nextPlayerText = switchPrompt.Find("PromptPanel/PlayerIndicator")?.GetComponent<TextMeshProUGUI>();
                    _hotSeatManager.confirmSwitchButton = switchPrompt.Find("PromptPanel/ConfirmButton")?.GetComponent<Button>();
                }

                var gameOverPanel = popups?.Find("GameOverPanel");
                if (gameOverPanel != null)
                {
                    _hotSeatManager.gameOverPanel = gameOverPanel.gameObject;
                    _hotSeatManager.gameOverText = gameOverPanel.Find("ContentPanel/GameOverText")?.GetComponent<TextMeshProUGUI>();
                    _hotSeatManager.restartButton = gameOverPanel.Find("ContentPanel/RestartButton")?.GetComponent<Button>();
                    _hotSeatManager.quitButton = gameOverPanel.Find("ContentPanel/QuitButton")?.GetComponent<Button>();
                }
            }

            Debug.Log("BattleUIGenerator: 所有引用连接完成");
        }

        #endregion

        #region Save

        private static void SaveScene()
        {
            string scenePath = $"{SCENE_PATH}/Battle.unity";
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
            AssetDatabase.Refresh();
            Debug.Log($"BattleUIGenerator: 场景已保存到 {scenePath}");
        }

        private static void SavePrefab(GameObject obj, string name)
        {
            string path = $"{PREFAB_PATH}/{name}.prefab";

            // 如果已存在，先删除
            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
            }

            PrefabUtility.SaveAsPrefabAsset(obj, path);
            Debug.Log($"BattleUIGenerator: Prefab已保存到 {path}");
        }

        #endregion

        #region Helper Methods

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

            // 设置中文字体
            if (_chineseFont != null)
            {
                tmp.font = _chineseFont;
            }

            var layout = obj.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;

            return obj;
        }

        private static GameObject CreateButton(string name, Transform parent, string text, Vector2 size)
        {
            var obj = CreateUIElement(name, parent, Vector2.zero, size);

            var image = obj.AddComponent<Image>();
            image.color = BUTTON_COLOR;

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

        private static GameObject CreateIndicator(string name, Transform parent, Color color)
        {
            var obj = CreateUIElement(name, parent, new Vector2(60, 95), new Vector2(20, 20));
            var image = obj.AddComponent<Image>();
            image.color = color;
            obj.SetActive(false);
            return obj;
        }

        private static GameObject CreateGlowIndicator(string name, Transform parent, Color color, float width = -1, float height = -1)
        {
            if (width < 0) width = CARD_WIDTH + 10;
            if (height < 0) height = CARD_HEIGHT + 10;

            var obj = CreateUIElement(name, parent, Vector2.zero, new Vector2(width, height));
            var image = obj.AddComponent<Image>();
            image.color = color;
            obj.transform.SetAsFirstSibling();
            obj.SetActive(false);
            return obj;
        }

        private static void SetAnchors(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition)
        {
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
        }

        private static void MakeCircle(GameObject obj)
        {
            // 简单的圆形效果（实际项目中应使用圆形Sprite）
            // 这里只是占位
        }

        #endregion
    }
}
