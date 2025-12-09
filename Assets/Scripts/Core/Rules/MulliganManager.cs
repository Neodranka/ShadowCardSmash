using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;
using ShadowCardSmash.Core.Effects;

namespace ShadowCardSmash.Core.Rules
{
    /// <summary>
    /// 换牌阶段管理器
    /// </summary>
    public class MulliganManager
    {
        private ICardDatabase _cardDatabase;
        private System.Func<int> _instanceIdGenerator;

        public MulliganManager(ICardDatabase cardDatabase, System.Func<int> instanceIdGenerator)
        {
            _cardDatabase = cardDatabase;
            _instanceIdGenerator = instanceIdGenerator;
        }

        /// <summary>
        /// 初始化换牌阶段
        /// </summary>
        public void InitializeMulligan(GameState state)
        {
            state.phase = GamePhase.Mulligan;
            state.mulliganState = new MulliganState();
        }

        /// <summary>
        /// 玩家选择/取消选择要换的牌
        /// </summary>
        public void ToggleCardSelection(GameState state, int playerId, int handIndex)
        {
            if (state.mulliganState == null) return;
            if (playerId < 0 || playerId > 1) return;

            var selected = state.mulliganState.selectedIndices[playerId];
            var player = state.players[playerId];

            // 验证索引有效性
            if (handIndex < 0 || handIndex >= player.hand.Count) return;

            if (selected.Contains(handIndex))
            {
                selected.Remove(handIndex);
            }
            else
            {
                selected.Add(handIndex);
            }
        }

        /// <summary>
        /// 玩家确认换牌选择
        /// </summary>
        public List<GameEvent> ConfirmMulligan(GameState state, int playerId)
        {
            var events = new List<GameEvent>();

            if (state.mulliganState == null) return events;
            if (playerId < 0 || playerId > 1) return events;
            if (state.mulliganState.playerReady[playerId]) return events; // 已确认过

            var player = state.players[playerId];
            var selectedIndices = new List<int>(state.mulliganState.selectedIndices[playerId]);

            // 按索引降序排列，从后往前移除避免索引错位
            selectedIndices.Sort((a, b) => b.CompareTo(a));

            // 记录要换掉的牌
            var cardsToReplace = new List<RuntimeCard>();
            foreach (int index in selectedIndices)
            {
                if (index >= 0 && index < player.hand.Count)
                {
                    cardsToReplace.Add(player.hand[index]);
                    player.hand.RemoveAt(index);
                }
            }

            // 把这些牌放回牌库
            foreach (var card in cardsToReplace)
            {
                player.deck.Add(card.cardId);
            }

            // 洗牌
            ShuffleDeck(player, state.randomSeed + playerId + state.turnNumber);

            // 抽同样数量的新牌
            int drawCount = cardsToReplace.Count;
            for (int i = 0; i < drawCount; i++)
            {
                if (player.deck.Count > 0)
                {
                    int cardId = player.deck[0];
                    player.deck.RemoveAt(0);

                    var cardData = _cardDatabase?.GetCardById(cardId);
                    RuntimeCard newCard;
                    if (cardData != null)
                    {
                        newCard = RuntimeCard.FromCardData(cardData, _instanceIdGenerator(), playerId);
                    }
                    else
                    {
                        newCard = new RuntimeCard
                        {
                            instanceId = _instanceIdGenerator(),
                            cardId = cardId,
                            ownerId = playerId
                        };
                    }
                    player.hand.Add(newCard);

                    events.Add(new CardDrawnEvent(playerId, cardId, newCard.instanceId, false, false));
                }
            }

            // 标记该玩家已完成换牌
            state.mulliganState.playerReady[playerId] = true;

            return events;
        }

        /// <summary>
        /// 检查是否所有玩家都完成换牌
        /// </summary>
        public bool IsMulliganComplete(GameState state)
        {
            if (state.mulliganState == null) return true;
            return state.mulliganState.AllPlayersReady;
        }

        /// <summary>
        /// 结束换牌阶段，开始正式游戏
        /// </summary>
        public List<GameEvent> EndMulliganPhase(GameState state)
        {
            var events = new List<GameEvent>();

            // 设置为先手玩家的回合开始
            state.currentPlayerId = 0;
            state.turnNumber = 1;
            state.phase = GamePhase.Main;

            // 先手玩家第一回合
            state.players[0].mana = 1;
            state.players[0].maxMana = 1;

            events.Add(new TurnStartEvent(0, 1, 1));

            // 先手玩家第一回合抽一张牌
            var player = state.players[0];
            if (player.deck.Count > 0)
            {
                int cardId = player.deck[0];
                player.deck.RemoveAt(0);

                var cardData = _cardDatabase?.GetCardById(cardId);
                RuntimeCard newCard;
                if (cardData != null)
                {
                    newCard = RuntimeCard.FromCardData(cardData, _instanceIdGenerator(), 0);
                }
                else
                {
                    newCard = new RuntimeCard
                    {
                        instanceId = _instanceIdGenerator(),
                        cardId = cardId,
                        ownerId = 0
                    };
                }
                player.hand.Add(newCard);
                events.Add(new CardDrawnEvent(0, cardId, newCard.instanceId, false, false));
            }

            return events;
        }

        /// <summary>
        /// 获取玩家选择的手牌索引
        /// </summary>
        public List<int> GetSelectedIndices(GameState state, int playerId)
        {
            if (state.mulliganState == null) return new List<int>();
            if (playerId < 0 || playerId > 1) return new List<int>();
            return new List<int>(state.mulliganState.selectedIndices[playerId]);
        }

        /// <summary>
        /// 检查玩家是否已完成换牌
        /// </summary>
        public bool IsPlayerReady(GameState state, int playerId)
        {
            if (state.mulliganState == null) return true;
            if (playerId < 0 || playerId > 1) return true;
            return state.mulliganState.playerReady[playerId];
        }

        private void ShuffleDeck(PlayerState player, int seed)
        {
            var rng = new System.Random(seed);
            int n = player.deck.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                int temp = player.deck[k];
                player.deck[k] = player.deck[n];
                player.deck[n] = temp;
            }
        }
    }
}
