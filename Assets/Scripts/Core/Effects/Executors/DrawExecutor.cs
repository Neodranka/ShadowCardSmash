using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 抽牌效果执行器
    /// </summary>
    public class DrawExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            int drawCount = context.Value;
            var player = context.GetSourcePlayer();

            for (int i = 0; i < drawCount; i++)
            {
                // 检查牌库是否为空
                if (player.IsDeckEmpty())
                {
                    // 疲劳伤害
                    player.fatigueCounter++;
                    int fatigueDamage = player.fatigueCounter;
                    player.TakeDamage(fatigueDamage);

                    context.AddEvent(new FatigueEvent(
                        context.SourcePlayerId,
                        fatigueDamage,
                        player.fatigueCounter
                    ));

                    // 检查玩家是否死亡
                    if (player.IsDead())
                    {
                        context.GameState.phase = GamePhase.GameOver;
                        context.AddEvent(new GameOverEvent(
                            1 - context.SourcePlayerId,
                            $"Player {context.SourcePlayerId} died from fatigue"
                        ));
                        return;
                    }

                    continue;
                }

                // 从牌库抽取一张牌
                int cardId = player.deck[0];
                player.deck.RemoveAt(0);

                // 检查手牌是否已满
                if (player.IsHandFull())
                {
                    // 直接进入墓地
                    player.graveyard.Add(cardId);

                    context.AddEvent(new CardDrawnEvent(
                        context.SourcePlayerId,
                        cardId,
                        -1,
                        false,
                        true // 因手牌满而丢弃
                    ));
                }
                else
                {
                    // 创建运行时卡牌并加入手牌
                    int instanceId = context.GenerateInstanceId?.Invoke() ?? 0;

                    CardData cardData = null;
                    if (context.CardDatabase != null)
                    {
                        cardData = context.CardDatabase.GetCardById(cardId);
                    }

                    RuntimeCard runtimeCard;
                    if (cardData != null)
                    {
                        runtimeCard = RuntimeCard.FromCardData(cardData, instanceId, context.SourcePlayerId);
                    }
                    else
                    {
                        // 如果没有卡牌数据库，创建一个基本的运行时卡牌
                        runtimeCard = new RuntimeCard
                        {
                            instanceId = instanceId,
                            cardId = cardId,
                            ownerId = context.SourcePlayerId
                        };
                    }

                    player.hand.Add(runtimeCard);

                    context.AddEvent(new CardDrawnEvent(
                        context.SourcePlayerId,
                        cardId,
                        instanceId,
                        false,
                        false
                    ));

                    // 触发抽牌效果（公会总管等）- 只在不是 single_trigger 模式下每张牌触发
                    // 如果是 single_trigger，会在循环结束后统一触发一次
                    bool isSingleTrigger = context.Parameters?.Contains("single_trigger") ?? false;
                    if (!isSingleTrigger && context.EffectSystem != null)
                    {
                        var drawTriggerEvents = context.EffectSystem.TriggerEffects(
                            context.GameState, EffectTrigger.OnDraw, null, context.SourcePlayerId);
                        foreach (var evt in drawTriggerEvents)
                        {
                            context.AddEvent(evt);
                        }
                    }
                }
            }

            // 如果是 single_trigger 模式（如"抽2张牌"），只触发一次OnDraw
            bool wasSingleTrigger = context.Parameters?.Contains("single_trigger") ?? false;
            if (wasSingleTrigger && context.EffectSystem != null)
            {
                var drawTriggerEvents = context.EffectSystem.TriggerEffects(
                    context.GameState, EffectTrigger.OnDraw, null, context.SourcePlayerId);
                foreach (var evt in drawTriggerEvents)
                {
                    context.AddEvent(evt);
                }
            }
        }
    }
}
