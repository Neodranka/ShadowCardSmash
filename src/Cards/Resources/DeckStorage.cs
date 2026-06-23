using Godot;
using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Cards.Resources;

/// <summary>
/// Read/write deck files under <c>user://decks/</c>. Paths are sanitized so weird deck names cannot escape the dir.
/// </summary>
public static class DeckStorage
{
    public const string DeckDir = "user://decks/";

    public static void EnsureDir()
    {
        if (!DirAccess.DirExistsAbsolute(DeckDir))
            DirAccess.MakeDirRecursiveAbsolute(DeckDir);
    }

    public static string MakePath(string deckName)
    {
        var safe = Sanitize(deckName);
        return $"{DeckDir}{safe}.tres";
    }

    public static Error Save(DeckResource deck)
    {
        EnsureDir();
        deck.LastModifiedTicks = System.DateTime.UtcNow.Ticks;
        return ResourceSaver.Save(deck, MakePath(deck.DeckName));
    }

    public static DeckResource? Load(string deckName)
    {
        var path = MakePath(deckName);
        if (!ResourceLoader.Exists(path)) return null;
        return GD.Load<DeckResource>(path);
    }

    public static List<string> ListDeckNames()
    {
        EnsureDir();
        var names = new List<string>();
        var dir = DirAccess.Open(DeckDir);
        if (dir is null) return names;
        dir.ListDirBegin();
        for (string file = dir.GetNext(); file != ""; file = dir.GetNext())
        {
            if (dir.CurrentIsDir()) continue;
            if (!file.EndsWith(".tres")) continue;
            names.Add(file[..^5]);
        }
        dir.ListDirEnd();
        return names;
    }

    private static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "untitled";
        var sb = new System.Text.StringBuilder();
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c >= 0x4E00) sb.Append(c);
        }
        var s = sb.ToString();
        return s.Length == 0 ? "untitled" : s;
    }
}
