using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>
/// 布伦哈尔家族骑士 — 5 费 3/2 帝国银，突进 + 守护。
/// 谢幕：召唤一个"布伦哈尔家族骑士"，使其获得屏障并失去谢幕。
/// </summary>
[Card(3006)]
public sealed class BrunhaltKnight : MinionCard
{
    public override void OnDeath(GameContext ctx)
    {
        var spawned = ctx.Summon(Id, ctx.SourceSide);
        if (spawned is null) return;
        var p = ctx.State.GetPlayer(ctx.SourceSide);
        var copy = p.FindOnField(spawned.Value);
        if (copy is null) return;
        ctx.GiveBarrier(copy);
        copy.OnDeathSuppressed = true; // 复制体失去谢幕，避免无限召唤
    }
}
