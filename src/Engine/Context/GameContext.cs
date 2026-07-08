using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Engine;

/// <summary>
/// Handle passed to IGameAction.Apply and to every card-script hook.
/// Encapsulates: state, RNG, the loop that owns the event chain, plus the "source / chosen targets" of
/// whatever caused this context to be created (so card scripts can reference 'self' and 'picked target').
///
/// Effect primitives are exposed as instance methods on this class — it is the single facade card scripts use.
/// Adding a new primitive: add a method here that calls EffectPrimitives.X(this, ...).
/// </summary>
public sealed class GameContext
{
    public GameState State { get; }
    public IRng Rng { get; }
    public GameLoop Loop { get; }
    public ICardDatabase CardDatabase { get; }

    public PlayerSide SourceSide { get; internal set; }
    public RuntimeCard? Source { get; internal set; }

    public RuntimeCard? PickedTarget { get; internal set; }
    public PlayerSide? PickedPlayerTarget { get; internal set; }
    public int? PickedTileIndex { get; internal set; }
    /// <summary>All minion targets selected for this card (length matches script.TargetsToPick).</summary>
    public IReadOnlyList<RuntimeCard> PickedTargets { get; internal set; } = System.Array.Empty<RuntimeCard>();
    /// <summary>Indices the player chose from CardScript.Choices.</summary>
    public IReadOnlyList<int> PickedChoices { get; internal set; } = System.Array.Empty<int>();
    /// <summary>True when this card's 强化 X condition was met → OnPlay should run the extra effect.</summary>
    public bool IsEnhanced { get; internal set; }

    public GameContext(GameState state, IRng rng, GameLoop loop, ICardDatabase cardDb, PlayerSide sourceSide)
    {
        State = state;
        Rng = rng;
        Loop = loop;
        CardDatabase = cardDb;
        SourceSide = sourceSide;
    }

    public PlayerState Owner => State.GetPlayer(SourceSide);
    public PlayerState Enemy => State.GetPlayer(SourceSide.Opponent());

    /// <summary>
    /// Returns a context for invoking another card's hooks. Shares state/loop/rng but re-roots Source.
    /// Used by EffectPrimitives to dispatch OnDamaged / OnDeath into the *target's* script.
    /// </summary>
    public GameContext WithSource(RuntimeCard newSource) => new(State, Rng, Loop, CardDatabase, newSource.Owner)
    {
        Source = newSource,
        PickedTarget = PickedTarget,
        PickedPlayerTarget = PickedPlayerTarget,
        PickedTileIndex = PickedTileIndex,
        PickedTargets = PickedTargets,
        PickedChoices = PickedChoices,
        IsEnhanced = IsEnhanced,
    };

    // === Effect primitives (facade) ===
    public bool Damage(RuntimeCard target, int amount) => EffectPrimitives.DamageMinion(this, target, amount);
    public void Damage(PlayerSide target, int amount) => EffectPrimitives.DamagePlayer(this, target, amount);
    public void SelfDamage(PlayerSide target, int amount) => EffectPrimitives.SelfDamagePlayer(this, target, amount);
    public void Heal(RuntimeCard target, int amount) => EffectPrimitives.HealMinion(this, target, amount);
    public void Heal(PlayerSide target, int amount) => EffectPrimitives.HealPlayer(this, target, amount);
    public void Draw(PlayerSide side, int count) => EffectPrimitives.Draw(this, side, count);
    public void Discard(PlayerSide side, RuntimeCard card) => EffectPrimitives.Discard(this, side, card);
    public InstanceId? Summon(CardId cardId, PlayerSide side, int? tileIndex = null)
        => EffectPrimitives.Summon(this, cardId, side, tileIndex);
    public void Buff(RuntimeCard target, int atk, int hp, int duration = -1)
        => EffectPrimitives.Buff(this, target, atk, hp, duration);
    public void GainKeyword(RuntimeCard target, Keyword kw) => EffectPrimitives.GainKeyword(this, target, kw);
    public void Silence(RuntimeCard target) => EffectPrimitives.Silence(this, target);
    public void Destroy(RuntimeCard target) => EffectPrimitives.Destroy(this, target);
    public void Vanish(RuntimeCard target) => EffectPrimitives.Vanish(this, target);
    public void GainMana(PlayerSide side, int amount, bool thisTurnOnly)
        => EffectPrimitives.GainMana(this, side, amount, thisTurnOnly);
    public void AddToHand(PlayerSide side, CardId cardId) => EffectPrimitives.AddToHand(this, side, cardId);
    public void ApplyTileEffect(PlayerSide side, int tileIndex, string key, int value, int duration)
        => EffectPrimitives.ApplyTileEffect(this, side, tileIndex, key, value, duration);
    public void GiveBarrier(RuntimeCard target, int stacks = 1) => EffectPrimitives.GiveBarrier(this, target, stacks);
    public void GiveBarrier(PlayerSide side, int stacks = 1) => EffectPrimitives.GiveBarrierToPlayer(this, side, stacks);
    public InstanceId? DrawSpecificFromDeck(PlayerSide side, System.Func<ICardScript, bool> predicate)
        => EffectPrimitives.DrawSpecificFromDeck(this, side, predicate);
    public int SummonUniqueFromDeck(PlayerSide side, System.Func<ICardScript, bool> predicate)
        => EffectPrimitives.SummonUniqueFromDeck(this, side, predicate);

    // Wave-of-2025 primitives (deck/hand/graveyard cycling + counters)
    public int DrawUntilHandFull(PlayerSide side) => EffectPrimitives.DrawUntilHandFull(this, side);
    public void MillRestOfDeck(PlayerSide side) => EffectPrimitives.MillRestOfDeck(this, side);
    public int DrawFromGraveyardRandom(PlayerSide side, int n) => EffectPrimitives.DrawFromGraveyardRandom(this, side, n);
    public int DrawFromGraveyardUntilHandFull(PlayerSide side) => EffectPrimitives.DrawFromGraveyardUntilHandFull(this, side);
    public void ReturnGraveyardToDeck(PlayerSide side) => EffectPrimitives.ReturnGraveyardToDeck(this, side);
    public bool ReturnGraveyardCardToHand(RuntimeCard target) => EffectPrimitives.ReturnGraveyardCardToHand(this, target);
    public void ShuffleHandToDeck(RuntimeCard card) => EffectPrimitives.ShuffleHandToDeck(this, card);
    public void PutHandCardOnTopOfDeck(RuntimeCard card) => EffectPrimitives.PutHandCardOnTopOfDeck(this, card);
    public void RefundMana(PlayerSide side, int amount, string source = "effect")
        => EffectPrimitives.RefundMana(this, side, amount, source);
    public void GrantMana(PlayerSide side, int amount, string source = "effect")
        => EffectPrimitives.GrantMana(this, side, amount, source);
    public void RevealCard(RuntimeCard card) => EffectPrimitives.RevealCard(this, card);
    public int DestroyRandomEnemyMinions(PlayerSide side, int n)
        => EffectPrimitives.DestroyRandomEnemyMinions(this, side, n);
    public void ChangePlayerCounter(PlayerSide side, string key, int delta)
        => EffectPrimitives.ChangePlayerCounter(this, side, key, delta);
    public int ConsumePlayerCounter(PlayerSide side, string key)
        => EffectPrimitives.ConsumePlayerCounter(this, side, key);
}
