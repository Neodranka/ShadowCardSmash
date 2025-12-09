using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 获得屏障效果执行器 - 用于给玩家或随从添加屏障
    /// </summary>
    public class GainBarrierExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            // 如果目标是玩家
            if (context.TargetIsPlayer)
            {
                var player = context.GameState.GetPlayer(context.TargetPlayerId);
                if (player != null && !player.hasBarrier)
                {
                    player.hasBarrier = true;
                    context.AddEvent(new BarrierGainedEvent(
                        context.SourcePlayerId,
                        context.TargetPlayerId,
                        true
                    ));
                    UnityEngine.Debug.Log($"GainBarrierExecutor: 玩家{context.TargetPlayerId}获得了屏障");
                }
                return;
            }

            // 如果目标是随从
            foreach (var target in context.Targets)
            {
                if (target == null || target.isSilenced) continue;

                if (!target.hasBarrier)
                {
                    target.hasBarrier = true;
                    context.AddEvent(new BarrierGainedEvent(
                        context.SourcePlayerId,
                        target.instanceId,
                        false
                    ));
                    UnityEngine.Debug.Log($"GainBarrierExecutor: 随从{target.instanceId}获得了屏障");
                }
            }
        }
    }
}
