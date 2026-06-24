namespace ShadowCardSmash.Engine;

/// <summary>
/// One option in a card's "choose N from M" mechanic. The card script declares M choices via <c>CardScript.Choices</c>
/// and <c>ChoicesToPick</c>; the player picks N at play time via a popup, and OnPlay reads ctx.PickedChoices.
/// </summary>
public sealed record CardChoice(
    string Title,
    string Description = "",
    TargetSpec TargetForChoice = TargetSpec.None,
    int TargetsForChoice = 1
);
