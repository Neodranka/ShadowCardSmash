using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>
/// 帝国之墙 拉多万·布伦哈尔 — 10 费 5/5 帝国彩。屏障。
/// 开幕：牌库中尽可能召唤名字互不相同的"布伦哈尔"随从。
/// 持续：我方"布伦哈尔"随从入场时，使其获得屏障与守护。
/// 持续：我方其他随从失去屏障时，自身与对方都获得 +1/+1，自身获得屏障。
/// </summary>
[Card(3005)]
public sealed class EmpireWallRadovan : MinionCard
{
    public override void OnPlay(GameContext ctx)
    {
        // 排除自己（cardId 已在场，SummonUnique 内部走 cardId 唯一性，不会再召唤一份）。
        ctx.SummonUniqueFromDeck(ctx.SourceSide, s => s.Tags.Contains("布伦哈尔") && s.Id != Id);
    }

    public override void OnFieldMinionEntered(GameContext ctx, RuntimeCard newcomer)
    {
        if (ctx.Source is null || ctx.Source.Zone != Zone.Field) return;
        if (newcomer.Owner != ctx.Source.Owner) return;
        if (newcomer.Instance == ctx.Source.Instance) return; // 自己入场不触发
        var script = ctx.CardDatabase.Get(newcomer.Card);
        if (!script.Tags.Contains("布伦哈尔")) return;
        ctx.GiveBarrier(newcomer);
        ctx.GainKeyword(newcomer, Keyword.Ward);
    }

    public override void OnFieldMinionBarrierLost(GameContext ctx, RuntimeCard target)
    {
        if (ctx.Source is null || ctx.Source.Zone != Zone.Field) return;
        if (target.Owner != ctx.Source.Owner) return;
        ctx.Buff(ctx.Source, +1, +1, duration: -1);
        ctx.Buff(target, +1, +1, duration: -1);
        ctx.GiveBarrier(ctx.Source);
    }
}
