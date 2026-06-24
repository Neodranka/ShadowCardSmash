namespace ShadowCardSmash.Domain;

public sealed class RuntimeCard
{
    public InstanceId Instance;
    public CardId Card;
    public PlayerSide Owner;
    public Zone Zone;

    public int CurrentAttack;
    public int CurrentHealth;
    public int MaxHealth;

    public bool IsEvolved;
    public bool CanAttackThisTurn;
    public bool IsSilenced;
    public int AttacksThisTurn;

    public int Countdown = -1;

    public Keyword Keywords;

    public List<BuffData> Buffs = new();

    public int BarrierStacks;

    public bool SummonedThisTurn;

    /// <summary>If true, the OnDeath hook is skipped (used by spawned copies that "lose 谢幕").</summary>
    public bool OnDeathSuppressed;

    public RuntimeCard Clone()
    {
        var copy = new RuntimeCard
        {
            Instance = Instance,
            Card = Card,
            Owner = Owner,
            Zone = Zone,
            CurrentAttack = CurrentAttack,
            CurrentHealth = CurrentHealth,
            MaxHealth = MaxHealth,
            IsEvolved = IsEvolved,
            CanAttackThisTurn = CanAttackThisTurn,
            IsSilenced = IsSilenced,
            AttacksThisTurn = AttacksThisTurn,
            Countdown = Countdown,
            Keywords = Keywords,
            BarrierStacks = BarrierStacks,
            SummonedThisTurn = SummonedThisTurn,
            OnDeathSuppressed = OnDeathSuppressed,
        };
        foreach (var buff in Buffs) copy.Buffs.Add(buff.Clone());
        return copy;
    }

    public bool HasKeyword(Keyword k) => (Keywords & k) == k;
    public void AddKeyword(Keyword k) => Keywords |= k;
    public void RemoveKeyword(Keyword k) => Keywords &= ~k;
}
