using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 破坏效果执行器
    /// </summary>
    public class DestroyExecutor : IEffectExecutor
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

                // 移除单位
                tile.RemoveUnit();

                // 加入墓地
                var owner = context.GameState.GetPlayer(ownerId);
                owner.graveyard.Add(cardId);

                // 生成破坏事件
                context.AddEvent(new UnitDestroyedEvent(
                    context.SourcePlayerId,
                    instanceId,
                    cardId,
                    tileIndex,
                    ownerId,
                    false // 不是消失，是破坏
                ));

                // 注意：谢幕效果应该在外部处理（效果系统会监听UnitDestroyedEvent）
                // 这里只负责破坏单位本身
            }
        }
    }
}
