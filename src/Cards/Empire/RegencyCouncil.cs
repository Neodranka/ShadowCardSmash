using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Empire;

/// <summary>摄政议会 — 1 费地标 传说。
/// 被动：每当你抽牌，"议案"层数 +1（按次数，不按张数）。
/// 开幕：选一张手牌洗回牌库，抽等量；若打出过的"摄政议会">1，则改为洗任意张。
/// 启动 0（冷却 1）：消耗所有议案；按层数阶梯效果：
///   >5   → 抽 1
///   >10  → 额外洗手牌抽等量
///   >20  → 额外破坏所有敌方随从
///   >30  → 额外将本局剩余的疲劳伤害转到对手
/// </summary>
[Card(3011)]
public sealed class RegencyCouncil : AmuletCard
{
    public const string CounterKey = "regency";
    public override bool IsLandmark => true;
    public override bool CanActivate => true;
    public override int ActivateCost => 0;
    public override TargetSpec PlayTarget => TargetSpec.MultipleFromHand;
    public override int TargetsToPick => int.MaxValue; // "任意张手牌"（第 2 张起）；第 1 张时 UI 上限用 min/max=1 由 controller 依据 state 决定

    public override void OnPlay(GameContext ctx)
    {
        // "打出过"次数在 PlayCardAction apply 里已经 +1（本次也计入），所以已有 >=2 时启用多选。
        var p = ctx.Owner;
        p.CardsPlayedCount.TryGetValue(Id.Value, out int played);
        bool multi = played > 1;

        int shuffled = 0;
        int limit = multi ? int.MaxValue : 1;
        foreach (var c in ctx.PickedTargets)
        {
            if (c.Zone != Zone.Hand) continue;
            if (shuffled >= limit) break;
            ctx.ShuffleHandToDeck(c);
            shuffled++;
        }
        if (shuffled > 0) ctx.Draw(ctx.SourceSide, shuffled);
    }

    public override void OnOwnerCardsDrawnBatch(GameContext ctx, int count)
    {
        // Per-draw-event = +1 layer (count = number drawn in the batch; per user Q9 we count events).
        if (count <= 0) return;
        ctx.ChangePlayerCounter(ctx.SourceSide, CounterKey, +1);
    }

    public override void OnActivate(GameContext ctx)
    {
        if (ctx.Source is null) return;
        int layers = ctx.ConsumePlayerCounter(ctx.SourceSide, CounterKey);
        ctx.Source.Counters["activate_cd"] = 1; // CD=1 (cooldown counts down at next own TurnStart)
        if (layers <= 5) return;

        ctx.Draw(ctx.SourceSide, 1);

        if (layers > 10)
        {
            // Shuffle whole hand into deck, then draw equal amount.
            var handSnapshot = new List<RuntimeCard>(ctx.Owner.Hand);
            foreach (var c in handSnapshot) ctx.ShuffleHandToDeck(c);
            ctx.Draw(ctx.SourceSide, handSnapshot.Count);
        }
        if (layers > 20)
        {
            var enemy = ctx.State.GetPlayer(ctx.SourceSide.Opponent());
            var toDestroy = new List<RuntimeCard>();
            foreach (var t in enemy.Field)
                if (t.Occupant is { } m) toDestroy.Add(m);
            foreach (var m in toDestroy) ctx.Destroy(m);
        }
        if (layers > 30)
        {
            ctx.Owner.FatigueRedirected = true;
        }
    }
}
