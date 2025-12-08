namespace ShadowCardSmash.Core.Effects
{
    /// <summary>
    /// 效果执行器接口 - 所有效果执行器必须实现此接口
    /// </summary>
    public interface IEffectExecutor
    {
        /// <summary>
        /// 执行效果
        /// </summary>
        /// <param name="context">效果执行上下文</param>
        void Execute(EffectContext context);
    }
}
