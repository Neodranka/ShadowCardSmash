using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 召唤效果执行器
    /// </summary>
    public class SummonExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            // parameters[0] 存储要召唤的cardId
            if (context.Parameters == null || context.Parameters.Count == 0)
            {
                UnityEngine.Debug.LogWarning("SummonExecutor: No cardId specified in parameters");
                return;
            }

            if (!int.TryParse(context.Parameters[0], out int cardIdToSummon))
            {
                UnityEngine.Debug.LogWarning($"SummonExecutor: Invalid cardId in parameters: {context.Parameters[0]}");
                return;
            }

            // 召唤数量由Value决定，默认为1
            int summonCount = context.Value > 0 ? context.Value : 1;

            var player = context.GetSourcePlayer();

            for (int i = 0; i < summonCount; i++)
            {
                // 查找空格子
                int tileIndex = player.GetFirstEmptyTileIndex();
                if (tileIndex < 0)
                {
                    // 战场已满，无法召唤更多
                    UnityEngine.Debug.Log("SummonExecutor: Field is full, cannot summon more units");
                    break;
                }

                // 获取卡牌数据
                CardData cardData = null;
                if (context.CardDatabase != null)
                {
                    cardData = context.CardDatabase.GetCardById(cardIdToSummon);
                }

                if (cardData == null)
                {
                    UnityEngine.Debug.LogWarning($"SummonExecutor: CardData not found for cardId: {cardIdToSummon}");
                    continue;
                }

                // 创建运行时卡牌
                int instanceId = context.GenerateInstanceId?.Invoke() ?? 0;
                var runtimeCard = RuntimeCard.FromCardData(cardData, instanceId, context.SourcePlayerId);

                // 设置召唤失调（除非有疾驰或突进）
                // 疾驰可以攻击任何目标，突进只能攻击随从
                runtimeCard.canAttack = runtimeCard.hasStorm || runtimeCard.hasRush;

                // 放置到格子
                player.field[tileIndex].PlaceUnit(runtimeCard);

                context.AddEvent(new SummonEvent(
                    context.SourcePlayerId,
                    instanceId,
                    cardIdToSummon,
                    tileIndex,
                    context.SourcePlayerId
                ));
            }
        }
    }
}
