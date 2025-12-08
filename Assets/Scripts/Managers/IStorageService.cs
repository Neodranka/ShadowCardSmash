using System.Collections.Generic;
using ShadowCardSmash.Core.Data;

namespace ShadowCardSmash.Managers
{
    /// <summary>
    /// 存储服务接口
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// 保存玩家收藏
        /// </summary>
        void SavePlayerCollection(PlayerCollection collection);

        /// <summary>
        /// 加载玩家收藏
        /// </summary>
        PlayerCollection LoadPlayerCollection(string playerId);

        /// <summary>
        /// 保存卡组
        /// </summary>
        void SaveDeck(string playerId, DeckData deck);

        /// <summary>
        /// 删除卡组
        /// </summary>
        void DeleteDeck(string playerId, string deckId);

        /// <summary>
        /// 加载所有卡组
        /// </summary>
        List<DeckData> LoadAllDecks(string playerId);

        /// <summary>
        /// 检查玩家数据是否存在
        /// </summary>
        bool HasPlayerData(string playerId);

        /// <summary>
        /// 删除所有玩家数据
        /// </summary>
        void DeleteAllPlayerData(string playerId);
    }
}
