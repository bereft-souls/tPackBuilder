using PackBuilder.Common.ModBuilding.Items;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace PackBuilder.Core.Systems
{
    public class ItemModifier : ModSystem
    {
        public static Dictionary<int, List<ItemChanges>> ItemModSets { get; } = [];

        [Autoload(false)]
        [LateLoad]
        internal class PackBuilderItem : GlobalItem
        {
            public override void SetDefaults(Item entity) => ApplyChanges(entity);

            public static void ApplyChanges(Item item)
            {
                if (ItemModSets.TryGetValue(item.type, out var value))
                    value.ForEach(c => c.ApplyTo(item));
            }
        }
    }
}
