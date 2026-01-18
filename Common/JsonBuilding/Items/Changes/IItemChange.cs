using Terraria;

namespace PackBuilder.Common.JsonBuilding.Items.Changes
{
    public interface IItemChange
    {
        public void ApplyTo(Item item);
    }
}
