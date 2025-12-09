using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects
{
    /// <summary>
    /// 效果执行上下文 - 包含效果执行所需的所有信息
    /// </summary>
    public class EffectContext
    {
        /// <summary>
        /// 当前游戏状态
        /// </summary>
        public GameState GameState { get; set; }

        /// <summary>
        /// 效果来源卡牌
        /// </summary>
        public RuntimeCard Source { get; set; }

        /// <summary>
        /// 效果来源玩家ID
        /// </summary>
        public int SourcePlayerId { get; set; }

        /// <summary>
        /// 目标列表
        /// </summary>
        public List<RuntimeCard> Targets { get; set; }

        /// <summary>
        /// 目标玩家ID（当目标是玩家时使用）
        /// </summary>
        public int TargetPlayerId { get; set; }

        /// <summary>
        /// 目标是否为玩家
        /// </summary>
        public bool TargetIsPlayer { get; set; }

        /// <summary>
        /// 效果数值
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// 次要数值（如Buff的生命值部分）
        /// </summary>
        public int SecondaryValue { get; set; }

        /// <summary>
        /// 条件表达式
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// 额外参数
        /// </summary>
        public List<string> Parameters { get; set; }

        /// <summary>
        /// 执行产生的事件列表
        /// </summary>
        public List<GameEvent> ResultEvents { get; set; }

        /// <summary>
        /// 卡牌数据库引用（用于查询CardData）
        /// </summary>
        public ICardDatabase CardDatabase { get; set; }

        /// <summary>
        /// 实例ID生成器
        /// </summary>
        public System.Func<int> GenerateInstanceId { get; set; }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public EffectContext()
        {
            Targets = new List<RuntimeCard>();
            Parameters = new List<string>();
            ResultEvents = new List<GameEvent>();
            TargetPlayerId = -1;
            TargetIsPlayer = false;
            SecondaryValue = 0;
            Condition = string.Empty;
        }

        /// <summary>
        /// 创建效果上下文
        /// </summary>
        public static EffectContext Create(GameState state, RuntimeCard source, int sourcePlayerId)
        {
            return new EffectContext
            {
                GameState = state,
                Source = source,
                SourcePlayerId = sourcePlayerId,
                Targets = new List<RuntimeCard>(),
                Parameters = new List<string>(),
                ResultEvents = new List<GameEvent>()
            };
        }

        /// <summary>
        /// 添加事件到结果列表
        /// </summary>
        public void AddEvent(GameEvent evt)
        {
            ResultEvents.Add(evt);
        }

        /// <summary>
        /// 获取对手玩家ID
        /// </summary>
        public int GetOpponentPlayerId()
        {
            return 1 - SourcePlayerId;
        }

        /// <summary>
        /// 获取来源玩家状态
        /// </summary>
        public PlayerState GetSourcePlayer()
        {
            return GameState.GetPlayer(SourcePlayerId);
        }

        /// <summary>
        /// 获取对手玩家状态
        /// </summary>
        public PlayerState GetOpponentPlayer()
        {
            return GameState.GetPlayer(GetOpponentPlayerId());
        }
    }

    /// <summary>
    /// 卡牌数据库接口 - 用于查询CardData
    /// </summary>
    public interface ICardDatabase
    {
        /// <summary>
        /// 根据ID获取卡牌数据
        /// </summary>
        CardData GetCardById(int cardId);

        /// <summary>
        /// 检查卡牌是否存在
        /// </summary>
        bool HasCard(int cardId);
    }
}
