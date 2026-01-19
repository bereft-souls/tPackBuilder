using PackBuilder.Common.ModBuilding.Items;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace PackBuilder.Core.Systems
{
    public class ItemModifier : ModSystem
    {
        public static Dictionary<int, List<IItemChange>> ItemMods { get; } = [];

        public static void RegisterItemChanges(int itemType, params IEnumerable<IItemChange> changes)
        {
            ItemMods.TryAdd(itemType, []);
            ItemMods[itemType].AddRange(changes);
        }

        [Autoload(false)]
        [LateLoad]
        internal class PackBuilderItem : GlobalItem
        {
            public override void SetDefaults(Item entity) => ApplyChanges(entity);

            public static void ApplyChanges(Item item)
            {
                if (ItemMods.TryGetValue(item.type, out var value))
                    value.ForEach(c => c.ApplyTo(item));
            }
        }
    }
}
