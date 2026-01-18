using PackBuilder.Common.ModBuilding.Items.Changes;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace PackBuilder.Common.ModBuilding.Items
{
    public class ItemChanges
    {
        public List<IItemChange> Changes = [];

        public VanillaItemChange Terraria { set => Changes.Add(value); }

        public CalamityItemChange CalamityMod
        {
            set
            {
                if (ModLoader.HasMod("CalamityMod"))
                    Changes.Add(value);
            }
        }

        public void ApplyTo(Item item)
        {
            foreach (var change in Changes)
                change.ApplyTo(item);
        }
    }
}
