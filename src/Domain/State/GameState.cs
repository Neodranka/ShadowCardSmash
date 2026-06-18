namespace ShadowCardSmash.Domain;

public sealed class GameState
{
    public const int ManaMax = 10;

    public int TurnNumber;
    public PlayerSide CurrentPlayer;
    public GamePhase Phase = GamePhase.NotStarted;
    public GameResult Result = GameResult.InProgress;

    public PlayerState[] Players { get; } =
    {
        new() { Side = PlayerSide.First },
        new() { Side = PlayerSide.Second },
    };

    public MulliganState Mulligan = new();

    public int RandomSeed;
    public ulong RandomCounter;

    private int _nextInstanceId = 1;
    public InstanceId AllocateInstanceId() => new(_nextInstanceId++);

    public PlayerState GetPlayer(PlayerSide side) => Players[(int)side];
    public PlayerState GetCurrentPlayer() => Players[(int)CurrentPlayer];
    public PlayerState GetOpponentOf(PlayerSide side) => Players[(int)side.Opponent()];

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
            _nextInstanceId = _nextInstanceId,
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
