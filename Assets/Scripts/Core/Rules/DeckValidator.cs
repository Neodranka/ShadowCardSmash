using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Data.Configs;
using ShadowCardSmash.Core.Effects;

namespace ShadowCardSmash.Core.Rules
{
    /// <summary>
    /// 卡组验证器 - 验证卡组是否符合规则
    /// </summary>
    public class DeckValidator
    {
        private DeckRulesConfig _rulesConfig;
        private ICardDatabase _cardDatabase;
        private CompensationCardDatabase _compensationCards;

        public DeckValidator(DeckRulesConfig rulesConfig, ICardDatabase cardDatabase,
            CompensationCardDatabase compensationCards = null)
        {
            _rulesConfig = rulesConfig;
            _cardDatabase = cardDatabase;
            _compensationCards = compensationCards ?? new CompensationCardDatabase();
        }

        /// <summary>
        /// 验证卡组
        /// </summary>
        public ValidationResult ValidateDeck(DeckData deck)
        {
            var result = new ValidationResult { isValid = true, errors = new List<string>() };

            if (deck == null)
            {
                result.isValid = false;
                result.errors.Add("卡组数据为空");
                return result;
            }

            // 1. 验证卡组大小
            int totalCards = deck.GetTotalCardCount();
            if (totalCards < _rulesConfig.minDeckSize)
            {
                result.isValid = false;
                result.errors.Add($"卡组数量不足: {totalCards}/{_rulesConfig.minDeckSize}");
            }
            if (totalCards > _rulesConfig.maxDeckSize)
            {
                result.isValid = false;
                result.errors.Add($"卡组数量超出: {totalCards}/{_rulesConfig.maxDeckSize}");
            }

            // 2. 验证每张卡的数量和职业
            foreach (var entry in deck.cards)
            {
                // 检查卡牌是否存在
                if (_cardDatabase == null || !_cardDatabase.HasCard(entry.cardId))
                {
                    result.isValid = false;
                    result.errors.Add($"卡牌ID {entry.cardId} 不存在");
                    continue;
                }

                var cardData = _cardDatabase.GetCardById(entry.cardId);

                // 检查数量限制
                int maxCopies = cardData.rarity == Rarity.Legendary
                    ? _rulesConfig.maxCopiesLegendary
                    : _rulesConfig.maxCopiesPerCard;

                if (entry.count > maxCopies)
                {
                    result.isValid = false;
                    result.errors.Add($"卡牌 [{cardData.cardName}] 数量超限: {entry.count}/{maxCopies}");
                }

                if (entry.count < 1)
                {
                    result.isValid = false;
                    result.errors.Add($"卡牌 [{cardData.cardName}] 数量无效: {entry.count}");
                }

                // 检查职业限制
                if (_rulesConfig.enforceClassRestriction)
                {
                    if (!_rulesConfig.IsCardClassValidForDeck(cardData.heroClass, deck.heroClass))
                    {
                        result.isValid = false;
                        result.errors.Add($"卡牌 [{cardData.cardName}] 职业不匹配: {cardData.heroClass} vs {deck.heroClass}");
                    }
                }
            }

            // 3. 验证补偿卡
            if (deck.compensationCardId > 0)
            {
                if (!_compensationCards.IsCompensationCard(deck.compensationCardId))
                {
                    result.isValid = false;
                    result.errors.Add($"无效的补偿卡ID: {deck.compensationCardId}");
                }
            }

            return result;
        }

        /// <summary>
        /// 检查是否可以添加卡牌到卡组
        /// </summary>
        public ValidationResult CanAddCard(DeckData deck, int cardId)
        {
            var result = new ValidationResult { isValid = true, errors = new List<string>() };

            // 检查卡牌是否存在
            if (_cardDatabase == null || !_cardDatabase.HasCard(cardId))
            {
                result.isValid = false;
                result.errors.Add($"卡牌ID {cardId} 不存在");
                return result;
            }

            var cardData = _cardDatabase.GetCardById(cardId);

            // 检查卡组是否已满
            if (deck.GetTotalCardCount() >= _rulesConfig.maxDeckSize)
            {
                result.isValid = false;
                result.errors.Add("卡组已满");
                return result;
            }

            // 检查该卡数量限制
            int currentCount = deck.GetCardCount(cardId);
            int maxCopies = cardData.rarity == Rarity.Legendary
                ? _rulesConfig.maxCopiesLegendary
                : _rulesConfig.maxCopiesPerCard;

            if (currentCount >= maxCopies)
            {
                result.isValid = false;
                result.errors.Add($"卡牌 [{cardData.cardName}] 已达上限: {currentCount}/{maxCopies}");
                return result;
            }

            // 检查职业限制
            if (_rulesConfig.enforceClassRestriction)
            {
                if (!_rulesConfig.IsCardClassValidForDeck(cardData.heroClass, deck.heroClass))
                {
                    result.isValid = false;
                    result.errors.Add($"卡牌 [{cardData.cardName}] 职业不匹配");
                    return result;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool isValid;
        public List<string> errors;

        public ValidationResult()
        {
            errors = new List<string>();
        }

        public string GetErrorMessage()
        {
            return string.Join("\n", errors);
        }
    }
}
