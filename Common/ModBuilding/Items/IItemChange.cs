using Terraria;

namespace PackBuilder.Common.ModBuilding.Items;

public interface IItemChange
{
    public void ApplyTo(Item item);
}
