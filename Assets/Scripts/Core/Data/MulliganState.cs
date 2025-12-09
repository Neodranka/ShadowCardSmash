using System.Collections.Generic;

namespace ShadowCardSmash.Core.Data
{
    /// <summary>
    /// 换牌阶段状态
    /// </summary>
    [System.Serializable]
    public class MulliganState
    {
        /// <summary>
        /// 玩家是否确认完成换牌
        /// </summary>
        public bool[] playerReady = new bool[2];

        /// <summary>
        /// 玩家选择要换掉的手牌索引
        /// </summary>
        public List<int>[] selectedIndices = new List<int>[2];

        public MulliganState()
        {
            selectedIndices[0] = new List<int>();
            selectedIndices[1] = new List<int>();
        }

        /// <summary>
        /// 检查是否所有玩家都准备好了
        /// </summary>
        public bool AllPlayersReady => playerReady[0] && playerReady[1];

        /// <summary>
        /// 重置状态
        /// </summary>
        public void Reset()
        {
            playerReady[0] = false;
            playerReady[1] = false;
            selectedIndices[0].Clear();
            selectedIndices[1].Clear();
        }
    }
}
