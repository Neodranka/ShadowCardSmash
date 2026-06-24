using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>
/// 决斗 — 5 费帝国法术（金）。选择两个随从：
///   - 若同方：互相获得"等同于对方攻击力"的攻击力；
///   - 若异方：互相造成"等同于对方攻击力"的伤害。
/// </summary>
[Card(3007)]
public sealed class Duel : SpellCard
{
    public override TargetSpec PlayTarget => TargetSpec.SingleAnyMinion;
    public override int TargetsToPick => 2;

    public override void OnPlay(GameContext ctx)
    {
        if (ctx.PickedTargets.Count < 2) return;
        var a = ctx.PickedTargets[0];
        var b = ctx.PickedTargets[1];
        if (a.Instance == b.Instance) return;

        // Capture attack values BEFORE any change (else the second effect would see modified stats).
        int atkA = a.CurrentAttack;
        int atkB = b.CurrentAttack;

        if (a.Owner == b.Owner)
        {
            ctx.Buff(a, atkB, 0, duration: -1);
            ctx.Buff(b, atkA, 0, duration: -1);
        }
        else
        {
            ctx.Damage(a, atkB);
            ctx.Damage(b, atkA);
        }
    }
}
