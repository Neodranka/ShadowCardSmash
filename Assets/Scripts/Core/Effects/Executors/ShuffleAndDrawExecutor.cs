using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 洗入牌库并抽牌执行器 - 用于军需官等卡牌
    /// 将选中的手牌洗入牌库，然后抽1张牌
    /// </summary>
    public class ShuffleAndDrawExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            var player = context.GetSourcePlayer();

            // 需要有选中的目标（手牌）
            if (context.Targets == null || context.Targets.Count == 0)
            {
                UnityEngine.Debug.LogWarning("ShuffleAndDrawExecutor: 没有选中的手牌");
                return;
            }

            var selectedCard = context.Targets[0];

            // 从手牌中移除
            int handIndex = player.hand.FindIndex(c => c.instanceId == selectedCard.instanceId);
            if (handIndex < 0)
            {
                UnityEngine.Debug.LogWarning($"ShuffleAndDrawExecutor: 找不到手牌 instanceId={selectedCard.instanceId}");
                return;
            }

            player.hand.RemoveAt(handIndex);

            // 洗入牌库（随机位置）
            if (player.deck.Count > 0)
            {
                int randomIndex = new System.Random().Next(player.deck.Count + 1);
                player.deck.Insert(randomIndex, selectedCard.cardId);
            }
            else
            {
                player.deck.Add(selectedCard.cardId);
            }

            UnityEngine.Debug.Log($"ShuffleAndDrawExecutor: 将 {selectedCard.cardId} 洗入牌库");

            // 抽1张牌
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

                if (player.IsDead())
                {
                    context.GameState.phase = GamePhase.GameOver;
                    context.AddEvent(new GameOverEvent(
                        1 - context.SourcePlayerId,
                        $"Player {context.SourcePlayerId} died from fatigue"
                    ));
                }
                return;
            }

            // 从牌库抽取一张牌
            int drawnCardId = player.deck[0];
            player.deck.RemoveAt(0);

            if (player.IsHandFull())
            {
                // 直接进入墓地
                player.graveyard.Add(drawnCardId);

                context.AddEvent(new CardDrawnEvent(
                    context.SourcePlayerId,
                    drawnCardId,
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
                    cardData = context.CardDatabase.GetCardById(drawnCardId);
                }

                RuntimeCard runtimeCard;
                if (cardData != null)
                {
                    runtimeCard = RuntimeCard.FromCardData(cardData, instanceId, context.SourcePlayerId);
                }
                else
                {
                    runtimeCard = new RuntimeCard
                    {
                        instanceId = instanceId,
                        cardId = drawnCardId,
                        ownerId = context.SourcePlayerId
                    };
                }

                player.hand.Add(runtimeCard);

                context.AddEvent(new CardDrawnEvent(
                    context.SourcePlayerId,
                    drawnCardId,
                    instanceId,
                    false,
                    false
                ));

                // 触发抽牌效果（公会总管等）
                if (context.EffectSystem != null)
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
}
