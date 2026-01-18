using Terraria.ModLoader;

namespace PackBuilder.Common.ModBuilding.Drops.Changes;

public interface IDropChange
{
    void ApplyTo(IIterableLoot loot);
}
