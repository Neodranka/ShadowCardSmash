using System;
using System.Collections.Generic;

namespace ShadowCardSmash.Core.Data
{
    /// <summary>
    /// 格子状态 - 战场上单个格子的状态
    /// </summary>
    [Serializable]
    public class TileState
    {
        /// <summary>
        /// 格子索引（0-5）
        /// </summary>
        public int tileIndex;

        /// <summary>
        /// 占据该格子的单位（可为null）
        /// </summary>
        public RuntimeCard occupant;

        /// <summary>
        /// 格子上的效果列表
        /// </summary>
        public List<TileEffect> effects;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public TileState()
        {
            effects = new List<TileEffect>();
        }

        /// <summary>
        /// 带索引的构造函数
        /// </summary>
        public TileState(int index)
        {
            tileIndex = index;
            occupant = null;
            effects = new List<TileEffect>();
        }

        /// <summary>
        /// 检查格子是否为空
        /// </summary>
        public bool IsEmpty()
        {
            return occupant == null;
        }

        /// <summary>
        /// 放置单位
        /// </summary>
        public bool PlaceUnit(RuntimeCard unit)
        {
            if (!IsEmpty()) return false;
            occupant = unit;
            return true;
        }

        /// <summary>
        /// 移除单位
        /// </summary>
        public RuntimeCard RemoveUnit()
        {
            var unit = occupant;
            occupant = null;
            return unit;
        }

        /// <summary>
        /// 添加格子效果
        /// </summary>
        public void AddEffect(TileEffect effect)
        {
            effects.Add(effect);
        }

        /// <summary>
        /// 移除过期效果
        /// </summary>
        public void RemoveExpiredEffects()
        {
            effects.RemoveAll(e => e.remainingTurns == 0);
        }

        /// <summary>
        /// 减少效果持续时间
        /// </summary>
        public void TickEffects()
        {
            foreach (var effect in effects)
            {
                if (effect.remainingTurns > 0)
                {
                    effect.remainingTurns--;
                }
            }
        }

        /// <summary>
        /// 检查是否有特定类型的地格效果
        /// </summary>
        public bool HasTileEffect(TileEffectType effectType)
        {
            foreach (var effect in effects)
            {
                if (effect.tileEffectType == effectType && effect.remainingTurns != 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取特定类型的地格效果
        /// </summary>
        public TileEffect GetTileEffect(TileEffectType effectType)
        {
            foreach (var effect in effects)
            {
                if (effect.tileEffectType == effectType && effect.remainingTurns != 0)
                {
                    return effect;
                }
            }
            return null;
        }

        /// <summary>
        /// 添加地格效果（倾盆大雨等）
        /// </summary>
        public void ApplyTileEffect(TileEffectType effectType, int duration, int ownerId)
        {
            var effect = new TileEffect(effectType, duration, ownerId);
            effects.Add(effect);
        }

        /// <summary>
        /// 检查是否有任何活跃的地格效果
        /// </summary>
        public bool HasAnyTileEffect()
        {
            foreach (var effect in effects)
            {
                if (effect.tileEffectType != TileEffectType.None && effect.remainingTurns != 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
