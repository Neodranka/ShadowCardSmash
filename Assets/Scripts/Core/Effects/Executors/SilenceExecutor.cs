using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 沉默效果执行器
    /// 沉默效果：
    /// - 移除所有特殊能力（守护、突进、疾驰）
    /// - 移除谢幕效果
    /// - 保留已获得的Buff数值
    /// - 对护符：谢幕失效、无法启动，但倒计时正常
    /// </summary>
    public class SilenceExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            foreach (var target in context.Targets)
            {
                if (target == null) continue;

                // 如果已经被沉默，跳过
                if (target.isSilenced) continue;

                // 应用沉默
                target.Silence();

                // 生成沉默事件
                context.AddEvent(new SilenceEvent(
                    context.SourcePlayerId,
                    target.instanceId
                ));
            }
        }
    }
}
