using System.Collections.Generic;
using ShadowCardSmash.Core.Data;

namespace ShadowCardSmash.Core.Cards
{
    /// <summary>
    /// 吸血鬼职业卡牌数据
    /// </summary>
    public static class VampireCards
    {
        public static List<CardData> GetAllCards()
        {
            return new List<CardData>
            {
                // === 铜卡 (5张) ===
                CreateBloodSeller(),         // 卖血者
                CreateBloodThorn(),          // 血刺
                CreateBloodOffering(),       // 鲜血献礼
                CreateBite(),                // 撕咬
                CreateHungryPredator(),      // 饥饿的捕食者

                // === 银卡 (5张) ===
                CreateLifeForDeath(),        // 以伤换命
                CreateBloodFan(),            // 血扇
                CreateBloodFanatic(),        // 鲜血狂信徒
                CreateBloodExecutor(),       // 鲜血执行者
                CreateThirstRune(),          // 渴血符文

                // === 金卡 (3张) ===
                CreateBloodMadman(),         // 渴血的狂人
                CreateKurenti(),             // 克伦缇
                CreateBloodGolem(),          // 鲜血魔像

                // === 彩卡 (2张) ===
                CreateLiviere(),             // 利维耶·西缇恩茨
                CreateBloodPriest(),         // 鲜血祭司
            };
        }

        public static List<CardData> GetTokenCards()
        {
            return new List<CardData>
            {
                CreateLongAwaitedHunt(),     // 久违的捕食 (TOKEN)
            };
        }

        /// <summary>
        /// 将所有吸血鬼卡牌注册到数据库
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

        // 卖血者 - 1费 1/1 铜
        // 开幕：对我方玩家造成2点伤害，抽1张牌。
        private static CardData CreateBloodSeller()
        {
            return new CardData
            {
                cardId = 2001,
                cardName = "卖血者",
                cardType = CardType.Minion,
                heroClass = HeroClass.Vampire,
                rarity = Rarity.Bronze,
                cost = 1,
                attack = 1,
                health = 1,
                evolvedAttack = 3,
                evolvedHealth = 3,
                tags = new List<string> { "人类" },
                description = "开幕：对我方玩家造成2点伤害，抽1张牌。",
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.SelfDamage,
                        targetType = TargetType.AllyPlayer,
                        value = 2
                    },
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Draw,
                        targetType = TargetType.AllyPlayer,
                        value = 1
                    }
                }
            };
        }

        // 血刺 - 1费法术 铜
        // 选择一个敌方随从，对其造成2点伤害，对我方玩家造成1点伤害，如果该随从因此伤害被破坏，则对敌方玩家造成2点伤害。
        private static CardData CreateBloodThorn()
        {
            return new CardData
            {
                cardId = 2002,
                cardName = "血刺",
                cardType = CardType.Spell,
                heroClass = HeroClass.Vampire,
                rarity = Rarity.Bronze,
                cost = 1,
                tags = new List<string> { "魔法" },
                description = "选择一个敌方随从，对其造成2点伤害，对我方玩家造成1点伤害。如果该随从因此伤害被破坏，则对敌方玩家造成2点伤害。",
                requiresTarget = true,
                validTargets = TargetType.SingleEnemy,
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Damage,
                        targetType = TargetType.PlayerChoice,
                        value = 2,
                        parameters = new List<string> { "on_kill:damage_enemy_player:2" }
                    },
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.SelfDamage,
                        targetType = TargetType.AllyPlayer,
                        value = 1
                    }
                }
            };
        }

        // 鲜血献礼 - 1费法术 铜
        // 对我方玩家造成1点伤害，对随机敌方随从造成3点伤害。
        // 如果本局对战中我方玩家在自己回合中受到的伤害为15以上，我方玩家的生命值回复3点。
        private static CardData CreateBloodOffering()
        {
            return new CardData
            {
                cardId = 2003,
                cardName = "鲜血献礼",
                cardType = CardType.Spell,
                heroClass = HeroClass.Vampire,
                rarity = Rarity.Bronze,
                cost = 1,
                tags = new List<string> { "魔法", "仪式" },
                description = "对我方玩家造成1点伤害，对随机敌方随从造成3点伤害。\n如果本局对战中我方玩家在自己回合中受到的伤害为15以上，我方玩家的生命值回复3点。",
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.SelfDamage,
                        targetType = TargetType.AllyPlayer,
                        value = 1
                    },
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Damage,
                        targetType = TargetType.RandomEnemy,
                        value = 3
                    },
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Heal,
                        targetType = TargetType.AllyPlayer,
                        value = 3,
                        condition = "total_self_damage >= 15"
                    }
                }
            };
        }

        // 撕咬 - 2费法术 铜
        // 选择一个敌方随从或玩家，对其造成3点伤害。
        // 强化 4：己方玩家的生命值回复5点。
        private static CardData CreateBite()
        {
            return new CardData
            {
                cardId = 2004,
                cardName = "撕咬",
                cardType = CardType.Spell,
                heroClass = HeroClass.Vampire,
                rarity = Rarity.Bronze,
                cost = 2,
                enhanceCost = 4,
                tags = new List<string> { "技术" },
                description = "选择一个敌方随从或玩家，对其造成3点伤害。\n强化 4：己方玩家的生命值回复5点。",
                requiresTarget = true,
                validTargets = TargetType.SingleEnemy, // 包括玩家
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Damage,
                        targetType = TargetType.PlayerChoice,
                        value = 3
                    }
                },
                enhanceEffects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Heal,
                        targetType = TargetType.AllyPlayer,
                        value = 5
                    }
                }
            };
        }

        // 饥饿的捕食者 - 2费 2/1 铜
        // 疾驰。开幕：选择1张手牌中的随从牌丢弃，若成功丢弃，则本随从+1/+1。
        private static CardData CreateHungryPredator()
        {
            return new CardData
            {
                cardId = 2005,
                cardName = "饥饿的捕食者",
                cardType = CardType.Minion,
                heroClass = HeroClass.Vampire,
                rarity = Rarity.Bronze,
                cost = 2,
                attack = 2,
                health = 1,
                evolvedAttack = 4,
                evolvedHealth = 3,
                tags = new List<string> { "吸血鬼" },
                description = "疾驰。\n开幕：选择1张手牌中的随从牌丢弃，若成功丢弃，则本随从+1/+1。",
                keywords = new List<Keyword> { Keyword.Storm },
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.DiscardToGain,
                        targetType = TargetType.Self,
                        value = 1, // 弃牌数量
                        parameters = new List<string> { "filter:minion", "buff:1,1" }
                    }
                }
            };
        }

        #endregion

        #region 银卡

        // 以伤换命 - 1费法术 银
        // 选择一个敌方随从并破坏。对我方玩家造成等于其费用的伤害。
        private static CardData CreateLifeForDeath()
        {
            return new CardData
            {
                cardId = 2006,
                cardName = "以伤换命",
                cardType = CardType.Spell,
                heroClass = HeroClass.Vampire,
                rarity = Rarity.Silver,
                cost = 1,
                tags = new List<string> { "技术" },
                description = "选择一个敌方随从并破坏。\n对我方玩家造成等于其费用的伤害。",
                requiresTarget = true,
                validTargets = TargetType.SingleEnemy,
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Destroy,
                        targetType = TargetType.PlayerChoice
                    },
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.SelfDamage,
                        targetType = TargetType.AllyPlayer,
                        value = -1, // -1 表示使用目标的费用
                        parameters = new List<string> { "value_from:target_cost" }
                    }
                }
            };
        }

        // 血扇 - 1费法术 银
        // 对所有随从与双方玩家造成1点伤害。
        // 强化 3：再造成2点伤害。
        private static CardData CreateBloodFan()
        {
            return new CardData
            {
                cardId = 2007,
                cardName = "血扇",
                cardType = CardType.Spell,
                heroClass = HeroClass.Vampire,
                rarity = Rarity.Silver,
                cost = 1,
                enhanceCost = 3,
                tags = new List<string> { "魔法" },
                description = "对所有随从与双方玩家造成1点伤害。\n强化 3：再造成2点伤害。",
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Damage,
                        targetType = TargetType.All, // 所有随从+双方玩家
                        value = 1
                    }
                },
                enhanceEffects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Damage,
                        targetType = TargetType.All,
                        value = 2
                    }
                }
            };
        }

        // 鲜血狂信徒 - 2费 2/1 银
        // 开幕：对我方玩家造成1点伤害，抽1张牌。如果本局对战中我方玩家在自己回合中受到的伤害为15以上，抽一张牌。
        private static CardData CreateBloodFanatic()
        {
            return new CardData
            {
                cardId = 2008,
                cardName = "鲜血狂信徒",
                cardType = CardType.Minion,
                heroClass = HeroClass.Vampire,
                rarity = Rarity.Silver,
                cost = 2,
                attack = 2,
                health = 1,
                evolvedAttack = 4,
                evolvedHealth = 3,
                tags = new List<string> { "人类" },
                description = "开幕：对我方玩家造成1点伤害，抽1张牌。如果本局对战中我方玩家在自己回合中受到的伤害为15以上，抽一张牌。",
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.SelfDamage,
                        targetType = TargetType.AllyPlayer,
                        value = 1
                    },
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Draw,
                        targetType = TargetType.AllyPlayer,
                        value = 1
                    },
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Draw,
                        targetType = TargetType.AllyPlayer,
                        value = 1,
                        condition = "total_self_damage >= 15"
                    }
                }
            };
        }

        // 鲜血执行者 - 4费 6/6 银
        // 开幕：对我方玩家造成2点伤害。如果本局对战中我方玩家在自己回合中受到的伤害为15以上，本随从获得吸血。
        private static CardData CreateBloodExecutor()
        {
            return new CardData
            {
                cardId = 2009,
                cardName = "鲜血执行者",
                cardType = CardType.Minion,
                heroClass = HeroClass.Vampire,
                rarity = Rarity.Silver,
                cost = 4,
                attack = 6,
                health = 6,
                evolvedAttack = 8,
                evolvedHealth = 8,
                tags = new List<string> { "人类" },
                description = "开幕：对我方玩家造成2点伤害。如果本局对战中我方玩家在自己回合中受到的伤害为15以上，本随从获得吸血。",
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.SelfDamage,
                        targetType = TargetType.AllyPlayer,
                        value = 2
                    },
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.GainKeyword,
                        targetType = TargetType.Self,
                        parameters = new List<string> { "Drain" },
                        condition = "total_self_damage >= 15"
                    }
                }
            };
        }

        // 渴血符文 - 1费法术 银
        // 选择一个随从，使其获得"己方回合结束时，对我方玩家造成1点伤害"效果与+2/+1。
        private static CardData CreateThirstRune()
        {
            return new CardData
            {
                cardId = 2010,
                cardName = "渴血符文",
                cardType = CardType.Spell,
                heroClass = HeroClass.Vampire,
                rarity = Rarity.Silver,
                cost = 1,
                tags = new List<string> { "魔法", "符文" },
                description = "选择一个随从，使其获得'己方回合结束时，对我方玩家造成1点伤害'效果与+2/+1。",
                requiresTarget = true,
                validTargets = TargetType.SingleMinion,
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Buff,
                        targetType = TargetType.PlayerChoice,
                        value = 2,
                        secondaryValue = 1
                    },
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.AddEffect,
                        targetType = TargetType.PlayerChoice,
                        parameters = new List<string> { "OnOwnerTurnEnd", "SelfDamage", "1" }
                    }
                }
            };
        }

        #endregion

        #region 金卡

        // 渴血的狂人 - 3费 2/3 金
        // 我方玩家在自己的回合收到伤害时，本随从+1/+1。
        // 开幕：对我方玩家造成2点伤害。
        private static CardData CreateBloodMadman()
        {
            return new CardData
            {
                cardId = 2011,
                cardName = "渴血的狂人",
                cardType = CardType.Minion,
                heroClass = HeroClass.Vampire,
                rarity = Rarity.Gold,
                cost = 3,
                attack = 2,
                health = 3,
                evolvedAttack = 4,
                evolvedHealth = 5,
                tags = new List<string> { "人类" },
                description = "我方玩家在自己的回合收到伤害时，本随从+1/+1。\n开幕：对我方玩家造成2点伤害。",
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.SelfDamage,
                        targetType = TargetType.AllyPlayer,
                        value = 2
                    },
                    new EffectData
                    {
                        trigger = EffectTrigger.OnOwnerDamaged,
                        effectType = EffectType.Buff,
                        targetType = TargetType.Self,
                        value = 1,
                        secondaryValue = 1,
                        condition = "is_own_turn"
                    }
                }
            };
        }

        // 真祖大人的女仆 克伦缇 - 4费 3/2 金
        // 开幕：使我方玩家获得屏障。
        // 进化时：本随从-2/-2，对我方玩家造成12点伤害，随后对所有敌方随从分配X点伤害。
        // （X为：12减去我方玩家因此能力受到的伤害。）
        private static CardData CreateKurenti()
        {
            return new CardData
            {
                cardId = 2012,
                cardName = "真祖大人的女仆 克伦缇",
                cardType = CardType.Minion,
                heroClass = HeroClass.Vampire,
                rarity = Rarity.Gold,
                cost = 4,
                attack = 3,
                health = 2,
                evolvedAttack = 5,
                evolvedHealth = 4,
                tags = new List<string> { "吸血鬼" },
                description = "开幕：使我方玩家获得屏障。\n进化时：本随从-2/-2，对我方玩家造成12点伤害，随后对所有敌方随从随机分配X点伤害。（X为：12减去我方玩家因此能力受到的伤害。）",
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.GainBarrier,
                        targetType = TargetType.AllyPlayer
                    }
                },
                evolveEffects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnEvolve,
                        effectType = EffectType.Buff,
                        targetType = TargetType.Self,
                        value = -2,
                        secondaryValue = -2
                    },
                    new EffectData
                    {
                        trigger = EffectTrigger.OnEvolve,
                        effectType = EffectType.KurentiSpecial, // 特殊效果需要单独实现
                        targetType = TargetType.AllEnemies,
                        value = 12
                    }
                }
            };
        }

        // 鲜血魔像 - 9费 5/5 金
        // 开幕：破坏所有其他随从。如果本局对战中我方玩家在自己回合中受到的伤害为15以上，我方玩家的生命值回复X点。
        // （X为：被此效果破坏的随从数量）
        private static CardData CreateBloodGolem()
        {
            return new CardData
            {
                cardId = 2013,
                cardName = "鲜血魔像",
                cardType = CardType.Minion,
                heroClass = HeroClass.Vampire,
                rarity = Rarity.Gold,
                cost = 9,
                attack = 5,
                health = 5,
                evolvedAttack = 7,
                evolvedHealth = 7,
                tags = new List<string> { "炼金生物" },
                description = "开幕：破坏所有其他随从。如果本局对战中我方玩家在自己回合中受到的伤害为15以上，我方玩家的生命值回复X点。（X为：被此效果破坏的随从数量）",
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.DestroyAllOther, // 破坏所有其他随从
                        targetType = TargetType.AllMinions,
                        parameters = new List<string> { "heal_by_count" },
                        condition = "total_self_damage >= 15"
                    }
                }
            };
        }

        #endregion

        #region 彩卡

        // 沉睡的饥渴 利维耶·西缇恩茨 - 1费 31/31 彩
        private static CardData CreateLiviere()
        {
            return new CardData
            {
                cardId = 2014,
                cardName = "沉睡的饥渴 利维耶·西缇恩茨",
                cardType = CardType.Minion,
                heroClass = HeroClass.Vampire,
                rarity = Rarity.Legendary,
                cost = 1,
                enhanceCost = 10,
                attack = 31,
                health = 31,
                evolvedAttack = 33,
                evolvedHealth = 33,
                tags = new List<string> { "吸血鬼" },
                canEvolveWithEP = false, // 无法使用EP进化
                description = "无法使用EP进化。\n开幕：本随从-30/-30。\n谢幕：如果是对方的回合，则对对方玩家造成4点伤害，我方玩家的生命值回复4点。\n己方回合结束时，对我方玩家造成1点伤害，如果本局对战中我方玩家在自己的回合受到伤害的次数为7以上，则本随从+2/+2。\n若本随从已进化，己方回合结束时，如果本回合内有随从被破坏，则本随从+2/+2。\n强化 10：本随从的开幕效果变为：本随从-27/-27，进化本随从，并增加一张'久违的捕食'到手牌中。",
                effects = new List<EffectData>
                {
                    // 普通开幕：-30/-30
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Buff,
                        targetType = TargetType.Self,
                        value = -30,
                        secondaryValue = -30
                    },
                    // 谢幕：对方回合时触发
                    new EffectData
                    {
                        trigger = EffectTrigger.OnDestroy,
                        effectType = EffectType.Damage,
                        targetType = TargetType.EnemyPlayer,
                        value = 4,
                        condition = "is_enemy_turn"
                    },
                    new EffectData
                    {
                        trigger = EffectTrigger.OnDestroy,
                        effectType = EffectType.Heal,
                        targetType = TargetType.AllyPlayer,
                        value = 4,
                        condition = "is_enemy_turn"
                    },
                    // 己方回合结束时
                    new EffectData
                    {
                        trigger = EffectTrigger.OnOwnerTurnEnd,
                        effectType = EffectType.SelfDamage,
                        targetType = TargetType.AllyPlayer,
                        value = 1
                    },
                    new EffectData
                    {
                        trigger = EffectTrigger.OnOwnerTurnEnd,
                        effectType = EffectType.Buff,
                        targetType = TargetType.Self,
                        value = 2,
                        secondaryValue = 2,
                        condition = "self_damage_count >= 7"
                    },
                    // 进化后：有随从被破坏时+2/+2
                    new EffectData
                    {
                        trigger = EffectTrigger.OnOwnerTurnEnd,
                        effectType = EffectType.Buff,
                        targetType = TargetType.Self,
                        value = 2,
                        secondaryValue = 2,
                        condition = "is_evolved && minion_destroyed_this_turn"
                    }
                },
                enhanceEffects = new List<EffectData>
                {
                    // 强化10开幕：-27/-27
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Buff,
                        targetType = TargetType.Self,
                        value = -27,
                        secondaryValue = -27,
                        overrideNormalEffect = true // 替代普通效果
                    },
                    // 自动进化
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.SelfEvolve,
                        targetType = TargetType.Self
                    },
                    // 加入久违的捕食
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.AddCardToHand,
                        targetType = TargetType.AllyPlayer,
                        parameters = new List<string> { "2015" } // 久违的捕食的cardId
                    }
                }
            };
        }

        // 鲜血祭司 - 5费 3/3 彩
        // 开幕：比较双方玩家的生命值，对生命值高的一方造成伤害，使得双方玩家的生命值相等。
        // 如果本局对战中我方玩家在自己回合中受到的伤害为15以上，则改为交换双方玩家的生命值。
        private static CardData CreateBloodPriest()
        {
            return new CardData
            {
                cardId = 2016,
                cardName = "鲜血祭司",
                cardType = CardType.Minion,
                heroClass = HeroClass.Vampire,
                rarity = Rarity.Legendary,
                cost = 5,
                attack = 3,
                health = 3,
                evolvedAttack = 5,
                evolvedHealth = 5,
                tags = new List<string> { "人类", "吸血鬼" },
                description = "开幕：比较双方玩家的生命值，对生命值高的一方造成伤害，使得双方玩家的生命值相等。\n如果本局对战中我方玩家在自己回合中受到的伤害为15以上，则改为交换双方玩家的生命值。",
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.EqualizeHealth,
                        condition = "total_self_damage < 15"
                    },
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.SwapHealth,
                        condition = "total_self_damage >= 15"
                    }
                }
            };
        }

        #endregion

        #region TOKEN

        // 久违的捕食 - 0费法术 TOKEN
        // 对所有敌方随从与敌方玩家造成6点伤害，根据被破坏的随从数使我方场上的"沉睡的饥渴 利维耶·西提恩茨"获得+1/+1。
        private static CardData CreateLongAwaitedHunt()
        {
            return new CardData
            {
                cardId = 2015,
                cardName = "久违的捕食",
                cardType = CardType.Spell,
                heroClass = HeroClass.Vampire,
                rarity = Rarity.Token,
                cost = 0,
                tags = new List<string> { "技术", "魔法" },
                description = "对所有敌方随从与敌方玩家造成6点伤害，根据被破坏的随从数使我方场上的'沉睡的饥渴 利维耶·西缇恩茨'获得+1/+1。",
                isToken = true,
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Damage,
                        targetType = TargetType.AllEnemies, // 包括敌方玩家
                        value = 6,
                        parameters = new List<string> { "include_enemy_player", "buff_liviere_by_kill_count" }
                    }
                }
            };
        }

        #endregion
    }
}
