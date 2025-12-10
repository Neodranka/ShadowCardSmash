using System.Collections.Generic;
using ShadowCardSmash.Core.Data;

namespace ShadowCardSmash.Core.Cards
{
    /// <summary>
    /// 中立卡牌数据
    /// </summary>
    public static class NeutralCards
    {
        public static List<CardData> GetAllCards()
        {
            return new List<CardData>
            {
                // === 铜卡 (4张) ===
                CreateTimidRecruit(),      // 胆怯的新兵
                CreateDefenseCaptain(),    // 防卫队长
                CreateNewKnight(),         // 新晋骑士
                CreateTownHerbalist(),     // 小镇草药师

                // === 银卡 (3张) ===
                CreateCatchBreath(),       // 调整呼吸
                CreateQuartermaster(),     // 军需官
                CreateDownpour(),          // 倾盆大雨

                // === 金卡 (2张) ===
                CreateGuildMaster(),       // 公会总管
                CreateDemolitionExpert(),  // 爆破专家

                // === 彩卡 (1张) ===
                CreateSuddenAssembly(),    // 唐突的集结
            };
        }

        public static List<CardData> GetTokenCards()
        {
            return new List<CardData>
            {
                CreateViceCommanderGodfrey(),  // 副团长 戈弗雷
            };
        }

        /// <summary>
        /// 将所有中立卡牌注册到数据库
        /// </summary>
        public static void RegisterToDatabase(Dictionary<int, CardData> database)
        {
            foreach (var card in GetAllCards())
            {
                database[card.cardId] = card;
            }
            foreach (var card in GetTokenCards())
            {
                database[card.cardId] = card;
            }
        }

        #region 铜卡

        // 胆怯的新兵 - 1费 1/2 铜
        // 开幕：如果你有其他随从，本随从+1/+0。
        private static CardData CreateTimidRecruit()
        {
            return new CardData
            {
                cardId = 3001,
                cardName = "胆怯的新兵",
                cardType = CardType.Minion,
                heroClass = HeroClass.Neutral,
                rarity = Rarity.Bronze,
                cost = 1,
                attack = 1,
                health = 2,
                evolvedAttack = 3,
                evolvedHealth = 4,
                tags = new List<string> { "人类" },
                description = "开幕：如果你有其他随从，本随从+1/+0。",
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Buff,
                        targetType = TargetType.Self,
                        value = 1,
                        secondaryValue = 0,
                        condition = "has_other_minions"
                    }
                }
            };
        }

        // 防卫队长 - 4费 4/5 铜
        // 守护
        private static CardData CreateDefenseCaptain()
        {
            return new CardData
            {
                cardId = 3002,
                cardName = "防卫队长",
                cardType = CardType.Minion,
                heroClass = HeroClass.Neutral,
                rarity = Rarity.Bronze,
                cost = 4,
                attack = 4,
                health = 5,
                evolvedAttack = 6,
                evolvedHealth = 7,
                tags = new List<string> { "人类" },
                description = "守护。",
                keywords = new List<Keyword> { Keyword.Ward }
            };
        }

        // 新晋骑士 - 5费 7/5 铜
        // 突进
        private static CardData CreateNewKnight()
        {
            return new CardData
            {
                cardId = 3003,
                cardName = "新晋骑士",
                cardType = CardType.Minion,
                heroClass = HeroClass.Neutral,
                rarity = Rarity.Bronze,
                cost = 5,
                attack = 7,
                health = 5,
                evolvedAttack = 9,
                evolvedHealth = 7,
                tags = new List<string> { "人类", "骑士" },
                description = "突进。",
                keywords = new List<Keyword> { Keyword.Rush }
            };
        }

        // 小镇草药师 - 2费 1/3 铜
        // 开幕：为我方玩家回复2点生命值。
        private static CardData CreateTownHerbalist()
        {
            return new CardData
            {
                cardId = 3004,
                cardName = "小镇草药师",
                cardType = CardType.Minion,
                heroClass = HeroClass.Neutral,
                rarity = Rarity.Bronze,
                cost = 2,
                attack = 1,
                health = 3,
                evolvedAttack = 3,
                evolvedHealth = 5,
                tags = new List<string> { "人类" },
                description = "开幕：为我方玩家回复2点生命值。",
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Heal,
                        targetType = TargetType.AllyPlayer,
                        value = 2
                    }
                }
            };
        }

        #endregion

        #region 银卡

        // 调整呼吸 - 3费法术 银
        // 抽2张牌。
        private static CardData CreateCatchBreath()
        {
            return new CardData
            {
                cardId = 3005,
                cardName = "调整呼吸",
                cardType = CardType.Spell,
                heroClass = HeroClass.Neutral,
                rarity = Rarity.Silver,
                cost = 3,
                tags = new List<string> { "技术" },
                description = "抽2张牌。",
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Draw,
                        targetType = TargetType.AllyPlayer,
                        value = 2,
                        parameters = new List<string> { "single_trigger" } // 一次性抽牌，只触发1次OnDraw
                    }
                }
            };
        }

        // 军需官 - 2费 2/2 银
        // 开幕：选择一张手牌，将其洗入牌库，抽1张牌。
        private static CardData CreateQuartermaster()
        {
            return new CardData
            {
                cardId = 3006,
                cardName = "军需官",
                cardType = CardType.Minion,
                heroClass = HeroClass.Neutral,
                rarity = Rarity.Silver,
                cost = 2,
                attack = 2,
                health = 2,
                evolvedAttack = 4,
                evolvedHealth = 4,
                tags = new List<string> { "人类" },
                description = "开幕：选择一张手牌，将其洗入牌库，抽1张牌。",
                requiresTarget = true,
                validTargets = TargetType.HandCard,
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.ShuffleAndDraw,
                        targetType = TargetType.HandCard,
                        value = 1,
                        parameters = new List<string> { "shuffle_selected", "draw_1" }
                    }
                }
            };
        }

        // 倾盆大雨 - 2费法术 银
        // 选择3个敌方地格，赋予"倾盆大雨"效果，持续3回合。
        private static CardData CreateDownpour()
        {
            return new CardData
            {
                cardId = 3007,
                cardName = "倾盆大雨",
                cardType = CardType.Spell,
                heroClass = HeroClass.Neutral,
                rarity = Rarity.Silver,
                cost = 2,
                tags = new List<string> { "环境", "天候" },
                description = "选择3个敌方地格，赋予「倾盆大雨」效果，持续3回合。\n（倾盆大雨：己方回合结束时，随机使本地块上的随从获得-1/-0或-0/-1）",
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.ApplyTileEffect,
                        targetType = TargetType.EnemyTiles,
                        parameters = new List<string> { "DownpourRain", "3", "3" } // 效果类型, 持续回合, 选择数量
                    }
                }
            };
        }

        #endregion

        #region 金卡

        // 公会总管 - 3费 2/2 金
        // 每当我方玩家抽牌时，获得+1/+1。
        // 开幕：抽1张牌。
        private static CardData CreateGuildMaster()
        {
            return new CardData
            {
                cardId = 3008,
                cardName = "公会总管",
                cardType = CardType.Minion,
                heroClass = HeroClass.Neutral,
                rarity = Rarity.Gold,
                cost = 3,
                attack = 2,
                health = 2,
                evolvedAttack = 4,
                evolvedHealth = 4,
                tags = new List<string> { "人类" },
                description = "每当我方玩家抽牌时，获得+1/+1。\n开幕：抽1张牌。",
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Draw,
                        targetType = TargetType.AllyPlayer,
                        value = 1
                    },
                    new EffectData
                    {
                        trigger = EffectTrigger.OnDraw,
                        effectType = EffectType.Buff,
                        targetType = TargetType.Self,
                        value = 1,
                        secondaryValue = 1
                    }
                }
            };
        }

        // 爆破专家 - 4费 3/3 金
        // 开幕：选择一个敌方随从，对其造成等于其攻击力的伤害。
        private static CardData CreateDemolitionExpert()
        {
            return new CardData
            {
                cardId = 3009,
                cardName = "爆破专家",
                cardType = CardType.Minion,
                heroClass = HeroClass.Neutral,
                rarity = Rarity.Gold,
                cost = 4,
                attack = 3,
                health = 3,
                evolvedAttack = 5,
                evolvedHealth = 5,
                tags = new List<string> { "矮人" },
                description = "开幕：选择一个敌方随从，对其造成等于其攻击力的伤害。",
                requiresTarget = true,
                validTargets = TargetType.SingleEnemy,
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Damage,
                        targetType = TargetType.PlayerChoice,
                        value = -1, // -1 表示使用目标的攻击力
                        parameters = new List<string> { "value_from:target_attack" }
                    }
                }
            };
        }

        #endregion

        #region 彩卡

        // 唐突的集结 - 9费法术 彩
        // 召唤2个"新晋骑士"与1个"副团长 戈弗雷"。
        private static CardData CreateSuddenAssembly()
        {
            return new CardData
            {
                cardId = 3010,
                cardName = "唐突的集结",
                cardType = CardType.Spell,
                heroClass = HeroClass.Neutral,
                rarity = Rarity.Legendary,
                cost = 9,
                tags = new List<string> { "骑士" },
                description = "召唤2个「新晋骑士」与1个「副团长 戈弗雷」。",
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Summon,
                        parameters = new List<string> { "3003" } // 新晋骑士
                    },
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Summon,
                        parameters = new List<string> { "3003" } // 新晋骑士
                    },
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Summon,
                        parameters = new List<string> { "3011" } // 副团长 戈弗雷
                    }
                }
            };
        }

        #endregion

        #region TOKEN

        // 副团长 戈弗雷 - 4费 7/1 TOKEN
        // 突进。屏障。
        private static CardData CreateViceCommanderGodfrey()
        {
            return new CardData
            {
                cardId = 3011,
                cardName = "副团长 戈弗雷",
                cardType = CardType.Minion,
                heroClass = HeroClass.Neutral,
                rarity = Rarity.Token,
                cost = 4,
                attack = 7,
                health = 1,
                evolvedAttack = 9,
                evolvedHealth = 3,
                isToken = true,
                tags = new List<string> { "骑士" },
                description = "突进。屏障。",
                keywords = new List<Keyword> { Keyword.Rush },
                hasBarrier = true
            };
        }

        #endregion
    }
}
