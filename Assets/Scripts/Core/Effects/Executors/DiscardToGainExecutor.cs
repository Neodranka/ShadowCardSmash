using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 弃牌获得效果执行器 - 选择手牌弃置以获得增益
    /// </summary>
    public class DiscardToGainExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            var player = context.GetSourcePlayer();
            if (player == null) return;

            // 解析参数
            string filter = null;
            int buffAttack = 0;
            int buffHealth = 0;

            foreach (var param in context.Parameters)
            {
                if (param.StartsWith("filter:"))
                {
                    filter = param.Substring(7); // "minion", "spell", etc.
                }
                else if (param.StartsWith("buff:"))
                {
                    var buffParts = param.Substring(5).Split(',');
                    if (buffParts.Length >= 2)
                    {
                        int.TryParse(buffParts[0], out buffAttack);
                        int.TryParse(buffParts[1], out buffHealth);
                    }
                }
            }

            // 找到可弃置的手牌
            var validCards = new List<RuntimeCard>();
            foreach (var card in player.hand)
            {
                // 跳过自己（如果是随从卡）
                if (context.Source != null && card.instanceId == context.Source.instanceId)
                    continue;

                if (string.IsNullOrEmpty(filter))
                {
                    validCards.Add(card);
                }
                else
                {
                    var cardData = context.CardDatabase?.GetCardById(card.cardId);
                    if (cardData != null)
                    {
                        bool matchFilter = false;
                        switch (filter)
                        {
                            case "minion":
                                matchFilter = cardData.cardType == CardType.Minion;
                                break;
                            case "spell":
                                matchFilter = cardData.cardType == CardType.Spell;
                                break;
                            case "amulet":
                                matchFilter = cardData.cardType == CardType.Amulet;
                                break;
                            default:
                                matchFilter = true;
                                break;
                        }
                        if (matchFilter)
                        {
                            validCards.Add(card);
                        }
                    }
                }
            }

            if (validCards.Count == 0)
            {
                UnityEngine.Debug.Log("DiscardToGainExecutor: 没有可弃置的卡牌");
                return;
            }

            // 简化实现：自动弃置第一张符合条件的牌
            // TODO: 将来需要实现玩家选择弃牌的 UI 交互
            var cardToDiscard = validCards[0];
            int cardIndex = player.hand.IndexOf(cardToDiscard);

            if (cardIndex >= 0)
            {
                // 从手牌移除
                player.hand.RemoveAt(cardIndex);
                // 加入墓地
                player.graveyard.Add(cardToDiscard.cardId);

                context.AddEvent(new DiscardEvent(
                    context.SourcePlayerId,
                    cardToDiscard.cardId,
                    cardToDiscard.instanceId
                ));

                UnityEngine.Debug.Log($"DiscardToGainExecutor: 弃置卡牌 {cardToDiscard.cardId}");

                // 给来源随从增益
                if (context.Source != null && (buffAttack != 0 || buffHealth != 0))
                {
                    context.Source.currentAttack += buffAttack;
                    context.Source.currentHealth += buffHealth;
                    context.Source.maxHealth += buffHealth;

                    context.AddEvent(new BuffEvent(
                        context.SourcePlayerId,
                        context.Source.instanceId,
                        buffAttack,
                        buffHealth,
                        null
                    ));

                    UnityEngine.Debug.Log($"DiscardToGainExecutor: 来源随从获得 +{buffAttack}/+{buffHealth}");
                }
            }
        }
    }
}
