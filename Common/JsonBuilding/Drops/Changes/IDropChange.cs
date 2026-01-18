using Terraria.ModLoader;

namespace PackBuilder.Common.JsonBuilding.Drops.Changes;

public interface IDropChange
{
    void ApplyTo(IIterableLoot loot);
}
