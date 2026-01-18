using PackBuilder.Core.Systems;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace PackBuilder.Common.ModBuilding.Items
{
    public class ItemMod : PackBuilderType
    {
        public List<string> Items = [];

        public required string Item { set => Items.Add(value); }

        public required ItemChanges Changes { get; set; }

        public override void Load(Mod mod)
        {
            if (Items.Count == 0)
                throw new NoItemsException();

            // Get the item mod ready for factory initialization.
            foreach (string item in Items)
            {
                int itemType = GetItem(item);

                ItemModifier.ItemModSets.TryAdd(itemType, []);
                ItemModifier.ItemModSets[itemType].Add(Changes);
            }
        }
    }
}
