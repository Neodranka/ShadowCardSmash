using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>
/// 决斗 — 5 费帝国法术（金）。选择两个随从：若同方，互相加等于对方攻击力的攻击力；若异方，互相造成等于对方攻击力的伤害。
/// V1：双目标选择 UI 未实装，此卡进入卡池但无行为。等 PlayCardAction 支持序列目标后再补。
/// </summary>
[Card(3007)]
public sealed class Duel : SpellCard
{
    public override TargetSpec PlayTarget => TargetSpec.None;

    public override void OnPlay(GameContext ctx)
    {
        // TODO: 需要支持 PlayCardAction 提交两个 InstanceId 的目标列表。
    }
}
