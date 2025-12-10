namespace ShadowCardSmash.Core.Data
{
    /// <summary>
    /// 卡牌类型
    /// </summary>
    public enum CardType
    {
        Minion,     // 随从
        Spell,      // 法术
        Amulet      // 护符
    }

    /// <summary>
    /// 卡牌稀有度
    /// </summary>
    public enum Rarity
    {
        Bronze,     // 铜（普通）
        Silver,     // 银（稀有）
        Gold,       // 金（史诗）
        Legendary,  // 彩（传说）
        Token       // 衍生卡
    }

    /// <summary>
    /// 职业类型
    /// </summary>
    public enum HeroClass
    {
        Neutral = 0,    // 中立
        Vampire = 1,    // 吸血鬼
        ClassA = 2,     // 职业A
        ClassB = 3,     // 职业B
        ClassC = 4      // 职业C
    }

    /// <summary>
    /// 游戏阶段
    /// </summary>
    public enum GamePhase
    {
        NotStarted,     // 未开始
        Mulligan,       // 换牌阶段
        TurnStart,      // 回合开始
        Draw,           // 抽牌阶段
        Main,           // 主要阶段
        TurnEnd,        // 回合结束
        GameOver        // 游戏结束
    }

    /// <summary>
    /// 关键词
    /// </summary>
    public enum Keyword
    {
        None,       // 无
        Ward,       // 守护
        Rush,       // 突进
        Storm,      // 疾驰
        Barrier,    // 屏障（免疫一次伤害）
        Drain       // 吸血（攻击时回复等量生命）
    }

    /// <summary>
    /// 效果触发时机
    /// </summary>
    public enum EffectTrigger
    {
        OnPlay,             // 开幕（入场时）
        OnDestroy,          // 谢幕（被破坏时）
        OnAttack,           // 攻击时
        OnDamaged,          // 受到伤害时
        OnTurnStart,        // 回合开始时
        OnTurnEnd,          // 回合结束时
        OnEvolve,           // 进化时
        OnActivate,         // 启动时（护符）
        OnAllyPlay,         // 友方单位入场时
        OnEnemyPlay,        // 敌方单位入场时
        OnAllyDestroy,      // 友方单位被破坏时
        OnEnemyDestroy,     // 敌方单位被破坏时
        OnOwnerTurnEnd,     // 自己回合结束时（吸血鬼用）
        OnMinionDestroyed,  // 任意随从被破坏时
        OnOwnerDamaged,     // 自己玩家受伤时
        OnDraw              // 抽牌时触发
    }

    /// <summary>
    /// 效果类型
    /// </summary>
    public enum EffectType
    {
        Damage,         // 造成伤害
        Heal,           // 恢复生命
        Draw,           // 抽牌
        Discard,        // 弃牌
        Summon,         // 召唤
        Buff,           // 增益（+X/+X）
        Debuff,         // 减益（-X/-X）
        Destroy,        // 破坏
        Vanish,         // 消失
        Silence,        // 沉默
        GainKeyword,    // 获得关键词
        AddToHand,      // 将卡牌加入手牌
        Transform,      // 变形
        Evolve,         // 进化
        GainCost,       // 获得费用
        TileEffect,     // 格子效果
        GainBarrier,    // 获得屏障
        SwapHealth,     // 交换生命值
        EqualizeHealth, // 均衡生命值
        RandomDamage,   // 随机伤害
        DamageByCount,  // 根据数量造成伤害
        HealByDamage,   // 根据伤害量回复
        SelfDamage,     // 对自己玩家造成伤害
        Reanimate,      // 复活墓地随从
        GainStats,      // 获得属性（根据条件）
        DiscardToGain,  // 弃牌获得效果
        AddEffect,      // 给目标添加效果
        KurentiSpecial, // 克伦缇特殊效果
        DestroyAllOther,// 破坏所有其他随从
        SelfEvolve,     // 自我进化
        AddCardToHand,  // 添加卡牌到手牌
        ApplyTileEffect,// 给地格施加效果
        ShuffleAndDraw  // 洗入牌库并抽牌
    }

    /// <summary>
    /// 目标类型
    /// </summary>
    public enum TargetType
    {
        Self,               // 自身
        SingleEnemy,        // 单个敌方随从（不含玩家）
        SingleEnemyOrPlayer,// 单个敌方随从或敌方玩家
        SingleAlly,         // 单个友方
        AllEnemies,         // 所有敌人
        AllAllies,          // 所有友方
        AllMinions,         // 所有随从
        RandomEnemy,        // 随机敌人
        RandomAlly,         // 随机友方
        PlayerChoice,       // 玩家选择
        EnemyPlayer,        // 敌方玩家
        AllyPlayer,         // 友方玩家
        AdjacentTiles,      // 相邻格子
        All,                // 所有随从+双方玩家
        SingleMinion,       // 单个随从（双方都可选）
        EnemyTiles,         // 敌方地格（用于倾盆大雨）
        HandCard            // 手牌（用于军需官）
    }

    /// <summary>
    /// 地格效果类型
    /// </summary>
    public enum TileEffectType
    {
        None,           // 无效果
        DownpourRain    // 倾盆大雨：回合结束时随机-1/-0或-0/-1
    }

    /// <summary>
    /// 玩家操作类型
    /// </summary>
    public enum ActionType
    {
        PlayCard,           // 使用卡牌
        Attack,             // 攻击
        Evolve,             // 进化
        ActivateAmulet,     // 启动护符
        EndTurn,            // 结束回合
        Surrender           // 投降
    }

    /// <summary>
    /// 网络消息类型
    /// </summary>
    public enum NetworkMessageType
    {
        // 连接阶段
        Connect,
        Disconnect,
        DeckSubmit,         // 提交卡组
        DeckAccepted,       // 卡组验证通过
        DeckRejected,       // 卡组验证失败
        Ready,

        // 游戏阶段
        GameStart,          // 游戏开始（含随机种子）
        PlayerAction,       // 玩家操作
        ActionResult,       // 操作结果
        StateSync,          // 状态同步
        GameEvent,          // 游戏事件（用于动画）

        // 其他
        Ping,
        Pong,
        Surrender           // 投降
    }
}
