using Godot;
using ShadowCardSmash.Engine;

namespace ShadowCardSmash.Cards.Resources;

/// <summary>
/// Bridges the Godot-free CardRegistry to the .tres pile under <c>res://resources/cards/</c>.
/// Called once at startup after CardRegistry.ScanAssembly. Cards whose .tres is missing fall back to
/// whatever overrides the C# class provides (or default placeholders).
/// </summary>
public static class CardResourceLoader
{
    public const string CardResourceDir = "res://resources/cards/";

    public readonly record struct Result(int Attached, int Missing);

    public static Result AttachAll(CardRegistry registry)
    {
        int attached = 0;
        int missing = 0;
        foreach (var script in registry.All())
        {
            var path = $"{CardResourceDir}{script.Id.Value}.tres";
            if (!ResourceLoader.Exists(path))
            {
                missing++;
                continue;
            }
            var resource = GD.Load<CardDataResource>(path);
            if (resource is not null && script is CardScript cs)
            {
                cs.AttachData(resource);
                attached++;
            }
            else
            {
                missing++;
            }
        }
        return new Result(attached, missing);
    }
}
