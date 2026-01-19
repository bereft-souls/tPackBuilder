using PackBuilder.Common.ModBuilding.Drops.Changes;

namespace PackBuilder.Common.ModBuilding.Drops;

public interface IDropChange
{
    void ApplyTo(IIterableLoot loot);
}
