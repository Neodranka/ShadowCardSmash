using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// One generic "play a card from hand" action. Supports minions (tile placement), spells (optional target),
/// and amulets (tile placement).
/// </summary>
public sealed record PlayCardAction(
    PlayerSide Issuer,
    InstanceId HandInstance,
    int? TileIndex,
    InstanceId? TargetMinion,
    PlayerSide? TargetPlayer
) : IGameAction
{
    public ActionResult Validate(GameState state)
    {
        if (state.Phase != GamePhase.Main) return ActionResult.Fail("Not in main phase.");
        if (state.CurrentPlayer != Issuer) return ActionResult.Fail("Not your turn.");

        var p = state.GetPlayer(Issuer);
        var card = p.Hand.FirstOrDefault(c => c.Instance == HandInstance);
        if (card is null) return ActionResult.Fail("Card not in hand.");
        return ActionResult.Ok();
    }

    public void Apply(GameContext ctx)
    {
        var state = ctx.State;
        var p = state.GetPlayer(Issuer);
        var card = p.Hand.First(c => c.Instance == HandInstance);
        var script = ctx.CardDatabase.Get(card.Card);

        if (p.Mana < script.Cost) throw new InvalidActionException("Not enough mana.");

        // Type-specific placement / target gating.
        switch (script.CardType)
        {
            case CardType.Minion:
            case CardType.Amulet:
                if (!TileIndex.HasValue) throw new InvalidActionException("Tile index required for minion/amulet.");
                if (TileIndex.Value < 0 || TileIndex.Value >= PlayerState.FieldSize)
                    throw new InvalidActionException("Tile out of range.");
                if (!p.Field[TileIndex.Value].IsEmpty) throw new InvalidActionException("Tile occupied.");
                break;
            case CardType.Spell:
                ValidateSpellTarget(state, script);
                break;
        }

        // Consume mana, remove from hand.
        p.Mana -= script.Cost;
        p.Hand.Remove(card);
        ctx.Loop.Publish(new ManaChangedEvent(Issuer, p.Mana, p.MaxMana), ctx);
        ctx.Loop.Publish(new CardPlayedEvent(Issuer, card.Instance, card.Card), ctx);

        // Resolve placement / spell.
        if (script.CardType == CardType.Spell)
        {
            card.Zone = Zone.Graveyard;
            p.Graveyard.Add(card);
            RuntimeCard? pickedMinion = ResolvePickedMinion(state);
            ctx.SourceSide = Issuer;
            ctx.Source = card;
            ctx.PickedTarget = pickedMinion;
            ctx.PickedPlayerTarget = TargetPlayer;
            script.OnPlay(ctx);
        }
        else
        {
            // Minion / amulet: rehydrate the hand card with script stats and place it on the chosen tile.
            // Keeps the InstanceId stable across Hand → Field so UI selection and Net replay stay coherent.
            int idx = TileIndex!.Value;
            card.Zone = Zone.Field;
            card.Owner = Issuer;
            card.CurrentAttack = script.BaseAttack;
            card.CurrentHealth = script.BaseHealth;
            card.MaxHealth = script.BaseHealth;
            card.Keywords = script.InitialKeywords;
            card.Countdown = script.InitialCountdown;
            card.CanAttackThisTurn = script.InitialKeywords.HasFlag(Keyword.Storm);
            card.SummonedThisTurn = true;
            p.Field[idx].Occupant = card;

            if (script.CardType == CardType.Amulet)
                ctx.Loop.Publish(new AmuletPlacedEvent(Issuer, card.Instance, card.Card, idx), ctx);
            else
                ctx.Loop.Publish(new MinionSummonedEvent(Issuer, card.Instance, card.Card, idx), ctx);

            ctx.SourceSide = Issuer;
            ctx.Source = card;
            ctx.PickedTarget = ResolvePickedMinion(state);
            ctx.PickedPlayerTarget = TargetPlayer;
            if (!card.IsSilenced) script.OnPlay(ctx);
        }
    }

    private RuntimeCard? ResolvePickedMinion(GameState state)
    {
        if (!TargetMinion.HasValue) return null;
        foreach (var side in new[] { PlayerSide.First, PlayerSide.Second })
        {
            var found = state.GetPlayer(side).FindOnField(TargetMinion.Value);
            if (found is not null) return found;
        }
        return null;
    }

    private void ValidateSpellTarget(GameState state, ICardScript script)
    {
        switch (script.PlayTarget)
        {
            case TargetSpec.None:
                return;
            case TargetSpec.EnemyPlayer:
                if (TargetPlayer != Issuer.Opponent()) throw new InvalidActionException("Spell needs enemy hero target.");
                return;
            case TargetSpec.AllyPlayer:
                if (TargetPlayer != Issuer) throw new InvalidActionException("Spell needs ally hero target.");
                return;
            case TargetSpec.SingleAnyMinion:
            case TargetSpec.SingleEnemyMinion:
            case TargetSpec.SingleAllyMinion:
                if (!TargetMinion.HasValue) throw new InvalidActionException("Spell needs a minion target.");
                var m = ResolvePickedMinion(state) ?? throw new InvalidActionException("Target minion missing.");
                if (script.PlayTarget == TargetSpec.SingleEnemyMinion && m.Owner == Issuer)
                    throw new InvalidActionException("Target must be enemy minion.");
                if (script.PlayTarget == TargetSpec.SingleAllyMinion && m.Owner != Issuer)
                    throw new InvalidActionException("Target must be ally minion.");
                return;
            default:
                return;
        }
    }
}
