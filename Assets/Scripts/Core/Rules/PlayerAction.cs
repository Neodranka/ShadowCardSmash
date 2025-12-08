using System;
using ShadowCardSmash.Core.Data;

namespace ShadowCardSmash.Core.Rules
{
    /// <summary>
    /// 玩家操作 - 封装玩家的一次游戏操作
    /// </summary>
    [Serializable]
    public class PlayerAction
    {
        /// <summary>
        /// 执行操作的玩家ID
        /// </summary>
        public int playerId;

        /// <summary>
        /// 操作类型
        /// </summary>
        public ActionType actionType;

        /// <summary>
        /// 手牌索引（PlayCard时使用）
        /// </summary>
        public int handIndex;

        /// <summary>
        /// 目标格子索引（放置单位时使用）
        /// </summary>
        public int tileIndex;

        /// <summary>
        /// 来源单位实例ID（Attack/Evolve/Activate时使用）
        /// </summary>
        public int sourceInstanceId;

        /// <summary>
        /// 目标单位实例ID
        /// </summary>
        public int targetInstanceId;

        /// <summary>
        /// 目标是否为玩家
        /// </summary>
        public bool targetIsPlayer;

        /// <summary>
        /// 目标玩家ID（当targetIsPlayer为true时使用）
        /// </summary>
        public int targetPlayerId;

        public PlayerAction()
        {
            handIndex = -1;
            tileIndex = -1;
            sourceInstanceId = -1;
            targetInstanceId = -1;
            targetPlayerId = -1;
        }

        /// <summary>
        /// 创建使用卡牌操作
        /// </summary>
        public static PlayerAction CreatePlayCard(int playerId, int handIndex, int tileIndex,
            int targetInstanceId = -1, bool targetIsPlayer = false, int targetPlayerId = -1)
        {
            return new PlayerAction
            {
                playerId = playerId,
                actionType = ActionType.PlayCard,
                handIndex = handIndex,
                tileIndex = tileIndex,
                targetInstanceId = targetInstanceId,
                targetIsPlayer = targetIsPlayer,
                targetPlayerId = targetPlayerId
            };
        }

        /// <summary>
        /// 创建攻击操作
        /// </summary>
        public static PlayerAction CreateAttack(int playerId, int attackerInstanceId,
            int targetInstanceId = -1, bool targetIsPlayer = false, int targetPlayerId = -1)
        {
            return new PlayerAction
            {
                playerId = playerId,
                actionType = ActionType.Attack,
                sourceInstanceId = attackerInstanceId,
                targetInstanceId = targetInstanceId,
                targetIsPlayer = targetIsPlayer,
                targetPlayerId = targetPlayerId
            };
        }

        /// <summary>
        /// 创建进化操作
        /// </summary>
        public static PlayerAction CreateEvolve(int playerId, int instanceId)
        {
            return new PlayerAction
            {
                playerId = playerId,
                actionType = ActionType.Evolve,
                sourceInstanceId = instanceId
            };
        }

        /// <summary>
        /// 创建启动护符操作
        /// </summary>
        public static PlayerAction CreateActivateAmulet(int playerId, int instanceId,
            int targetInstanceId = -1, bool targetIsPlayer = false, int targetPlayerId = -1)
        {
            return new PlayerAction
            {
                playerId = playerId,
                actionType = ActionType.ActivateAmulet,
                sourceInstanceId = instanceId,
                targetInstanceId = targetInstanceId,
                targetIsPlayer = targetIsPlayer,
                targetPlayerId = targetPlayerId
            };
        }

        /// <summary>
        /// 创建结束回合操作
        /// </summary>
        public static PlayerAction CreateEndTurn(int playerId)
        {
            return new PlayerAction
            {
                playerId = playerId,
                actionType = ActionType.EndTurn
            };
        }

        /// <summary>
        /// 创建投降操作
        /// </summary>
        public static PlayerAction CreateSurrender(int playerId)
        {
            return new PlayerAction
            {
                playerId = playerId,
                actionType = ActionType.Surrender
            };
        }
    }
}
