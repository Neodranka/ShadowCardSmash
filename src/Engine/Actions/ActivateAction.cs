using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// Player-issued: spend ActivateCost mana to trigger a field Amulet/Landmark's OnActivate hook.
/// Cooldown is enforced by the card script via <see cref="RuntimeCard.Counters"/>["activate_cd"].
/// </summary>
public sealed record ActivateAction(PlayerSide Issuer, InstanceId Target) : IGameAction
{
    public ActionResult Validate(GameState state)
    {
        if (state.Phase != GamePhase.Main) return ActionResult.Fail("不在主要阶段");
        if (state.CurrentPlayer != Issuer) return ActionResult.Fail("不是你的回合");
        var p = state.GetPlayer(Issuer);
        var card = p.FindOnFieldOrTerrain(Target);
        if (card is null) return ActionResult.Fail("目标不在己方场地");
        return ActionResult.Ok();
    }

    public void Apply(GameContext ctx)
    {
        var p = ctx.State.GetPlayer(Issuer);
        var card = p.FindOnFieldOrTerrain(Target)!;
        var script = ctx.CardDatabase.Get(card.Card);
        if (!script.CanActivate) throw new InvalidActionException("此卡无法启动");
        if (p.Mana < script.ActivateCost) throw new InvalidActionException("费用不足");
        // Cooldown check — cards store their own remaining CD in Counters["activate_cd"].
        card.Counters.TryGetValue("activate_cd", out int cd);
        if (cd > 0) throw new InvalidActionException($"冷却中（还需 {cd} 回合）");

        p.Mana -= script.ActivateCost;
        ctx.Loop.Publish(new ManaChangedEvent(Issuer, p.Mana, p.MaxMana), ctx);

        if (!card.IsSilenced)
        {
            ctx.Source = card;
            ctx.SourceSide = card.Owner;
            script.OnActivate(ctx.WithSource(card));
        }
        ctx.Loop.Publish(new AmuletActivatedEvent(card.Instance), ctx);
    }
}
