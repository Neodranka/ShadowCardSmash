using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;
using ShadowCardSmash.Core.Effects;

namespace ShadowCardSmash.Core.Rules
{
    /// <summary>
    /// 进化系统 - 处理随从进化逻辑
    /// </summary>
    public class EvolutionSystem
    {
        private EffectSystem _effectSystem;
        private ICardDatabase _cardDatabase;

        /// <summary>
        /// 后手可以开始使用进化的回合（后手第4回合 = 游戏第2回合的后手）
        /// 实际上是后手的第4个行动回合，即游戏回合数>=4时后手可进化
        /// </summary>
        public const int EVOLUTION_AVAILABLE_TURN = 4;

        public EvolutionSystem(EffectSystem effectSystem, ICardDatabase cardDatabase)
        {
            _effectSystem = effectSystem;
            _cardDatabase = cardDatabase;
        }

        /// <summary>
        /// 检查玩家是否可以使用进化
        /// </summary>
        public bool CanUseEvolution(GameState state, int playerId)
        {
            var player = state.GetPlayer(playerId);

            // 检查EP
            if (player.evolutionPoints <= 0)
            {
                return false;
            }

            // 检查本回合是否已手动进化
            if (player.hasEvolvedThisTurn)
            {
                return false;
            }

            // 先手从第5回合开始可用，后手从第4回合开始可用
            int requiredTurn = (playerId == 0) ? 5 : EVOLUTION_AVAILABLE_TURN;
            if (state.turnNumber < requiredTurn)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查随从是否可以被进化
        /// </summary>
        public bool CanEvolveMinion(GameState state, RuntimeCard minion, int playerId)
        {
            if (minion == null) return false;

            // 必须是玩家自己的随从
            if (minion.ownerId != playerId) return false;

            // 必须是随从（不能是护符）
            var cardData = _cardDatabase?.GetCardById(minion.cardId);
            if (cardData == null || cardData.cardType != CardType.Minion)
            {
                return false;
            }

            // 未被进化过
            if (minion.isEvolved) return false;

            // 必须在场上
            var tile = state.FindTileByInstanceId(minion.instanceId);
            if (tile == null) return false;

            return true;
        }

        /// <summary>
        /// 执行进化
        /// </summary>
        /// <param name="state">游戏状态</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="minion">要进化的随从</param>
        /// <param name="consumeEP">是否消耗EP（手动进化消耗，效果进化不消耗）</param>
        /// <returns>产生的事件列表</returns>
        public List<GameEvent> Evolve(GameState state, int playerId, RuntimeCard minion, bool consumeEP)
        {
            var events = new List<GameEvent>();
            var player = state.GetPlayer(playerId);

            // 获取卡牌数据
            var cardData = _cardDatabase?.GetCardById(minion.cardId);
            if (cardData == null) return events;

            // 计算进化后的属性
            int evolvedAttack = cardData.evolvedAttack;
            int evolvedHealth = cardData.evolvedHealth;

            // 如果卡牌没有设置进化属性，使用默认 +2/+2
            if (evolvedAttack == 0 && evolvedHealth == 0)
            {
                evolvedAttack = cardData.attack + 2;
                evolvedHealth = cardData.health + 2;
            }

            // 应用进化
            int attackGain = evolvedAttack - cardData.attack;
            int healthGain = evolvedHealth - cardData.health;

            minion.currentAttack += attackGain;
            minion.currentHealth += healthGain;
            minion.maxHealth += healthGain;
            minion.isEvolved = true;

            // 进化获得突进（可以攻击随从）
            if (!minion.hasStorm)
            {
                minion.hasRush = true;
            }

            // 如果本回合入场，可以攻击了
            if (!minion.attackedThisTurn)
            {
                minion.canAttack = true;
            }

            // 消耗EP
            if (consumeEP)
            {
                player.evolutionPoints--;
                player.hasEvolvedThisTurn = true;
            }

            // 生成进化事件
            events.Add(new EvolveEvent(playerId, minion.instanceId, minion.cardId, consumeEP));

            // 触发进化效果
            if (!minion.isSilenced)
            {
                var evolveEvents = _effectSystem.TriggerEffects(state, EffectTrigger.OnEvolve, minion, playerId);
                events.AddRange(evolveEvents);
            }

            return events;
        }

        /// <summary>
        /// 获取可进化的随从列表
        /// </summary>
        public List<RuntimeCard> GetEvolvableMinions(GameState state, int playerId)
        {
            var result = new List<RuntimeCard>();

            if (!CanUseEvolution(state, playerId))
            {
                return result;
            }

            var player = state.GetPlayer(playerId);
            foreach (var tile in player.field)
            {
                if (tile.occupant != null && CanEvolveMinion(state, tile.occupant, playerId))
                {
                    result.Add(tile.occupant);
                }
            }

            return result;
        }
    }
}
