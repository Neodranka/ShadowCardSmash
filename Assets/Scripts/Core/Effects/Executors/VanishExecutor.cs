using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 消失效果执行器
    /// 消失与破坏的区别：
    /// - 不进入墓地
    /// - 不触发谢幕效果
    /// </summary>
    public class VanishExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            foreach (var target in context.Targets)
            {
                if (target == null) continue;

                // 找到目标所在格子
                var tile = context.GameState.FindTileByInstanceId(target.instanceId);
                if (tile == null) continue;

                int tileIndex = tile.tileIndex;
                int ownerId = target.ownerId;
                int cardId = target.cardId;
                int instanceId = target.instanceId;

                // 移除单位（不加入墓地）
                tile.RemoveUnit();

                // 生成消失事件（wasVanished = true）
                context.AddEvent(new UnitDestroyedEvent(
                    context.SourcePlayerId,
                    instanceId,
                    cardId,
                    tileIndex,
                    ownerId,
                    true // 是消失，不是破坏
                ));

                // 消失不触发谢幕效果，因此不需要额外处理
            }
        }
    }
}
