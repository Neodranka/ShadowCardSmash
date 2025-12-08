using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ShadowCardSmash.Core.Data;

namespace ShadowCardSmash.Managers
{
    /// <summary>
    /// 本地存储服务实现
    /// </summary>
    public class LocalStorageService : IStorageService
    {
        private readonly string _basePath;
        private const string SAVE_FOLDER = "SaveData";
        private const string COLLECTION_PREFIX = "collection_";
        private const string DECKS_FOLDER = "decks";

        public LocalStorageService()
        {
            _basePath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
            EnsureDirectoryExists(_basePath);
        }

        /// <summary>
        /// 自定义基础路径（用于测试）
        /// </summary>
        public LocalStorageService(string customBasePath)
        {
            _basePath = customBasePath;
            EnsureDirectoryExists(_basePath);
        }

        #region Player Collection

        public void SavePlayerCollection(PlayerCollection collection)
        {
            if (collection == null || string.IsNullOrEmpty(collection.playerId))
            {
                Debug.LogError("LocalStorageService: Cannot save null collection or empty playerId");
                return;
            }

            try
            {
                string filePath = GetCollectionFilePath(collection.playerId);
                string json = JsonUtility.ToJson(collection, true);
                File.WriteAllText(filePath, json);
                Debug.Log($"LocalStorageService: Saved collection for player {collection.playerId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"LocalStorageService: Failed to save collection - {e.Message}");
            }
        }

        public PlayerCollection LoadPlayerCollection(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                Debug.LogError("LocalStorageService: Cannot load collection with empty playerId");
                return null;
            }

            try
            {
                string filePath = GetCollectionFilePath(playerId);

                if (!File.Exists(filePath))
                {
                    Debug.Log($"LocalStorageService: No collection found for player {playerId}, creating new");
                    return PlayerCollection.Create(playerId);
                }

                string json = File.ReadAllText(filePath);
                var collection = JsonUtility.FromJson<PlayerCollection>(json);

                // 确保列表不为null
                if (collection.ownedCards == null)
                    collection.ownedCards = new List<CardOwnership>();
                if (collection.decks == null)
                    collection.decks = new List<DeckData>();

                Debug.Log($"LocalStorageService: Loaded collection for player {playerId}");
                return collection;
            }
            catch (Exception e)
            {
                Debug.LogError($"LocalStorageService: Failed to load collection - {e.Message}");
                return PlayerCollection.Create(playerId);
            }
        }

        #endregion

        #region Deck Management

        public void SaveDeck(string playerId, DeckData deck)
        {
            if (string.IsNullOrEmpty(playerId) || deck == null)
            {
                Debug.LogError("LocalStorageService: Cannot save deck with empty playerId or null deck");
                return;
            }

            try
            {
                string deckFolder = GetDeckFolderPath(playerId);
                EnsureDirectoryExists(deckFolder);

                string filePath = Path.Combine(deckFolder, $"{deck.deckId}.json");
                string json = JsonUtility.ToJson(deck, true);
                File.WriteAllText(filePath, json);
                Debug.Log($"LocalStorageService: Saved deck {deck.deckName} ({deck.deckId})");
            }
            catch (Exception e)
            {
                Debug.LogError($"LocalStorageService: Failed to save deck - {e.Message}");
            }
        }

        public void DeleteDeck(string playerId, string deckId)
        {
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(deckId))
            {
                Debug.LogError("LocalStorageService: Cannot delete deck with empty playerId or deckId");
                return;
            }

            try
            {
                string filePath = Path.Combine(GetDeckFolderPath(playerId), $"{deckId}.json");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"LocalStorageService: Deleted deck {deckId}");
                }
                else
                {
                    Debug.LogWarning($"LocalStorageService: Deck {deckId} not found for deletion");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"LocalStorageService: Failed to delete deck - {e.Message}");
            }
        }

        public List<DeckData> LoadAllDecks(string playerId)
        {
            var decks = new List<DeckData>();

            if (string.IsNullOrEmpty(playerId))
            {
                Debug.LogError("LocalStorageService: Cannot load decks with empty playerId");
                return decks;
            }

            try
            {
                string deckFolder = GetDeckFolderPath(playerId);

                if (!Directory.Exists(deckFolder))
                {
                    Debug.Log($"LocalStorageService: No deck folder found for player {playerId}");
                    return decks;
                }

                string[] files = Directory.GetFiles(deckFolder, "*.json");

                foreach (string file in files)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var deck = JsonUtility.FromJson<DeckData>(json);

                        // 确保列表不为null
                        if (deck.cards == null)
                            deck.cards = new List<DeckEntry>();

                        decks.Add(deck);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"LocalStorageService: Failed to load deck from {file} - {e.Message}");
                    }
                }

                Debug.Log($"LocalStorageService: Loaded {decks.Count} decks for player {playerId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"LocalStorageService: Failed to load decks - {e.Message}");
            }

            return decks;
        }

        #endregion

        #region Utility Methods

        public bool HasPlayerData(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return false;

            string collectionPath = GetCollectionFilePath(playerId);
            string deckFolder = GetDeckFolderPath(playerId);

            return File.Exists(collectionPath) || Directory.Exists(deckFolder);
        }

        public void DeleteAllPlayerData(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                Debug.LogError("LocalStorageService: Cannot delete data with empty playerId");
                return;
            }

            try
            {
                // 删除收藏文件
                string collectionPath = GetCollectionFilePath(playerId);
                if (File.Exists(collectionPath))
                {
                    File.Delete(collectionPath);
                }

                // 删除卡组文件夹
                string deckFolder = GetDeckFolderPath(playerId);
                if (Directory.Exists(deckFolder))
                {
                    Directory.Delete(deckFolder, true);
                }

                Debug.Log($"LocalStorageService: Deleted all data for player {playerId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"LocalStorageService: Failed to delete player data - {e.Message}");
            }
        }

        private string GetCollectionFilePath(string playerId)
        {
            return Path.Combine(_basePath, $"{COLLECTION_PREFIX}{playerId}.json");
        }

        private string GetDeckFolderPath(string playerId)
        {
            return Path.Combine(_basePath, DECKS_FOLDER, playerId);
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// 获取存储根路径（用于调试）
        /// </summary>
        public string GetBasePath()
        {
            return _basePath;
        }

        #endregion
    }
}
