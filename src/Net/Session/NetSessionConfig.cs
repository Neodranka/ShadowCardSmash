namespace ShadowCardSmash.Net;

/// <summary>
/// Cross-version coupling constants for the network layer. Bumping <see cref="ProtocolVersion"/> means
/// older builds can no longer connect to this one — used by the handshake to fail fast.
///
/// Bump whenever:
///   • Adding/removing/changing any NetMessage subtype
///   • Adding/removing/changing any IGameAction or BoardEvent subtype
///   • Changing the GameState JSON shape (added/removed field on Domain types)
///   • Any wire-format breaking change
/// </summary>
public static class NetSessionConfig
{
    // V2 (Phase 4a): added ActionRequest / ActionApplied / ActionRejected.
    // V3 (Phase 4d): added StartGame for initial state push from host to client.
    public const int ProtocolVersion = 3;
    public const int DefaultPort = 49321;
    public const int MaxPlayers = 2;        // V1: strictly 1v1; lobby slots are hardcoded to host + 1 client.
}
