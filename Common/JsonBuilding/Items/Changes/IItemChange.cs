using Terraria;

namespace PackBuilder.Common.JsonBuilding.Items.Changes
{
    internal interface IItemChange
    {
        public void ApplyTo(Item item);
    }
}
