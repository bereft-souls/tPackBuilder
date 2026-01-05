using Terraria.ModLoader;

namespace PackBuilder.Common.JsonBuilding.Drops.Changes;

internal interface IDropChange
{
    void ApplyTo(IIterableLoot loot);
}
