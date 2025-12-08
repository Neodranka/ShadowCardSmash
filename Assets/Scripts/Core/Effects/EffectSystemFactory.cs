using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Effects.Executors;

namespace ShadowCardSmash.Core.Effects
{
    /// <summary>
    /// 效果系统工厂 - 用于创建和初始化效果系统
    /// </summary>
    public static class EffectSystemFactory
    {
        /// <summary>
        /// 创建并初始化一个完整的效果系统
        /// </summary>
        /// <param name="random">随机数生成器（用于随机目标选择）</param>
        /// <returns>初始化好的效果系统</returns>
        public static EffectSystem Create(System.Random random = null)
        {
            var effectSystem = new EffectSystem();

            // 设置目标选择器
            effectSystem.SetTargetSelector(new DefaultTargetSelector(random));

            // 设置条件检查器
            effectSystem.SetConditionChecker(new DefaultConditionChecker());

            // 注册所有效果执行器
            RegisterExecutors(effectSystem);

            return effectSystem;
        }

        /// <summary>
        /// 注册所有效果执行器
        /// </summary>
        private static void RegisterExecutors(EffectSystem effectSystem)
        {
            effectSystem.RegisterExecutor(EffectType.Damage, new DamageExecutor());
            effectSystem.RegisterExecutor(EffectType.Heal, new HealExecutor());
            effectSystem.RegisterExecutor(EffectType.Draw, new DrawExecutor());
            effectSystem.RegisterExecutor(EffectType.Summon, new SummonExecutor());
            effectSystem.RegisterExecutor(EffectType.Buff, new BuffExecutor());
            effectSystem.RegisterExecutor(EffectType.Debuff, new BuffExecutor()); // Debuff使用相同执行器，参数为负数
            effectSystem.RegisterExecutor(EffectType.Destroy, new DestroyExecutor());
            effectSystem.RegisterExecutor(EffectType.Vanish, new VanishExecutor());
            effectSystem.RegisterExecutor(EffectType.Silence, new SilenceExecutor());
            effectSystem.RegisterExecutor(EffectType.Discard, new DiscardExecutor());
            effectSystem.RegisterExecutor(EffectType.GainKeyword, new GainKeywordExecutor());

            // TODO: 实现更多效果执行器
            // effectSystem.RegisterExecutor(EffectType.AddToHand, new AddToHandExecutor());
            // effectSystem.RegisterExecutor(EffectType.Transform, new TransformExecutor());
            // effectSystem.RegisterExecutor(EffectType.Evolve, new EvolveExecutor());
            // effectSystem.RegisterExecutor(EffectType.GainCost, new GainCostExecutor());
            // effectSystem.RegisterExecutor(EffectType.TileEffect, new TileEffectExecutor());
        }
    }
}
