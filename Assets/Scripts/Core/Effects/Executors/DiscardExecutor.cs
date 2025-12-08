using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 弃牌效果执行器
    /// </summary>
    public class DiscardExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            int discardCount = context.Value;
            var player = context.GetSourcePlayer();

            // 如果有指定的目标卡牌（从手牌中选择）
            if (context.Targets != null && context.Targets.Count > 0)
            {
                foreach (var target in context.Targets)
                {
                    if (target == null) continue;

                    // 从手牌中移除
                    for (int i = player.hand.Count - 1; i >= 0; i--)
                    {
                        if (player.hand[i].instanceId == target.instanceId)
                        {
                            var card = player.hand[i];
                            player.hand.RemoveAt(i);
                            player.graveyard.Add(card.cardId);

                            context.AddEvent(new DiscardEvent(
                                context.SourcePlayerId,
                                card.cardId,
                                card.instanceId
                            ));
                            break;
                        }
                    }
                }
            }
            else
            {
                // 随机弃牌
                var random = new System.Random();

                for (int i = 0; i < discardCount && player.hand.Count > 0; i++)
                {
                    int index = random.Next(player.hand.Count);
                    var card = player.hand[index];

                    player.hand.RemoveAt(index);
                    player.graveyard.Add(card.cardId);

                    context.AddEvent(new DiscardEvent(
                        context.SourcePlayerId,
                        card.cardId,
                        card.instanceId
                    ));
                }
            }
        }
    }
}
