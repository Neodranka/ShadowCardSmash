using ShadowCardSmash.Domain;

namespace ShadowCardSmash.Cards;

/// <summary>
/// 场地牌 — 占据玩家独立的"场地槽位"（<see cref="PlayerState.TerrainSlot"/>，不在 6 格战场内）。
/// 同一时间每方最多 1 张场地牌。提供持续效果，通过 OnOwnerTurnStart / OnOwnerTurnEnd 等钩子实现。
/// </summary>
public abstract class TerrainCard : CardScript
{
    public sealed override CardType CardType => CardType.Terrain;
    public sealed override int BaseAttack => 0;
}
