using ShadowCardSmash.Cards.Resources;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Engine;
using ShadowCardSmash.Net.Transport;

namespace ShadowCardSmash.App;

public enum BattleMode { Hotseat, NetHost, NetClient }

/// <summary>
/// Static state passed across scene transitions (Godot has no built-in DI container).
/// Set by DeckSelection / MultiplayerLobby; read by BattleController.
/// </summary>
public static class BattleSetup
{
    public static DeckResource? Player1Deck;
    public static DeckResource? Player2Deck;
    public static int Seed;

    /// <summary>Which path BattleController should run on _Ready.</summary>
    public static BattleMode Mode = BattleMode.Hotseat;

    // ---- Net-only handoff state ----
    /// <summary>Active ENet transport (parented to /root so it survives lobby→battle scene change).
    /// BattleController takes ownership and disposes on game end.</summary>
    public static EnetTransport? NetTransport;

    /// <summary>The side this peer plays. PlayerSide.First for host, .Second for client. Hotseat ignores.</summary>
    public static PlayerSide NetLocalSide = PlayerSide.First;

    /// <summary>Client-only: seed state received from host's StartGame message. Host ignores.</summary>
    public static GameState? NetInitialState;

    /// <summary>Host-only: live GameLoop built in lobby (already past mulligan). BattleController takes ownership.</summary>
    public static GameLoop? PendingHostLoop;

    /// <summary>Clear all net handoff fields after BattleController consumes them.</summary>
    public static void ClearNetHandoff()
    {
        NetTransport = null;
        NetInitialState = null;
        PendingHostLoop = null;
        Mode = BattleMode.Hotseat;
    }
}
