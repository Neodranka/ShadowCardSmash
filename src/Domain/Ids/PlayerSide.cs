namespace ShadowCardSmash.Domain;

public enum PlayerSide
{
    First = 0,
    Second = 1,
}

public static class PlayerSideExtensions
{
    public static PlayerSide Opponent(this PlayerSide side) => side == PlayerSide.First ? PlayerSide.Second : PlayerSide.First;
    public static int Index(this PlayerSide side) => (int)side;
}
