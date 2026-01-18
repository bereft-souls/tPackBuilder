using Terraria;

namespace PackBuilder.Common.ModBuilding.Items.Changes
{
    public interface IItemChange
    {
        public void ApplyTo(Item item);
    }
}
