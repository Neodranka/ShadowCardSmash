# Phase 7: 战斗界面 UI 开发指令

## 概述

本阶段目标：创建完整的战斗界面 UI，支持本地热座模式测试。

**设计风格**：奇幻风格（边框装饰、羊皮纸质感）
**目标分辨率**：1920 x 1080

---

## 界面布局设计

```
┌─────────────────────────────────────────────────────────────────────────┐
│  [对手头像] [生命:40] [费用:3/10]        [手牌数:5] [EP:●●●]           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│              对手手牌区（显示卡背，最多10张，居中排列）                   │
│                                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│        │                                                       │        │
│ [对手] │   对手战场：[格子1] [格子2] [格子3] [格子4] [格子5] [格子6]  │ [对手] │
│ [牌库] │                                                       │ [墓地] │
│  23张  │                                                       │  5张  │
├────────┼───────────────────────────────────────────────────────┼────────┤
│ [我方] │   我方战场：[格子1] [格子2] [格子3] [格子4] [格子5] [格子6]  │ [我方] │
│ [牌库] │                                                       │ [墓地] │
│  18张  │                                                       │  8张  │
├────────┴───────────────────────────────────────────────────────┴────────┤
│                                                                         │
│              我方手牌区（显示卡牌正面，最多10张，居中排列）               │
│                                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│  [我方头像] [生命:40] [费用:5/10]        [手牌数:7]    [结束回合按钮]    │
│                                          [进化按钮 EP:●●○]              │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 指令 7.1 - 创建基础 UI 框架

```
请创建战斗界面的基础 UI 框架：

1. 创建场景 Assets/Scenes/Battle.unity

2. 设置 Canvas：
   - Canvas Scaler: Scale With Screen Size
   - Reference Resolution: 1920 x 1080
   - Match: 0.5 (宽高平衡)

3. 创建 UI 层级结构：

BattleCanvas
├── Background (全屏背景图)
├── TopPanel (对手信息栏)
│   ├── OpponentPortrait (头像)
│   ├── OpponentHealthText (生命值)
│   ├── OpponentManaText (费用)
│   ├── OpponentHandCount (手牌数)
│   └── OpponentEPDisplay (进化点显示)
│
├── OpponentHandArea (对手手牌区域)
│   └── OpponentHandContainer (手牌容器，Horizontal Layout)
│
├── BattlefieldArea (战场区域)
│   ├── LeftSidePanel (左侧面板)
│   │   ├── OpponentDeckPile (对手牌库)
│   │   └── MyDeckPile (我方牌库)
│   │
│   ├── CenterBattlefield (中央战场)
│   │   ├── OpponentField (对手6格子)
│   │   │   └── TileSlot x 6
│   │   └── MyField (我方6格子)
│   │       └── TileSlot x 6
│   │
│   └── RightSidePanel (右侧面板)
│       ├── OpponentGraveyard (对手墓地)
│       └── MyGraveyard (我方墓地)
│
├── MyHandArea (我方手牌区域)
│   └── MyHandContainer (手牌容器，Horizontal Layout)
│
├── BottomPanel (我方信息栏)
│   ├── MyPortrait (头像)
│   ├── MyHealthText (生命值)
│   ├── MyManaText (费用)
│   ├── MyHandCount (手牌数)
│   ├── EvolveButton (进化按钮)
│   └── EndTurnButton (结束回合按钮)
│
├── Popups (弹窗层)
│   ├── CardDetailPopup (卡牌详情弹窗，默认隐藏)
│   ├── DeckListPopup (牌库列表弹窗，默认隐藏)
│   └── GraveyardPopup (墓地列表弹窗，默认隐藏)
│
└── OverlayEffects (特效层)
    └── TurnIndicator (回合指示器)

4. 为所有面板添加 Image 组件作为背景占位（使用纯色，后期替换）：
   - 信息栏背景：深棕色 #3D2817
   - 手牌区背景：半透明黑 #00000080
   - 战场背景：深绿色 #1A2F1A
   - 格子背景：浅色边框 #8B7355

5. 创建 Assets/Scripts/UI/Battle/ 目录
```

---

## 指令 7.2 - 创建卡牌 Prefab

```
创建卡牌的 UI Prefab：

1. 创建 Assets/Prefabs/UI/CardView.prefab：

结构：
CardView (160 x 220 像素)
├── CardFrame (Image - 卡牌边框，稀有度决定颜色)
│   ├── Bronze: #CD7F32
│   ├── Silver: #C0C0C0
│   ├── Gold: #FFD700
│   └── Legendary: #FF69B4 (彩虹渐变或粉紫色)
│
├── CardArt (Image - 卡牌插画，占据大部分空间)
│   └── 位置：上方留出名称区域，下方留出攻/血区域
│
├── CardNameBG (Image - 名称背景条)
│   └── CardNameText (TextMeshPro - 卡牌名称)
│       └── 字体大小：14-16，居中
│
├── CostGem (Image - 左上角费用宝石背景)
│   └── CostText (TextMeshPro - 费用数字)
│       └── 字体大小：20，加粗，白色
│
├── AttackIcon (Image - 左下角攻击力背景，剑图标)
│   └── AttackText (TextMeshPro - 攻击力数字)
│
├── HealthIcon (Image - 右下角生命值背景，心图标)
│   └── HealthText (TextMeshPro - 生命值数字)
│
├── EvolvedIndicator (进化标记，默认隐藏)
│
├── SummoningSickness (召唤失调标记，默认隐藏)
│   └── 半透明灰色遮罩 + Zzz 图标
│
├── CanAttackGlow (可攻击时的发光边框，默认隐藏)
│
└── SelectionHighlight (选中高亮，默认隐藏)

2. 创建 Assets/Prefabs/UI/CardBack.prefab：
   - 卡背设计（统一图案）
   - 尺寸与 CardView 相同

3. 创建 Assets/Prefabs/UI/MiniCard.prefab：
   - 用于牌库/墓地列表显示
   - 尺寸：120 x 165（缩小版）

4. 创建 Assets/Scripts/UI/Battle/CardViewController.cs：

public class CardViewController : MonoBehaviour
{
    [Header("UI References")]
    public Image cardFrame;
    public Image cardArt;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI healthText;
    
    [Header("State Indicators")]
    public GameObject evolvedIndicator;
    public GameObject summoningSicknessIndicator;
    public GameObject canAttackGlow;
    public GameObject selectionHighlight;
    
    [Header("Frame Colors")]
    public Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);
    public Color silverColor = new Color(0.75f, 0.75f, 0.75f);
    public Color goldColor = new Color(1f, 0.84f, 0f);
    public Color legendaryColor = new Color(1f, 0.41f, 0.71f);
    
    private CardData cardData;
    private RuntimeCard runtimeCard;
    
    // 设置卡牌数据（用于手牌/收藏显示）
    public void SetCardData(CardData data);
    
    // 设置运行时卡牌数据（用于战场显示）
    public void SetRuntimeCard(RuntimeCard runtime, CardData baseData);
    
    // 更新显示
    public void RefreshDisplay();
    
    // 设置选中状态
    public void SetSelected(bool selected);
    
    // 设置可攻击状态
    public void SetCanAttack(bool canAttack);
    
    // 播放进化动画
    public void PlayEvolveAnimation();
}
```

---

## 指令 7.3 - 创建战场格子 Prefab

```
创建战场格子的 UI Prefab：

1. 创建 Assets/Prefabs/UI/TileSlot.prefab：

结构 (180 x 250 像素)：
TileSlot
├── TileBackground (Image - 格子背景)
│   └── 默认：半透明边框
│   └── 可放置时：发光提示
│
├── OccupantHolder (放置卡牌/护符的容器)
│   └── 子物体为 CardView 实例
│
├── TileEffectIndicator (格子效果指示器)
│   └── 显示该格子上的特殊效果图标
│
├── ValidTargetHighlight (有效目标高亮，默认隐藏)
│   └── 绿色边框发光
│
└── InvalidTargetIndicator (无效目标标记，默认隐藏)
    └── 红色 X 标记

2. 创建 Assets/Scripts/UI/Battle/TileSlotController.cs：

public class TileSlotController : MonoBehaviour
{
    [Header("UI References")]
    public Image tileBackground;
    public Transform occupantHolder;
    public GameObject tileEffectIndicator;
    public GameObject validTargetHighlight;
    
    [Header("Settings")]
    public int tileIndex;
    public bool isOpponentTile;
    
    private CardViewController currentOccupant;
    
    // 放置单位
    public void PlaceUnit(CardViewController cardView);
    
    // 移除单位
    public void RemoveUnit();
    
    // 获取当前单位
    public CardViewController GetOccupant();
    
    // 设置为有效放置目标
    public void SetValidPlacementTarget(bool valid);
    
    // 设置为有效攻击目标
    public void SetValidAttackTarget(bool valid);
    
    // 显示格子效果
    public void ShowTileEffect(TileEffect effect);
    
    // 点击事件
    public event Action<TileSlotController> OnTileClicked;
}
```

---

## 指令 7.4 - 创建信息面板

```
创建玩家信息面板：

1. 创建 Assets/Scripts/UI/Battle/PlayerInfoPanel.cs：

public class PlayerInfoPanel : MonoBehaviour
{
    [Header("UI References")]
    public Image portrait;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI manaText;
    public TextMeshProUGUI handCountText;
    public Transform epContainer; // 放置EP图标
    
    [Header("EP Icons")]
    public GameObject epIconPrefab;
    public Color epAvailableColor = Color.yellow;
    public Color epUsedColor = Color.gray;
    
    private List<Image> epIcons = new List<Image>();
    
    // 更新显示
    public void UpdateDisplay(PlayerState playerState);
    
    // 更新生命值（带动画）
    public void UpdateHealth(int current, int max, bool animate = true);
    
    // 更新费用
    public void UpdateMana(int current, int max);
    
    // 更新手牌数
    public void UpdateHandCount(int count);
    
    // 更新进化点
    public void UpdateEvolutionPoints(int available, int total);
    
    // 受伤动画
    public void PlayDamageAnimation(int amount);
    
    // 治疗动画
    public void PlayHealAnimation(int amount);
}

2. 创建 Assets/Scripts/UI/Battle/DeckPileDisplay.cs：

public class DeckPileDisplay : MonoBehaviour
{
    public TextMeshProUGUI countText;
    public Image deckImage;
    public Button clickArea;
    
    private List<int> deckContents; // 牌库内容（用于显示列表）
    
    public void UpdateCount(int count);
    public void SetDeckContents(List<int> cardIds);
    
    // 点击显示牌库列表
    public event Action OnDeckClicked;
}

3. 创建 Assets/Scripts/UI/Battle/GraveyardDisplay.cs：

public class GraveyardDisplay : MonoBehaviour
{
    public TextMeshProUGUI countText;
    public Image graveyardImage;
    public Button clickArea;
    
    private List<int> graveyardContents;
    
    public void UpdateCount(int count);
    public void SetGraveyardContents(List<int> cardIds);
    public void AddCard(int cardId); // 添加时可能播放动画
    
    // 点击显示墓地列表
    public event Action OnGraveyardClicked;
}
```

---

## 指令 7.5 - 创建弹窗系统

```
创建弹窗 UI：

1. 创建 Assets/Prefabs/UI/CardDetailPopup.prefab：

结构：
CardDetailPopup
├── DarkOverlay (半透明黑色背景，点击关闭)
├── PopupPanel (主面板，居中)
│   ├── CardDisplay (大尺寸卡牌显示 240 x 330)
│   │   ├── CardFrame
│   │   ├── CardArt
│   │   ├── CostText
│   │   ├── CardNameText
│   │   ├── AttackText (如果是随从)
│   │   └── HealthText (如果是随从)
│   │
│   ├── DescriptionPanel (效果描述区域)
│   │   ├── DescriptionBG (羊皮纸风格背景)
│   │   └── DescriptionText (TextMeshPro，支持富文本)
│   │
│   ├── TagsDisplay (标签显示)
│   │   └── "龙 | 士兵 | 机械"
│   │
│   └── CloseButton (关闭按钮)

2. 创建 Assets/Scripts/UI/Battle/CardDetailPopup.cs：

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
    public GameObject attackHealthGroup; // 随从才显示
    public Button closeButton;
    public Button overlayButton;
    
    public void Show(CardData cardData);
    public void Show(RuntimeCard runtimeCard, CardData baseData);
    public void Hide();
    
    // 格式化描述文本（处理关键词高亮）
    private string FormatDescription(string description);
}

3. 创建 Assets/Prefabs/UI/CardListPopup.prefab：

用于显示牌库/墓地列表：
CardListPopup
├── DarkOverlay
├── PopupPanel
│   ├── TitleText ("牌库" 或 "墓地")
│   ├── ScrollView
│   │   └── Content (Grid Layout)
│   │       └── MiniCard x N
│   ├── CardCountText ("共 23 张")
│   └── CloseButton

4. 创建 Assets/Scripts/UI/Battle/CardListPopup.cs：

public class CardListPopup : MonoBehaviour
{
    public GameObject popupRoot;
    public TextMeshProUGUI titleText;
    public Transform contentContainer;
    public TextMeshProUGUI cardCountText;
    public GameObject miniCardPrefab;
    
    public void ShowDeck(List<int> cardIds, Dictionary<int, CardData> cardDatabase);
    public void ShowGraveyard(List<int> cardIds, Dictionary<int, CardData> cardDatabase);
    public void Hide();
    
    // 点击列表中的卡牌显示详情
    public event Action<int> OnCardClicked;
}
```

---

## 指令 7.6 - 创建手牌区域管理

```
创建手牌区域控制器：

1. 创建 Assets/Scripts/UI/Battle/HandAreaController.cs：

public class HandAreaController : MonoBehaviour
{
    [Header("Settings")]
    public bool isOpponentHand;
    public Transform handContainer;
    public GameObject cardPrefab;
    public GameObject cardBackPrefab;
    
    [Header("Layout Settings")]
    public float cardSpacing = 10f;
    public float maxHandWidth = 1200f;
    public float hoverRaiseAmount = 30f;
    public float selectedScale = 1.2f;
    
    private List<CardViewController> handCards = new List<CardViewController>();
    private CardViewController hoveredCard;
    private CardViewController selectedCard;
    
    // 设置手牌（对手只显示卡背）
    public void SetHand(List<RuntimeCard> cards, Dictionary<int, CardData> cardDatabase);
    
    // 添加一张牌（带动画）
    public void AddCard(RuntimeCard card, CardData data);
    
    // 移除一张牌（带动画）
    public void RemoveCard(int instanceId);
    
    // 更新手牌布局（当数量变化时重新排列）
    public void RefreshLayout();
    
    // 高亮可使用的卡牌
    public void HighlightPlayableCards(List<int> playableIndices);
    
    // 清除所有高亮
    public void ClearHighlights();
    
    // 获取选中的卡牌
    public CardViewController GetSelectedCard();
    
    // 事件
    public event Action<int> OnCardClicked;      // 参数：手牌索引
    public event Action<int> OnCardHovered;
    public event Action OnCardUnhovered;
    public event Action<int> OnCardRightClicked; // 显示详情
}

2. 实现手牌扇形排列（可选）：

如果手牌数量多，可以做成扇形：
- 5张以下：水平排列
- 6-10张：轻微扇形，中间牌稍高
```

---

## 指令 7.7 - 创建战斗界面主控制器

```
创建战斗界面主控制器：

1. 创建 Assets/Scripts/UI/Battle/BattleUIController.cs：

public class BattleUIController : MonoBehaviour
{
    [Header("Player Info Panels")]
    public PlayerInfoPanel myInfoPanel;
    public PlayerInfoPanel opponentInfoPanel;
    
    [Header("Hand Areas")]
    public HandAreaController myHandArea;
    public HandAreaController opponentHandArea;
    
    [Header("Battlefield")]
    public TileSlotController[] myTiles;      // 6个
    public TileSlotController[] opponentTiles; // 6个
    
    [Header("Deck & Graveyard")]
    public DeckPileDisplay myDeckPile;
    public DeckPileDisplay opponentDeckPile;
    public GraveyardDisplay myGraveyard;
    public GraveyardDisplay opponentGraveyard;
    
    [Header("Buttons")]
    public Button endTurnButton;
    public Button evolveButton;
    
    [Header("Popups")]
    public CardDetailPopup cardDetailPopup;
    public CardListPopup cardListPopup;
    
    [Header("Turn Indicator")]
    public GameObject myTurnIndicator;
    public TextMeshProUGUI turnNumberText;
    
    [Header("References")]
    public GameController gameController; // Phase 6 创建的
    
    // 当前交互状态
    private BattleUIState currentState = BattleUIState.Idle;
    private CardViewController selectedCard;
    private int selectedHandIndex = -1;
    private TileSlotController selectedTile;
    
    // ===== 初始化 =====
    public void Initialize(GameState initialState, int localPlayerId);
    
    // ===== 刷新显示 =====
    public void RefreshAllUI();
    public void RefreshPlayerInfo(int playerId);
    public void RefreshHand(int playerId);
    public void RefreshBattlefield();
    
    // ===== 回合管理 =====
    public void OnTurnStart(int playerId);
    public void OnTurnEnd(int playerId);
    public void SetMyTurn(bool isMyTurn);
    
    // ===== 交互处理 =====
    private void OnHandCardClicked(int handIndex);
    private void OnTileClicked(TileSlotController tile);
    private void OnEndTurnClicked();
    private void OnEvolveClicked();
    
    // ===== 目标选择 =====
    public void EnterTargetSelectionMode(List<int> validTargetInstanceIds);
    public void ExitTargetSelectionMode();
    
    // ===== 卡牌使用流程 =====
    // 1. 点击手牌 -> 高亮可放置格子
    // 2. 点击格子 -> 如果需要目标，进入目标选择
    // 3. 选择目标 -> 执行使用卡牌
    
    // ===== 攻击流程 =====
    // 1. 点击我方随从 -> 高亮可攻击目标
    // 2. 点击目标 -> 执行攻击
    
    // ===== 事件动画 =====
    public void PlayGameEvent(GameEvent gameEvent);
    public void PlayDamageAnimation(int targetInstanceId, int amount);
    public void PlayHealAnimation(int targetInstanceId, int amount);
    public void PlaySummonAnimation(int tileIndex, bool isOpponent);
    public void PlayDeathAnimation(int instanceId);
    public void PlayDrawAnimation(int playerId);
    public void PlayAttackAnimation(int attackerInstanceId, int targetInstanceId);
}

public enum BattleUIState
{
    Idle,               // 等待输入
    CardSelected,       // 已选中手牌
    SelectingTile,      // 选择放置格子
    SelectingTarget,    // 选择效果目标
    SelectingAttacker,  // 选择攻击者
    SelectingAttackTarget, // 选择攻击目标
    WaitingForOpponent, // 等待对手
    Animating           // 播放动画中
}
```

---

## 指令 7.8 - 创建本地热座测试模式

```
创建本地双人测试模式：

1. 创建 Assets/Scripts/Tests/HotSeatGameManager.cs：

public class HotSeatGameManager : MonoBehaviour
{
    [Header("References")]
    public BattleUIController battleUI;
    public GameController gameController;
    
    [Header("Test Settings")]
    public bool useTestDeck = true;
    
    private GameState gameState;
    private int currentViewingPlayer = 0; // 当前显示哪个玩家的视角
    
    void Start()
    {
        InitializeTestGame();
    }
    
    void InitializeTestGame()
    {
        // 1. 创建测试卡牌数据库
        var cardDatabase = CreateTestCardDatabase();
        
        // 2. 创建初始游戏状态
        gameState = CreateInitialGameState();
        
        // 3. 初始化游戏控制器
        gameController.Initialize(gameState);
        
        // 4. 初始化UI
        battleUI.Initialize(gameState, currentViewingPlayer);
        
        // 5. 开始第一回合
        StartFirstTurn();
    }
    
    Dictionary<int, CardData> CreateTestCardDatabase()
    {
        // 创建 Phase 3 中定义的测试卡牌
        return TestCardDatabase.GetAllCards();
    }
    
    GameState CreateInitialGameState()
    {
        // 创建双方玩家状态
        // 洗牌、发起始手牌等
    }
    
    void StartFirstTurn()
    {
        // 决定先后手
        // 开始先手玩家的回合
    }
    
    // 切换视角（热座模式，回合结束时调用）
    public void SwitchPlayerView()
    {
        currentViewingPlayer = 1 - currentViewingPlayer;
        battleUI.Initialize(gameState, currentViewingPlayer);
        
        // 可以加一个 "请将设备交给对方玩家" 的提示
    }
    
    // 回合结束回调
    public void OnTurnEnded(int playerId)
    {
        // 显示切换提示
        // 等待确认后切换视角
        ShowPlayerSwitchPrompt();
    }
    
    void ShowPlayerSwitchPrompt()
    {
        // 显示 "回合结束，请将设备交给 玩家X" 的提示
        // 点击确认后调用 SwitchPlayerView()
    }
}

2. 创建切换玩家提示 UI Assets/Prefabs/UI/PlayerSwitchPrompt.prefab：

PlayerSwitchPrompt
├── FullScreenBlocker (全屏遮挡)
├── PromptPanel
│   ├── MessageText ("回合结束！请将设备交给对方玩家")
│   ├── PlayerIndicator ("轮到：玩家2")
│   └── ConfirmButton ("准备好了")

3. 更新测试场景 Assets/Scenes/Battle.unity：
   - 添加 HotSeatGameManager
   - 连接所有引用
```

---

## 指令 7.9 - 创建测试卡牌数据库

```
更新测试卡牌数据库，确保有足够的卡牌进行测试：

1. 更新 Assets/Scripts/Tests/TestCardDatabase.cs：

public static class TestCardDatabase
{
    private static Dictionary<int, CardData> cards;
    
    public static Dictionary<int, CardData> GetAllCards()
    {
        if (cards == null)
        {
            cards = new Dictionary<int, CardData>();
            CreateAllTestCards();
        }
        return cards;
    }
    
    public static CardData GetCard(int cardId)
    {
        GetAllCards();
        return cards.TryGetValue(cardId, out var card) ? card : null;
    }
    
    private static void CreateAllTestCards()
    {
        // ===== 中立随从 =====
        AddCard(new CardData {
            cardId = 1001,
            cardName = "新兵",
            description = "",
            cardType = CardType.Minion,
            rarity = Rarity.Bronze,
            cost = 1,
            heroClass = HeroClass.Neutral,
            attack = 1,
            health = 2,
            evolvedAttack = 3,
            evolvedHealth = 4,
            tags = new List<string> { "士兵" }
        });
        
        AddCard(new CardData {
            cardId = 1002,
            cardName = "护卫兵",
            description = "守护",
            cardType = CardType.Minion,
            rarity = Rarity.Bronze,
            cost = 2,
            heroClass = HeroClass.Neutral,
            attack = 1,
            health = 4,
            evolvedAttack = 3,
            evolvedHealth = 6,
            tags = new List<string> { "士兵" },
            // 效果：守护
        });
        
        AddCard(new CardData {
            cardId = 1003,
            cardName = "精锐剑士",
            description = "",
            cardType = CardType.Minion,
            rarity = Rarity.Silver,
            cost = 3,
            heroClass = HeroClass.Neutral,
            attack = 3,
            health = 4,
            evolvedAttack = 5,
            evolvedHealth = 6,
            tags = new List<string> { "士兵" }
        });
        
        AddCard(new CardData {
            cardId = 1004,
            cardName = "突击骑兵",
            description = "突进",
            cardType = CardType.Minion,
            rarity = Rarity.Silver,
            cost = 4,
            heroClass = HeroClass.Neutral,
            attack = 4,
            health = 3,
            evolvedAttack = 6,
            evolvedHealth = 5,
            tags = new List<string> { "士兵" }
        });
        
        AddCard(new CardData {
            cardId = 1005,
            cardName = "治疗师",
            description = "开幕：恢复友方玩家3点生命",
            cardType = CardType.Minion,
            rarity = Rarity.Silver,
            cost = 2,
            heroClass = HeroClass.Neutral,
            attack = 1,
            health = 3,
            evolvedAttack = 3,
            evolvedHealth = 5,
            tags = new List<string> { "治疗" }
        });
        
        AddCard(new CardData {
            cardId = 1006,
            cardName = "狂战士",
            description = "疾驰",
            cardType = CardType.Minion,
            rarity = Rarity.Gold,
            cost = 5,
            heroClass = HeroClass.Neutral,
            attack = 5,
            health = 4,
            evolvedAttack = 7,
            evolvedHealth = 6,
            tags = new List<string> { "战士" }
        });
        
        // ===== 中立法术 =====
        AddCard(new CardData {
            cardId = 2001,
            cardName = "火球术",
            description = "对一个敌方随从造成3点伤害",
            cardType = CardType.Spell,
            rarity = Rarity.Bronze,
            cost = 2,
            heroClass = HeroClass.Neutral
        });
        
        AddCard(new CardData {
            cardId = 2002,
            cardName = "治愈之光",
            description = "恢复一个友方角色4点生命",
            cardType = CardType.Spell,
            rarity = Rarity.Bronze,
            cost = 2,
            heroClass = HeroClass.Neutral
        });
        
        AddCard(new CardData {
            cardId = 2003,
            cardName = "毁灭",
            description = "破坏一个随从",
            cardType = CardType.Spell,
            rarity = Rarity.Gold,
            cost = 5,
            heroClass = HeroClass.Neutral
        });
        
        AddCard(new CardData {
            cardId = 2004,
            cardName = "知识渴求",
            description = "抽2张牌",
            cardType = CardType.Spell,
            rarity = Rarity.Silver,
            cost = 3,
            heroClass = HeroClass.Neutral
        });
        
        // ===== 中立护符 =====
        AddCard(new CardData {
            cardId = 3001,
            cardName = "神圣祭坛",
            description = "倒计时3。谢幕：抽2张牌",
            cardType = CardType.Amulet,
            rarity = Rarity.Silver,
            cost = 2,
            heroClass = HeroClass.Neutral,
            countdown = 3,
            canActivate = false
        });
        
        AddCard(new CardData {
            cardId = 3002,
            cardName = "能量水晶",
            description = "启动：获得1点费用，破坏此护符",
            cardType = CardType.Amulet,
            rarity = Rarity.Bronze,
            cost = 0,
            heroClass = HeroClass.Neutral,
            countdown = -1,
            canActivate = true,
            activateCost = 0
        });
        
        // ===== 后手补偿卡 =====
        AddCard(new CardData {
            cardId = 10001,
            cardName = "临时水晶",
            description = "本回合+1费用",
            cardType = CardType.Spell,
            rarity = Rarity.Bronze,
            cost = 0,
            heroClass = HeroClass.Neutral
        });
        
        AddCard(new CardData {
            cardId = 10002,
            cardName = "微型打击",
            description = "对一个随从造成2点伤害",
            cardType = CardType.Spell,
            rarity = Rarity.Bronze,
            cost = 0,
            heroClass = HeroClass.Neutral
        });
        
        AddCard(new CardData {
            cardId = 10003,
            cardName = "小鬼召唤",
            description = "召唤一个1/1的小鬼",
            cardType = CardType.Spell,
            rarity = Rarity.Bronze,
            cost = 0,
            heroClass = HeroClass.Neutral
        });
    }
    
    private static void AddCard(CardData card)
    {
        cards[card.cardId] = card;
    }
    
    // 创建测试卡组（各20张，填满40张）
    public static List<int> CreateTestDeck()
    {
        var deck = new List<int>();
        
        // 每种卡3张
        for (int i = 0; i < 3; i++)
        {
            deck.Add(1001); // 新兵
            deck.Add(1002); // 护卫兵
            deck.Add(1003); // 精锐剑士
            deck.Add(1004); // 突击骑兵
            deck.Add(1005); // 治疗师
            deck.Add(2001); // 火球术
            deck.Add(2002); // 治愈之光
            deck.Add(2004); // 知识渴求
            deck.Add(3001); // 神圣祭坛
        }
        
        // 补充到40张
        deck.Add(1006); // 狂战士
        deck.Add(1006);
        deck.Add(2003); // 毁灭
        deck.Add(2003);
        deck.Add(3002); // 能量水晶
        deck.Add(3002);
        deck.Add(3002);
        
        // 洗牌
        ShuffleDeck(deck);
        
        return deck;
    }
    
    private static void ShuffleDeck(List<int> deck)
    {
        var rng = new System.Random();
        int n = deck.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            int temp = deck[k];
            deck[k] = deck[n];
            deck[n] = temp;
        }
    }
}
```

---

## 测试验证

完成以上所有指令后，在 Unity 中：

1. 打开 Battle 场景
2. 点击 Play
3. 应该能看到：
   - 双方的生命值、费用显示
   - 手牌显示（先手4张，后手5张）
   - 空的战场格子
   - 可以点击手牌查看详情
   - 可以使用卡牌放置到格子
   - 可以结束回合
   - 回合结束后提示切换玩家

如果某些功能还没完全实现，用 Debug.Log 标注进度。
```

---

*UI 开发指令版本: 1.0*
