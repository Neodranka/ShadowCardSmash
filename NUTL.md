请创建中立卡数据文件，并实现"倾盆大雨"地格效果系统：

## 1. 添加新的枚举值

在 Enums.cs 中添加：
```csharp
// EffectTrigger 添加
OnDraw,           // 抽牌时触发

// EffectType 添加
ApplyTileEffect,  // 给地格施加效果
SummonMultiple,   // 召唤多个随从

// 添加地格效果类型枚举
public enum TileEffectType
{
    None,
    DownpourRain,  // 倾盆大雨：回合结束时随机-1/-0或-0/-1
}
```

## 2. 扩展 TileState

修改 Assets/Scripts/Core/Data/GameState.cs 中的 TileState：
```csharp
[System.Serializable]
public class TileState
{
    public int index;
    public RuntimeCard occupant;
    
    // 地格效果
    public TileEffectType tileEffect;
    public int effectRemainingTurns;  // 剩余持续回合数
    public int effectOwnerId;         // 效果施加者（用于判断"己方"）
    
    public bool HasEffect => tileEffect != TileEffectType.None && effectRemainingTurns > 0;
    
    public void ApplyEffect(TileEffectType effect, int duration, int ownerId)
    {
        tileEffect = effect;
        effectRemainingTurns = duration;
        effectOwnerId = ownerId;
    }
    
    public void ClearEffect()
    {
        tileEffect = TileEffectType.None;
        effectRemainingTurns = 0;
        effectOwnerId = -1;
    }
    
    public void TickEffect()
    {
        if (effectRemainingTurns > 0)
        {
            effectRemainingTurns--;
            if (effectRemainingTurns <= 0)
            {
                ClearEffect();
            }
        }
    }
}
```

## 3. 实现地格效果处理

在 TurnManager.cs 的 EndTurn 方法中添加地格效果处理：
```csharp
// 在回合结束时处理地格效果
private List<GameEvent> ProcessTileEffects(GameState state, int playerId)
{
    var events = new List<GameEvent>();
    var player = state.players[playerId];
    
    for (int i = 0; i < player.field.Length; i++)
    {
        var tile = player.field[i];
        
        if (tile.HasEffect && tile.effectOwnerId == playerId)
        {
            // 这是该玩家的回合结束，处理属于他的地格效果
            switch (tile.tileEffect)
            {
                case TileEffectType.DownpourRain:
                    if (tile.occupant != null)
                    {
                        // 随机-1/-0或-0/-1
                        bool reduceAttack = UnityEngine.Random.value > 0.5f;
                        if (reduceAttack)
                        {
                            tile.occupant.currentAttack = Mathf.Max(0, tile.occupant.currentAttack - 1);
                        }
                        else
                        {
                            tile.occupant.currentHealth -= 1;
                            // 检查死亡
                            if (tile.occupant.currentHealth <= 0)
                            {
                                events.Add(new UnitDestroyedEvent(
                                    tile.occupant.ownerId,
                                    tile.occupant.instanceId,
                                    tile.occupant.cardId,
                                    i,
                                    false
                                ));
                                // 处理死亡...
                            }
                        }
                        events.Add(new TileEffectTriggeredEvent(playerId, i, tile.tileEffect));
                    }
                    break;
            }
            
            // 减少持续回合（无论是否有随从）
            tile.TickEffect();
        }
    }
    
    return events;
}
```

## 4. 创建 TileEffectTriggeredEvent

在 Assets/Scripts/Core/Events/ 创建：
```csharp
namespace ShadowCardSmash.Core.Events
{
    public class TileEffectTriggeredEvent : GameEvent
    {
        public int playerId;
        public int tileIndex;
        public TileEffectType effectType;
        
        public TileEffectTriggeredEvent(int playerId, int tileIndex, TileEffectType effectType)
        {
            this.playerId = playerId;
            this.tileIndex = tileIndex;
            this.effectType = effectType;
        }
    }
}
```

## 5. 创建 ApplyTileEffectExecutor

创建 Assets/Scripts/Core/Effects/Executors/ApplyTileEffectExecutor.cs：
```csharp
using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using UnityEngine;

namespace ShadowCardSmash.Core.Effects.Executors
{
    public class ApplyTileEffectExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            // parameters[0] = 效果类型, parameters[1] = 持续回合, parameters[2] = 选择数量
            if (context.Parameters == null || context.Parameters.Count < 3) return;
            
            var effectType = (TileEffectType)System.Enum.Parse(typeof(TileEffectType), context.Parameters[0]);
            int duration = int.Parse(context.Parameters[1]);
            int selectCount = int.Parse(context.Parameters[2]);
            
            int enemyId = 1 - context.SourcePlayerId;
            var enemyField = context.GameState.players[enemyId].field;
            
            // 选择指定数量的敌方地格（需要玩家选择或随机）
            // 这里先实现随机选择，后续可改为玩家选择
            var availableTiles = new List<int>();
            for (int i = 0; i < enemyField.Length; i++)
            {
                if (!enemyField[i].HasEffect) // 没有效果的格子
                {
                    availableTiles.Add(i);
                }
            }
            
            // 随机选择
            int selected = 0;
            while (selected < selectCount && availableTiles.Count > 0)
            {
                int randomIndex = Random.Range(0, availableTiles.Count);
                int tileIndex = availableTiles[randomIndex];
                
                enemyField[tileIndex].ApplyEffect(effectType, duration, enemyId);
                availableTiles.RemoveAt(randomIndex);
                selected++;
                
                Debug.Log($"地格 {tileIndex} 被施加了 {effectType} 效果，持续 {duration} 回合");
            }
        }
    }
}
```

在 EffectSystemFactory 中注册：
```csharp
system.RegisterExecutor(EffectType.ApplyTileEffect, new ApplyTileEffectExecutor());
```

## 6. 实现抽牌触发效果

修改抽牌逻辑，在抽牌后触发 OnDraw 效果：

在 TurnManager.cs 或相关抽牌方法中：
```csharp
// 抽牌后触发场上随从的 OnDraw 效果
private List<GameEvent> TriggerOnDrawEffects(GameState state, int playerId, int drawCount)
{
    var events = new List<GameEvent>();
    var player = state.players[playerId];
    
    // 遍历场上随从
    foreach (var tile in player.field)
    {
        if (tile.occupant != null)
        {
            var cardData = _cardDatabase?.GetCardById(tile.occupant.cardId);
            if (cardData?.effects != null)
            {
                foreach (var effect in cardData.effects)
                {
                    if (effect.trigger == EffectTrigger.OnDraw)
                    {
                        // 根据抽牌方式决定触发次数
                        // 如果是一次性抽多张（如"抽2张牌"），只触发1次
                        // 这里假设调用时 drawCount 表示触发次数
                        for (int i = 0; i < drawCount; i++)
                        {
                            var effectEvents = _effectSystem.ProcessEffect(
                                state, tile.occupant, playerId, effect, null);
                            events.AddRange(effectEvents);
                        }
                    }
                }
            }
        }
    }
    
    return events;
}
```

注意：对于"调整呼吸"这种"抽2张牌"，drawCount = 1（一次性效果，触发1次）。
对于两个独立的"抽1张牌"效果，各自调用，各触发1次。

## 7. 创建 NeutralCards.cs

创建 Assets/Scripts/Data/NeutralCards.cs：
```csharp
using System.Collections.Generic;
using ShadowCardSmash.Core.Data;

namespace ShadowCardSmash.Data
{
    public static class NeutralCards
    {
        public static List<CardData> GetAllCards()
        {
            return new List<CardData>
            {
                // 铜卡 (4张)
                CreateTimidRecruit(),      // 胆怯的新兵
                CreateDefenseCaptain(),    // 防卫队长
                CreateNewKnight(),         // 新晋骑士
                CreateTownHerbalist(),     // 小镇草药师
                
                // 银卡 (3张)
                CreateCatchBreath(),       // 调整呼吸
                CreateQuartermaster(),     // 军需官
                CreateDownpour(),          // 倾盆大雨
                
                // 金卡 (2张)
                CreateGuildMaster(),       // 公会总管
                CreateDemolitionExpert(),  // 爆破专家
                
                // 彩卡 (1张)
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

        #region 铜卡

        // 胆怯的新兵 - 1费 1/2 铜
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
                tags = new List<string> { "人类" },
                description = "开幕：如果你有其他随从，本随从+1/+0。",
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.Buff,
                        targetType = TargetType.Self,
                        parameters = new List<string> { "1", "0" },
                        condition = "has_other_minions"
                    }
                }
            };
        }

        // 防卫队长 - 4费 4/5 铜
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
                tags = new List<string> { "人类" },
                description = "守护。",
                keywords = new List<Keyword> { Keyword.Ward }
            };
        }

        // 新晋骑士 - 5费 7/5 铜
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
                tags = new List<string> { "人类", "骑士" },
                description = "突进。",
                keywords = new List<Keyword> { Keyword.Rush }
            };
        }

        // 小镇草药师 - 2费 1/3 铜
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
                        parameters = new List<string> { "single_trigger" } // 标记为一次性抽牌，只触发1次OnDraw
                    }
                }
            };
        }

        // 军需官 - 2费 2/2 银
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
                tags = new List<string> { "人类" },
                description = "开幕：选择一张手牌，将其洗入牌库，抽1张牌。",
                requiresTarget = true,
                effects = new List<EffectData>
                {
                    new EffectData
                    {
                        trigger = EffectTrigger.OnPlay,
                        effectType = EffectType.ShuffleAndDraw,
                        targetType = TargetType.HandCard, // 需要选择手牌
                        value = 1,
                        parameters = new List<string> { "shuffle_selected", "draw_1" }
                    }
                }
            };
        }

        // 倾盆大雨 - 2费法术 银
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
                description = "选择3个敌方地格，赋予"倾盆大雨"效果，持续3回合。\n（倾盆大雨：己方回合结束时，随机使本地块上的随从获得-1/-0或-0/-1）",
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
                        parameters = new List<string> { "1", "1" }
                    }
                }
            };
        }

        // 爆破专家 - 4费 3/3 金
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
                description = "召唤2个"新晋骑士"与1个"副团长 戈弗雷"。",
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
```

## 8. 在 HeroClass 枚举中添加 Neutral
```csharp
public enum HeroClass
{
    Neutral,   // 中立
    Vampire,   // 吸血鬼
    // ... 其他职业
}
```

## 9. 更新卡牌数据库

确保 TestCardDatabase 或你使用的卡牌数据库加载中立卡：
```csharp
// 在初始化时
var allCards = new List<CardData>();
allCards.AddRange(VampireCards.GetAllCards());
allCards.AddRange(VampireCards.GetTokenCards());
allCards.AddRange(NeutralCards.GetAllCards());
allCards.AddRange(NeutralCards.GetTokenCards());
```

## 10. 添加缺失的 TargetType
```csharp
public enum TargetType
{
    // ... 现有的 ...
    EnemyTiles,    // 敌方地格（用于倾盆大雨）
    HandCard,      // 手牌（用于军需官）
}
```

完成后告诉我结果！