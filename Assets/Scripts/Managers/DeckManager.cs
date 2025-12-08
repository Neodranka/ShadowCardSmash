using System.Collections.Generic;
using UnityEngine;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Data.Configs;
using ShadowCardSmash.Core.Effects;
using ShadowCardSmash.Core.Rules;

namespace ShadowCardSmash.Managers
{
    /// <summary>
    /// 卡组管理器 - 管理玩家的卡组
    /// </summary>
    public class DeckManager
    {
        private IStorageService _storageService;
        private DeckValidator _validator;
        private CompensationCardDatabase _compensationCards;
        private ICardDatabase _cardDatabase;

        private string _currentPlayerId;
        private List<DeckData> _decks;
        private DeckData _currentDeck;
        private int _selectedDeckIndex;

        public List<DeckData> Decks => _decks;
        public DeckData CurrentDeck => _currentDeck;
        public int SelectedDeckIndex => _selectedDeckIndex;

        public DeckManager(IStorageService storageService, ICardDatabase cardDatabase,
            DeckRulesConfig rulesConfig = null)
        {
            _storageService = storageService;
            _cardDatabase = cardDatabase;
            _compensationCards = new CompensationCardDatabase();

            // 如果没有提供规则配置，创建默认配置
            if (rulesConfig == null)
            {
                rulesConfig = ScriptableObject.CreateInstance<DeckRulesConfig>();
            }

            _validator = new DeckValidator(rulesConfig, cardDatabase, _compensationCards);
            _decks = new List<DeckData>();
            _selectedDeckIndex = -1;
        }

        /// <summary>
        /// 初始化（加载玩家卡组）
        /// </summary>
        public void Initialize(string playerId)
        {
            _currentPlayerId = playerId;
            _decks = _storageService.LoadAllDecks(playerId);

            if (_decks.Count > 0)
            {
                _selectedDeckIndex = 0;
                _currentDeck = _decks[0];
            }
            else
            {
                _selectedDeckIndex = -1;
                _currentDeck = null;
            }

            Debug.Log($"DeckManager: Initialized with {_decks.Count} decks for player {playerId}");
        }

        /// <summary>
        /// 获取所有卡组
        /// </summary>
        public List<DeckData> GetAllDecks()
        {
            return new List<DeckData>(_decks);
        }

        /// <summary>
        /// 根据ID获取卡组
        /// </summary>
        public DeckData GetDeck(string deckId)
        {
            return _decks.Find(d => d.deckId == deckId);
        }

        /// <summary>
        /// 创建新卡组
        /// </summary>
        public ValidationResult CreateDeck(string name, HeroClass heroClass)
        {
            var result = new ValidationResult { isValid = true, errors = new List<string>() };

            if (string.IsNullOrEmpty(name))
            {
                result.isValid = false;
                result.errors.Add("卡组名称不能为空");
                return result;
            }

            var newDeck = DeckData.Create(name, heroClass);
            _decks.Add(newDeck);

            // 保存到存储
            _storageService.SaveDeck(_currentPlayerId, newDeck);

            // 如果是第一个卡组，自动选中
            if (_decks.Count == 1)
            {
                _selectedDeckIndex = 0;
                _currentDeck = newDeck;
            }

            Debug.Log($"DeckManager: Created deck '{name}' ({heroClass})");
            return result;
        }

        /// <summary>
        /// 保存卡组
        /// </summary>
        public ValidationResult SaveDeck(DeckData deck)
        {
            // 验证卡组
            var validationResult = _validator.ValidateDeck(deck);

            // 即使验证不通过也保存（允许未完成的卡组）
            _storageService.SaveDeck(_currentPlayerId, deck);

            // 更新内存中的卡组
            int index = _decks.FindIndex(d => d.deckId == deck.deckId);
            if (index >= 0)
            {
                _decks[index] = deck;
            }
            else
            {
                _decks.Add(deck);
            }

            return validationResult;
        }

        /// <summary>
        /// 删除卡组
        /// </summary>
        public void DeleteDeck(string deckId)
        {
            int index = _decks.FindIndex(d => d.deckId == deckId);
            if (index >= 0)
            {
                _decks.RemoveAt(index);
                _storageService.DeleteDeck(_currentPlayerId, deckId);

                // 更新选中状态
                if (_selectedDeckIndex >= _decks.Count)
                {
                    _selectedDeckIndex = _decks.Count - 1;
                }

                if (_selectedDeckIndex >= 0)
                {
                    _currentDeck = _decks[_selectedDeckIndex];
                }
                else
                {
                    _currentDeck = null;
                }

                Debug.Log($"DeckManager: Deleted deck {deckId}");
            }
        }

        /// <summary>
        /// 添加卡牌到卡组
        /// </summary>
        public ValidationResult AddCardToDeck(string deckId, int cardId)
        {
            var deck = GetDeck(deckId);
            if (deck == null)
            {
                return new ValidationResult
                {
                    isValid = false,
                    errors = new List<string> { "卡组不存在" }
                };
            }

            // 验证是否可以添加
            var canAddResult = _validator.CanAddCard(deck, cardId);
            if (!canAddResult.isValid)
            {
                return canAddResult;
            }

            // 添加卡牌
            deck.AddCard(cardId);

            // 保存
            _storageService.SaveDeck(_currentPlayerId, deck);

            return new ValidationResult { isValid = true, errors = new List<string>() };
        }

        /// <summary>
        /// 从卡组移除卡牌
        /// </summary>
        public ValidationResult RemoveCardFromDeck(string deckId, int cardId)
        {
            var deck = GetDeck(deckId);
            if (deck == null)
            {
                return new ValidationResult
                {
                    isValid = false,
                    errors = new List<string> { "卡组不存在" }
                };
            }

            if (!deck.RemoveCard(cardId))
            {
                return new ValidationResult
                {
                    isValid = false,
                    errors = new List<string> { "卡组中没有该卡牌" }
                };
            }

            // 保存
            _storageService.SaveDeck(_currentPlayerId, deck);

            return new ValidationResult { isValid = true, errors = new List<string>() };
        }

        /// <summary>
        /// 设置后手补偿卡
        /// </summary>
        public ValidationResult SetCompensationCard(string deckId, int compensationCardId)
        {
            var deck = GetDeck(deckId);
            if (deck == null)
            {
                return new ValidationResult
                {
                    isValid = false,
                    errors = new List<string> { "卡组不存在" }
                };
            }

            if (!_compensationCards.IsCompensationCard(compensationCardId))
            {
                return new ValidationResult
                {
                    isValid = false,
                    errors = new List<string> { "无效的补偿卡ID" }
                };
            }

            deck.compensationCardId = compensationCardId;

            // 保存
            _storageService.SaveDeck(_currentPlayerId, deck);

            return new ValidationResult { isValid = true, errors = new List<string>() };
        }

        /// <summary>
        /// 选择卡组
        /// </summary>
        public void SelectDeck(string deckId)
        {
            int index = _decks.FindIndex(d => d.deckId == deckId);
            if (index >= 0)
            {
                _selectedDeckIndex = index;
                _currentDeck = _decks[index];
                Debug.Log($"DeckManager: Selected deck '{_currentDeck.deckName}'");
            }
        }

        /// <summary>
        /// 通过索引选择卡组
        /// </summary>
        public void SelectDeckByIndex(int index)
        {
            if (index >= 0 && index < _decks.Count)
            {
                _selectedDeckIndex = index;
                _currentDeck = _decks[index];
            }
        }

        /// <summary>
        /// 获取当前选中的卡组
        /// </summary>
        public DeckData GetSelectedDeck()
        {
            return _currentDeck;
        }

        /// <summary>
        /// 验证当前选中的卡组是否可用于游戏
        /// </summary>
        public ValidationResult ValidateSelectedDeck()
        {
            if (_currentDeck == null)
            {
                return new ValidationResult
                {
                    isValid = false,
                    errors = new List<string> { "未选择卡组" }
                };
            }

            return _validator.ValidateDeck(_currentDeck);
        }

        /// <summary>
        /// 获取所有补偿卡
        /// </summary>
        public List<CardData> GetAllCompensationCards()
        {
            return _compensationCards.GetAllCompensationCards();
        }

        /// <summary>
        /// 复制卡组
        /// </summary>
        public ValidationResult DuplicateDeck(string deckId, string newName)
        {
            var sourceDeck = GetDeck(deckId);
            if (sourceDeck == null)
            {
                return new ValidationResult
                {
                    isValid = false,
                    errors = new List<string> { "源卡组不存在" }
                };
            }

            var newDeck = DeckData.Create(newName, sourceDeck.heroClass);
            newDeck.cards = new List<DeckEntry>();

            foreach (var entry in sourceDeck.cards)
            {
                newDeck.cards.Add(new DeckEntry(entry.cardId, entry.count));
            }

            newDeck.compensationCardId = sourceDeck.compensationCardId;

            _decks.Add(newDeck);
            _storageService.SaveDeck(_currentPlayerId, newDeck);

            return new ValidationResult { isValid = true, errors = new List<string>() };
        }

        /// <summary>
        /// 重命名卡组
        /// </summary>
        public void RenameDeck(string deckId, string newName)
        {
            var deck = GetDeck(deckId);
            if (deck != null && !string.IsNullOrEmpty(newName))
            {
                deck.deckName = newName;
                _storageService.SaveDeck(_currentPlayerId, deck);
            }
        }

        /// <summary>
        /// 获取卡组卡牌总数
        /// </summary>
        public int GetDeckCardCount(string deckId)
        {
            var deck = GetDeck(deckId);
            return deck?.GetTotalCardCount() ?? 0;
        }

        /// <summary>
        /// 检查卡组是否完整（40张）
        /// </summary>
        public bool IsDeckComplete(string deckId)
        {
            var deck = GetDeck(deckId);
            if (deck == null) return false;

            var result = _validator.ValidateDeck(deck);
            return result.isValid;
        }
    }
}
