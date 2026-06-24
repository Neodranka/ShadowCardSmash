# ShadowCardSmash - 游戏设计文档

## 1. 游戏概述

**类型**：双人对战卡牌游戏（类似炉石传说/影之诗）  
**平台**：Unity 2022.3.62f3  
**联机方式**：IP直连 / 局域网  
**开发阶段**：可玩Demo（约45-60张卡牌）

---

## 2. 核心规则

### 2.1 基础数值

| 参数 | 值 |
|------|-----|
| 初始生命值 | 40 |
| 卡组大小 | 40张 |
| 同名卡上限 | 3张（所有稀有度统一） |
| 起始手牌 | 先手4张 / 后手5张+补偿卡 |
| 手牌上限 | 10张（超出时抽到的牌直接进入墓地） |
| 每回合抽牌 | 1张 |
| 费用上限 | 10（每回合+1，从1开始） |
| 战场格子 | 双方各6格（共12格） |

### 2.2 胜负条件

- **失败**：生命值归零
- **同归于尽**：当前回合的玩家判负
- **牌库耗尽**：抽牌时受到疲劳伤害（递增：1, 2, 3...）
- **特殊胜利**：部分卡牌可能有特殊胜利条件

### 2.3 费用系统

- 第1回合：1费上限
- 第2回合：2费上限
- ...
- 第10回合及以后：10费上限
- 每回合开始时费用恢复至上限

---

## 3. 卡牌系统

### 3.1 卡牌类型

| 类型 | 说明 |
|------|------|
| **随从** | 有攻击力/生命值，可进行战斗，占据格子 |
| **法术** | 一次性效果，使用后进入墓地 |
| **护符** | 放置在格子上，有持续效果或触发效果 |

### 3.2 卡牌稀有度

| 稀有度 | 标识 | 同名上限 |
|--------|------|----------|
| 彩（传说） | ★★★★ | 3张 |
| 金（史诗） | ★★★ | 3张 |
| 银（稀有） | ★★ | 3张 |
| 铜（普通） | ★ | 3张 |

### 3.3 卡牌归属

- **职业卡**：只能在对应职业的卡组中使用
- **中立卡**：所有职业都可以使用

### 3.4 职业（初期3个）

| 职业ID | 占位名 | 主题（待定） |
|--------|--------|-------------|
| 1 | 职业A | - |
| 2 | 职业B | - |
| 3 | 职业C | - |

### 3.5 标签系统（Tags）

每张卡牌可以拥有多个标签，用于：
- 效果筛选（如"对所有【龙】族随从+1/+1"）
- 羁绊/协同效果
- 统计和检索

```
示例标签：龙、士兵、机械、魔法、自然、亡灵...
```

### 3.6 白板随从身材参考

| 费用 | 身材总和 | 示例身材 |
|------|----------|----------|
| 1费 | 3 | 1/2, 2/1 |
| 2费 | 5 | 2/3, 3/2 |
| 3费 | 8 | 3/5, 4/4 |
| 4费 | 11 | 4/7, 5/6 |
| 5费 | 14 | 5/9, 6/8, 7/7 |
| 6费 | 17 | 6/11, 8/9 |
| 7费 | 20 | 7/13, 10/10 |
| 8费 | 23 | 8/15, 11/12 |
| 9费 | 27 | 9/18, 13/14 |
| 10费 | 30 | 10/20, 15/15 |

---

## 4. 战场系统

### 4.1 格子布局

```
对方场地:  [1] [2] [3] [4] [5] [6]
═══════════════════════════════════
我方场地:  [1] [2] [3] [4] [5] [6]
```

### 4.2 格子规则

- 每个格子最多放置**1个单位**（随从或护符）
- 使用随从/护符时**主动选择目标格子**
- 6格全满时**不能再放置新单位**
- 格子位置**影响部分效果**（如：对相邻单位造成伤害）

### 4.3 格子效果（场地效果）

格子本身可以被附加效果：

```
示例：
- "此格子上的随从在回合结束时受到5点伤害，持续2回合"
- "此格子上的随从获得+1攻击力"
```

格子效果数据结构需要支持：
- 效果类型
- 持续回合数
- 触发时机
- 效果来源

---

## 5. 战斗系统

### 5.1 攻击规则

- 攻击目标：可自由选择**任意敌方随从**或**敌方玩家**
- **守护例外**：场上有守护随从时，必须优先攻击守护随从
- 英雄**不能主动攻击**

### 5.2 召唤失调

- 随从入场当回合**默认不能攻击**
- **突进**：可立即攻击**随从**
- **疾驰**：可立即攻击**任意目标**

### 5.3 战斗结算

攻击方与防守方**同时造成伤害**（互相扣血）

---

## 6. 关键词/机制

### 6.1 随从关键词

| 关键词 | 效果 |
|--------|------|
| **守护** | 对方必须优先攻击拥有守护的随从（不影响法术/效果指向） |
| **突进** | 入场当回合可攻击敌方随从 |
| **疾驰** | 入场当回合可攻击任意目标 |
| **开幕** | 入场时触发效果（原战吼） |
| **谢幕** | 被**破坏**时触发效果（原亡语） |

### 6.2 护符机制

| 机制 | 说明 |
|------|------|
| **倒计时(X)** | 每回合开始时-1，归零时破坏自身（触发谢幕） |
| **启动** | 主动使用护符效果，可能消耗费用，可能破坏自身（写在卡牌文本中） |
| **开幕** | 护符入场时触发 |
| **谢幕** | 护符被破坏时触发 |

护符特性：
- 占据格子
- **不能被攻击**
- **可以被沉默**（谢幕失效、无法启动，但倒计时正常）

### 6.3 沉默

- 移除随从的**所有特殊能力**（守护、突进、疾驰、开幕已触发无影响、谢幕被移除）
- **不影响已获得的Buff**（+X/+X等数值变化保留）
- 对护符：谢幕失效、无法启动，**倒计时正常运作**

### 6.4 破坏与消失

| 机制 | 进入墓地 | 触发谢幕 |
|------|----------|----------|
| **破坏** | ✓ 是 | ✓ 是 |
| **消失** | ✗ 否（仅保留使用记录） | ✗ 否 |

---

## 7. 进化系统

### 7.1 进化点（EP）

| 参数 | 值 |
|------|-----|
| 初始EP | 3点（后手专属） |
| 可用时机 | 后手第4回合开始 |
| 每回合使用限制 | 手动进化最多1次 |

### 7.2 进化效果

- **+2/+2**属性提升
- 获得**突进**
- 卡牌**插画变更**

### 7.3 特殊进化

- 部分卡牌可通过**条件自动进化**（写在卡牌描述中）
- 部分卡牌可**使其他随从进化**
- 自动进化/效果进化**不消耗EP**，**不受每回合1次限制**

---

## 8. 后手补偿系统

### 8.1 补偿卡机制

- 后手玩家额外获得**1张补偿卡**
- 补偿卡在**卡组构建时选择**（从预设选项中选1张）
- 补偿卡**不占卡组40张**
- 补偿卡**费用为0**

### 8.2 补偿卡选项（示例）

| 名称 | 效果 |
|------|------|
| 临时水晶 | 本回合+1费用上限 |
| 微型打击 | 对一个随从造成2点伤害 |
| 小鬼召唤 | 召唤一个1/1的小鬼 |
| 贪婪抽取 | 丢弃1张手牌，抽2张牌 |
| ... | （可扩展更多选项） |

---

## 9. 区域定义

| 区域 | 说明 |
|------|------|
| **牌库** | 未抽取的卡牌，游戏开始时洗牌 |
| **手牌** | 玩家当前持有的牌，上限10张 |
| **战场** | 双方各6格，放置随从/护符 |
| **墓地** | 被破坏的单位、使用过的法术 |
| **消失区** | 被消失的卡牌（仅记录，不可交互） |

---

## 10. 回合流程

```
回合开始
├── 1. 回合开始阶段
│   ├── 费用上限+1（若未满10）
│   ├── 费用恢复至上限
│   ├── 护符倒计时-1（归零则破坏）
│   ├── 格子效果处理
│   └── 触发"回合开始"效果
│
├── 2. 抽牌阶段
│   ├── 抽1张牌
│   ├── 若牌库空，受到疲劳伤害
│   └── 若手牌满，抽到的牌进入墓地
│
├── 3. 主要阶段（玩家行动）
│   ├── 使用卡牌（消耗费用）
│   ├── 随从攻击
│   ├── 使用进化点（后手第4回合起）
│   ├── 启动护符效果
│   └── 可按任意顺序执行
│
└── 4. 回合结束阶段
    ├── 触发"回合结束"效果
    └── 切换到对方回合
```

---

## 11. 数据结构设计

### 11.1 卡牌数据 (CardData)

```csharp
[System.Serializable]
public class CardData
{
    // 基础信息
    public int cardId;
    public string cardName;
    public string description;
    public CardType cardType;        // Minion, Spell, Amulet
    public Rarity rarity;            // Bronze, Silver, Gold, Legendary
    public int cost;
    
    // 归属
    public HeroClass heroClass;      // Neutral, ClassA, ClassB, ClassC
    public List<string> tags;        // 标签列表
    
    // 随从属性
    public int attack;
    public int health;
    
    // 护符属性
    public int countdown;            // -1表示无倒计时
    public bool canActivate;         // 是否可启动
    public int activateCost;         // 启动费用
    
    // 进化属性
    public int evolvedAttack;        // 进化后攻击
    public int evolvedHealth;        // 进化后生命
    
    // 效果
    public List<EffectData> effects;
    
    // 资源引用
    public string artworkPath;
    public string evolvedArtworkPath;
}
```

### 11.2 效果数据 (EffectData)

```csharp
[System.Serializable]
public class EffectData
{
    public EffectTrigger trigger;    // 触发时机
    public EffectCondition condition; // 触发条件
    public TargetSelector target;    // 目标选择
    public EffectType effectType;    // 效果类型
    public int value;                // 效果数值
    public List<string> parameters;  // 额外参数
}

public enum EffectTrigger
{
    OnPlay,          // 开幕（入场时）
    OnDestroy,       // 谢幕（被破坏时）
    OnAttack,        // 攻击时
    OnDamaged,       // 受到伤害时
    OnTurnStart,     // 回合开始时
    OnTurnEnd,       // 回合结束时
    OnEvolve,        // 进化时
    OnActivate,      // 启动时（护符）
    OnAllyPlay,      // 友方单位入场时
    OnEnemyPlay,     // 敌方单位入场时
    OnAllyDestroy,   // 友方单位被破坏时
    OnEnemyDestroy,  // 敌方单位被破坏时
    // ...可扩展
}

public enum EffectType
{
    Damage,          // 造成伤害
    Heal,            // 恢复生命
    Draw,            // 抽牌
    Discard,         // 弃牌
    Summon,          // 召唤
    Buff,            // 增益（+X/+X）
    Debuff,          // 减益（-X/-X）
    Destroy,         // 破坏
    Vanish,          // 消失
    Silence,         // 沉默
    GainKeyword,     // 获得关键词
    AddToHand,       // 将卡牌加入手牌
    Transform,       // 变形
    Evolve,          // 进化
    GainCost,        // 获得费用
    TileEffect,      // 格子效果
    // ...可扩展
}
```

### 11.3 游戏状态 (GameState)

```csharp
[System.Serializable]
public class GameState
{
    public int turnNumber;
    public int currentPlayerId;
    public GamePhase phase;
    public PlayerState[] players;    // [0]=先手, [1]=后手
    public int randomSeed;
}

[System.Serializable]
public class PlayerState
{
    public int playerId;
    public HeroClass heroClass;
    
    // 生命与费用
    public int health;
    public int maxHealth;
    public int mana;
    public int maxMana;
    
    // 进化点
    public int evolutionPoints;
    public bool hasEvolvedThisTurn;
    
    // 疲劳
    public int fatigueCounter;
    
    // 卡牌区域
    public List<int> deck;           // 牌库（cardId列表）
    public List<RuntimeCard> hand;   // 手牌
    public TileState[] field;        // 战场（6格）
    public List<int> graveyard;      // 墓地
    
    // 后手补偿卡
    public int compensationCardId;   // -1表示无/先手
}

[System.Serializable]
public class TileState
{
    public int tileIndex;            // 0-5
    public RuntimeCard occupant;     // 占据的单位（null表示空）
    public List<TileEffect> effects; // 格子效果
}

[System.Serializable]
public class RuntimeCard
{
    public int instanceId;           // 运行时唯一ID
    public int cardId;               // 对应CardData
    
    // 当前状态
    public int currentAttack;
    public int currentHealth;
    public int maxHealth;
    public bool isEvolved;
    public bool canAttack;
    public bool isSilenced;
    public int currentCountdown;     // 护符倒计时
    
    // 关键词状态
    public bool hasWard;
    public bool hasRush;
    public bool hasStorm;
    
    // Buff列表（用于显示和结算）
    public List<BuffData> buffs;
}
```

### 11.4 卡组数据 (DeckData)

```csharp
[System.Serializable]
public class DeckData
{
    public string deckId;
    public string deckName;
    public HeroClass heroClass;
    public List<DeckEntry> cards;    // 40张
    public int compensationCardId;   // 后手补偿卡选择
    public long lastModified;
}

[System.Serializable]
public class DeckEntry
{
    public int cardId;
    public int count;                // 1-3
}
```

---

## 12. 枚举定义

```csharp
public enum CardType
{
    Minion,      // 随从
    Spell,       // 法术
    Amulet       // 护符
}

public enum Rarity
{
    Bronze,      // 铜
    Silver,      // 银
    Gold,        // 金
    Legendary    // 彩
}

public enum HeroClass
{
    Neutral = 0, // 中立
    ClassA = 1,  // 职业A
    ClassB = 2,  // 职业B
    ClassC = 3   // 职业C
}

public enum GamePhase
{
    NotStarted,
    Mulligan,        // 换牌阶段（如果有）
    TurnStart,
    Draw,
    Main,
    TurnEnd,
    GameOver
}

public enum Keyword
{
    Ward,        // 守护
    Rush,        // 突进
    Storm,       // 疾驰
    // ...可扩展
}
```

---

## 13. 网络同步设计

### 13.1 架构模式

- **Host-Client模式**：一方作为Host（服务器+客户端），另一方作为Client
- **确定性锁步**：双方使用相同随机种子，执行相同逻辑
- **Host权威**：所有操作由Host验证和执行

### 13.2 同步消息类型

```csharp
public enum NetworkMessageType
{
    // 连接阶段
    Connect,
    Disconnect,
    DeckSubmit,          // 提交卡组
    DeckAccepted,        // 卡组验证通过
    DeckRejected,        // 卡组验证失败
    Ready,
    
    // 游戏阶段
    GameStart,           // 游戏开始（含随机种子）
    PlayerAction,        // 玩家操作
    ActionResult,        // 操作结果
    StateSync,           // 状态同步
    GameEvent,           // 游戏事件（用于动画）
    
    // 其他
    Ping,
    Pong,
    Surrender            // 投降
}
```

### 13.3 玩家操作类型

```csharp
public enum ActionType
{
    PlayCard,            // 使用卡牌
    Attack,              // 攻击
    Evolve,              // 进化
    ActivateAmulet,      // 启动护符
    EndTurn,             // 结束回合
    Surrender            // 投降
}
```

---

## 14. 开发阶段规划

### Phase 1: 核心数据与效果系统
- [ ] CardData / EffectData 数据结构
- [ ] ScriptableObject 配置系统
- [ ] 效果系统框架（触发器、条件、目标选择、效果执行）
- [ ] 基础效果实现（伤害、治疗、抽牌、召唤等）

### Phase 2: 游戏规则引擎
- [ ] GameState 状态管理
- [ ] 回合流程控制
- [ ] 战场格子系统
- [ ] 战斗结算逻辑
- [ ] 进化系统
- [ ] 关键词系统

### Phase 3: 卡组系统
- [ ] DeckData 数据结构
- [ ] 卡组验证器
- [ ] 本地存储服务
- [ ] 后手补偿卡系统

### Phase 4: 网络层
- [ ] NetworkManager 框架
- [ ] Host/Client 连接
- [ ] 消息序列化
- [ ] 状态同步
- [ ] 断线重连（可选）

### Phase 5: UI与视觉
- [ ] 主菜单
- [ ] 卡组构建界面
- [ ] 联机大厅
- [ ] 战斗界面
- [ ] 卡牌动画

### Phase 6: 卡牌制作
- [ ] 中立卡 10-15张
- [ ] 职业A卡 10-15张
- [ ] 职业B卡 10-15张
- [ ] 职业C卡 10-15张
- [ ] 补偿卡 4-5张

---

## 15. 项目目录结构

```
Assets/
├── Scripts/
│   ├── Core/                    # 核心逻辑（纯C#）
│   │   ├── Data/               # 数据结构
│   │   │   ├── CardData.cs
│   │   │   ├── EffectData.cs
│   │   │   ├── DeckData.cs
│   │   │   ├── GameState.cs
│   │   │   └── Enums.cs
│   │   ├── Effects/            # 效果系统
│   │   │   ├── EffectSystem.cs
│   │   │   ├── Triggers/
│   │   │   ├── Conditions/
│   │   │   ├── TargetSelectors/
│   │   │   └── EffectExecutors/
│   │   └── Rules/              # 游戏规则
│   │       ├── GameRuleEngine.cs
│   │       ├── TurnManager.cs
│   │       ├── CombatResolver.cs
│   │       ├── EvolutionSystem.cs
│   │       └── DeckValidator.cs
│   │
│   ├── Network/                 # 网络模块
│   │   ├── NetworkManager.cs
│   │   ├── NetworkServer.cs
│   │   ├── NetworkClient.cs
│   │   └── Messages/
│   │
│   ├── Managers/               # 管理器
│   │   ├── GameManager.cs
│   │   ├── DeckManager.cs
│   │   ├── CollectionManager.cs
│   │   └── StorageService.cs
│   │
│   ├── View/                    # 视图层
│   │   ├── CardView.cs
│   │   ├── BoardView.cs
│   │   ├── TileView.cs
│   │   └── AnimationController.cs
│   │
│   └── UI/                      # UI
│       ├── MainMenuUI.cs
│       ├── DeckBuilderUI.cs
│       ├── LobbyUI.cs
│       └── BattleUI.cs
│
├── Data/                        # 配置数据
│   ├── Cards/                  # 卡牌ScriptableObject
│   ├── Effects/                # 效果配置
│   └── Rules/                  # 规则配置
│
├── Prefabs/
│   ├── Cards/
│   ├── UI/
│   └── Effects/
│
├── Art/
│   ├── Cards/                  # 卡牌插画
│   ├── UI/
│   └── VFX/
│
└── Scenes/
    ├── MainMenu.unity
    ├── DeckBuilder.unity
    ├── Lobby.unity
    └── Battle.unity
```

---

## 16. 待确定事项

- [ ] 三个职业的具体主题和名称
- [ ] 是否需要换牌阶段（Mulligan）
- [ ] 具体卡牌设计
- [ ] 美术风格

---

*文档版本: 1.0*  
*最后更新: 2025.12.9*
