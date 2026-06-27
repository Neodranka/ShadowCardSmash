namespace ShadowCardSmash.Domain;

public sealed class GameState
{
    public const int ManaMax = 10;

    public int TurnNumber;
    public PlayerSide CurrentPlayer;
    public GamePhase Phase = GamePhase.NotStarted;
    public GameResult Result = GameResult.InProgress;

    // Public field (not auto-property) so System.Text.Json can replace contents on Deserialize.
    public PlayerState[] Players =
    {
        new() { Side = PlayerSide.First },
        new() { Side = PlayerSide.Second },
    };

    public MulliganState Mulligan = new();

    public int RandomSeed;
    public ulong RandomCounter;

    /// <summary>Monotonic seed for AllocateInstanceId. Public so JSON serialization can round-trip it.</summary>
    public int NextInstanceIdSeed = 1;
    public InstanceId AllocateInstanceId() => new(NextInstanceIdSeed++);

    public PlayerState GetPlayer(PlayerSide side) => Players[(int)side];
    public PlayerState GetCurrentPlayer() => Players[(int)CurrentPlayer];
    public PlayerState GetOpponentOf(PlayerSide side) => Players[(int)side.Opponent()];

    /// <summary>
    /// Hidden-info filtered copy from <paramref name="viewer"/>'s perspective. Opponent's hand and
    /// deck contents have their CardId replaced with <see cref="CardId.Hidden"/>; counts and
    /// InstanceIds are preserved so UI can still render face-down stacks and track moves.
    /// Public zones (field, graveyard, vanished) are NOT touched — these are always visible.
    /// V1 1v1: assumes exactly two seats and that "opponent" = the other side.
    /// </summary>
    public GameState FilterFor(PlayerSide viewer)
    {
        var copy = Snapshot();
        var opp = copy.GetPlayer(viewer.Opponent());
        for (int i = 0; i < opp.Hand.Count; i++)
        {
            opp.Hand[i] = new RuntimeCard
            {
                Instance = opp.Hand[i].Instance,
                Card = CardId.Hidden,
                Owner = opp.Side,
                Zone = Zone.Hand,
            };
        }
        for (int i = 0; i < opp.Deck.Count; i++)
        {
            opp.Deck[i] = new RuntimeCard
            {
                Instance = opp.Deck[i].Instance,
                Card = CardId.Hidden,
                Owner = opp.Side,
                Zone = Zone.Deck,
            };
        }
        return copy;
    }

    /// <summary>
    /// Full deep copy used by GameLoop to snapshot before each external Action
    /// (enables rollback on validation failure and desync hashing).
    /// </summary>
    public GameState Snapshot()
    {
        var copy = new GameState
        {
            TurnNumber = TurnNumber,
            CurrentPlayer = CurrentPlayer,
            Phase = Phase,
            Result = Result,
            RandomSeed = RandomSeed,
            RandomCounter = RandomCounter,
            NextInstanceIdSeed = NextInstanceIdSeed,
            Mulligan = Mulligan.Clone(),
        };
        for (int i = 0; i < Players.Length; i++)
        {
            var cloned = Players[i].Clone();
            cloned.Side = Players[i].Side;
            copy.Players[i] = cloned;
        }
        return copy;
    }
}
