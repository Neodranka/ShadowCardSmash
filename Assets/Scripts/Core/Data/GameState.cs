using System;

namespace ShadowCardSmash.Core.Data
{
    /// <summary>
    /// 游戏状态 - 记录整个游戏的完整状态
    /// </summary>
    [Serializable]
    public class GameState
    {
        /// <summary>
        /// 当前回合数
        /// </summary>
        public int turnNumber;

        /// <summary>
        /// 当前行动玩家ID（0或1）
        /// </summary>
        public int currentPlayerId;

        /// <summary>
        /// 游戏阶段
        /// </summary>
        public GamePhase phase;

        /// <summary>
        /// 玩家状态数组 [0]=先手, [1]=后手
        /// </summary>
        public PlayerState[] players;

        /// <summary>
        /// 随机种子（用于确定性同步）
        /// </summary>
        public int randomSeed;

        /// <summary>
        /// 换牌阶段状态
        /// </summary>
        public MulliganState mulliganState;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public GameState()
        {
            players = new PlayerState[2];
        }

        /// <summary>
        /// 创建初始游戏状态
        /// </summary>
        public static GameState CreateInitial(PlayerState player0, PlayerState player1, int randomSeed)
        {
            return new GameState
            {
                turnNumber = 0,
                currentPlayerId = 0, // 先手先行动
                phase = GamePhase.NotStarted,
                players = new PlayerState[] { player0, player1 },
                randomSeed = randomSeed
            };
        }

        /// <summary>
        /// 获取当前玩家状态
        /// </summary>
        public PlayerState GetCurrentPlayer()
        {
            return players[currentPlayerId];
        }

        /// <summary>
        /// 获取对手玩家状态
        /// </summary>
        public PlayerState GetOpponentPlayer()
        {
            return players[1 - currentPlayerId];
        }

        /// <summary>
        /// 获取指定玩家状态
        /// </summary>
        public PlayerState GetPlayer(int playerId)
        {
            if (playerId < 0 || playerId > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(playerId), "Player ID must be 0 or 1");
            }
            return players[playerId];
        }

        /// <summary>
        /// 获取对手ID
        /// </summary>
        public int GetOpponentId(int playerId)
        {
            return 1 - playerId;
        }

        /// <summary>
        /// 切换到下一个玩家
        /// </summary>
        public void SwitchPlayer()
        {
            currentPlayerId = 1 - currentPlayerId;
        }

        /// <summary>
        /// 检查游戏是否结束
        /// </summary>
        public bool IsGameOver()
        {
            return phase == GamePhase.GameOver;
        }

        /// <summary>
        /// 获取胜利者ID（-1表示未结束或平局）
        /// </summary>
        public int GetWinnerId()
        {
            if (!IsGameOver()) return -1;

            bool p0Dead = players[0].IsDead();
            bool p1Dead = players[1].IsDead();

            if (p0Dead && p1Dead)
            {
                // 双方同时死亡，当前回合玩家判负
                return 1 - currentPlayerId;
            }
            else if (p0Dead)
            {
                return 1;
            }
            else if (p1Dead)
            {
                return 0;
            }

            return -1;
        }

        /// <summary>
        /// 根据instanceId查找运行时卡牌
        /// </summary>
        public RuntimeCard FindCardByInstanceId(int instanceId)
        {
            foreach (var player in players)
            {
                // 检查手牌
                foreach (var card in player.hand)
                {
                    if (card.instanceId == instanceId) return card;
                }

                // 检查战场
                foreach (var tile in player.field)
                {
                    if (tile.occupant != null && tile.occupant.instanceId == instanceId)
                    {
                        return tile.occupant;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 查找卡牌所在的格子
        /// </summary>
        public TileState FindTileByInstanceId(int instanceId)
        {
            foreach (var player in players)
            {
                foreach (var tile in player.field)
                {
                    if (tile.occupant != null && tile.occupant.instanceId == instanceId)
                    {
                        return tile;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 查找卡牌所属玩家ID
        /// </summary>
        public int FindOwnerByInstanceId(int instanceId)
        {
            var card = FindCardByInstanceId(instanceId);
            return card?.ownerId ?? -1;
        }

        /// <summary>
        /// 深拷贝游戏状态
        /// </summary>
        public GameState DeepCopy()
        {
            // 使用JSON序列化进行深拷贝（简单但不是最高效的方式）
            var json = UnityEngine.JsonUtility.ToJson(this);
            return UnityEngine.JsonUtility.FromJson<GameState>(json);
        }
    }
}
