using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 获得关键词效果执行器
    /// </summary>
    public class GainKeywordExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            // 从参数中获取关键词
            // parameters[0] = 关键词名称 (如 "Ward", "Rush", "Storm")
            if (context.Parameters == null || context.Parameters.Count == 0)
            {
                UnityEngine.Debug.LogWarning("GainKeywordExecutor: No keyword specified in parameters");
                return;
            }

            string keywordStr = context.Parameters[0];
            if (!System.Enum.TryParse<Keyword>(keywordStr, true, out var keyword))
            {
                UnityEngine.Debug.LogWarning($"GainKeywordExecutor: Invalid keyword: {keywordStr}");
                return;
            }

            foreach (var target in context.Targets)
            {
                if (target == null) continue;

                // 如果已被沉默，关键词无法获得
                if (target.isSilenced) continue;

                // 应用关键词
                switch (keyword)
                {
                    case Keyword.Ward:
                        if (!target.hasWard)
                        {
                            target.hasWard = true;
                            context.AddEvent(new KeywordGainedEvent(
                                context.SourcePlayerId,
                                target.instanceId,
                                Keyword.Ward
                            ));
                        }
                        break;

                    case Keyword.Rush:
                        if (!target.hasRush)
                        {
                            target.hasRush = true;
                            // 如果本回合入场且还没攻击过，获得突进后可以攻击随从
                            if (!target.attackedThisTurn)
                            {
                                target.canAttack = true;
                            }
                            context.AddEvent(new KeywordGainedEvent(
                                context.SourcePlayerId,
                                target.instanceId,
                                Keyword.Rush
                            ));
                        }
                        break;

                    case Keyword.Storm:
                        if (!target.hasStorm)
                        {
                            target.hasStorm = true;
                            target.hasRush = true; // 疾驰包含突进
                            // 如果本回合入场且还没攻击过，获得疾驰后可以攻击任意目标
                            if (!target.attackedThisTurn)
                            {
                                target.canAttack = true;
                            }
                            context.AddEvent(new KeywordGainedEvent(
                                context.SourcePlayerId,
                                target.instanceId,
                                Keyword.Storm
                            ));
                        }
                        break;
                }
            }
        }
    }
}
