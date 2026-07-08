using System;
using System.Text.Json;
using Godot;
using ShadowCardSmash.Domain;

namespace ShadowCardSmash.App;

/// <summary>
/// Client-side persistence of the active net session so the player can close and reopen the app
/// and reconnect to the still-running host. Saved on StartGame (session is committed), cleared on
/// game end / forfeit / reject / voluntary back-to-menu.
///
/// V1 does NOT persist the host's authoritative GameState — if the host process dies the session
/// is gone. Client restart only.
///
/// File: user://active_session.json
///   • Windows: %APPDATA%\Godot\app_userdata\ShadowCardSmash\active_session.json
///   • macOS/Linux: XDG equivalent.
/// </summary>
public sealed class PersistedNetSession
{
    public Guid Token { get; set; }
    public string HostAddress { get; set; } = "";
    public int HostPort { get; set; }
    public PlayerSide AssignedSide { get; set; }

    private const string FilePath = "user://active_session.json";

    public static void Save(PersistedNetSession session)
    {
        try
        {
            var json = JsonSerializer.Serialize(session);
            using var f = Godot.FileAccess.Open(FilePath, Godot.FileAccess.ModeFlags.Write);
            if (f is null)
            {
                GD.PrintErr($"[PersistedNetSession] Save failed to open {FilePath}: {Godot.FileAccess.GetOpenError()}");
                return;
            }
            f.StoreString(json);
            GD.Print($"[PersistedNetSession] Saved (token={session.Token.ToString().Substring(0, 8)}..., host={session.HostAddress}:{session.HostPort}, side={session.AssignedSide})");
        }
        catch (Exception e)
        {
            GD.PrintErr($"[PersistedNetSession] Save exception: {e}");
        }
    }

    public static PersistedNetSession? Load()
    {
        if (!Godot.FileAccess.FileExists(FilePath)) return null;
        try
        {
            using var f = Godot.FileAccess.Open(FilePath, Godot.FileAccess.ModeFlags.Read);
            if (f is null) return null;
            var json = f.GetAsText();
            var loaded = JsonSerializer.Deserialize<PersistedNetSession>(json);
            if (loaded is null || string.IsNullOrEmpty(loaded.HostAddress)) return null;
            return loaded;
        }
        catch (Exception e)
        {
            GD.PrintErr($"[PersistedNetSession] Load exception: {e}");
            return null;
        }
    }

    public static void Clear()
    {
        if (!Godot.FileAccess.FileExists(FilePath)) return;
        try
        {
            var abs = ProjectSettings.GlobalizePath(FilePath);
            if (System.IO.File.Exists(abs))
            {
                System.IO.File.Delete(abs);
                GD.Print($"[PersistedNetSession] Cleared {FilePath}");
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"[PersistedNetSession] Clear exception: {e}");
        }
    }

    public static bool Exists() => Godot.FileAccess.FileExists(FilePath);
}
