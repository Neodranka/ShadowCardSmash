using System.Collections.Generic;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Events;

namespace ShadowCardSmash.Core.Effects.Executors
{
    /// <summary>
    /// 获得费用执行器 - 增加当前回合可用费用
    /// </summary>
    public class GainManaExecutor : IEffectExecutor
    {
        public void Execute(EffectContext context)
        {
            var player = context.GameState.players[context.SourcePlayerId];

            int gainAmount = context.Value;
            int oldMana = player.mana;

            // 增加费用，可以超过maxMana（临时费用）
            player.mana += gainAmount;

            UnityEngine.Debug.Log($"Player {context.SourcePlayerId} gained {gainAmount} mana: {oldMana} -> {player.mana}");
        }
    }
}
