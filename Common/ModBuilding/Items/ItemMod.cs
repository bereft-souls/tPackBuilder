using PackBuilder.Core.Systems;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace PackBuilder.Common.ModBuilding.Items;

public class ItemMod : PackBuilderType
{
    public List<string> Items = [];

    public List<IItemChange> Changes = [];

    public override void Load(Mod mod)
    {
        if (Items.Count == 0)
            throw new NoItemsException();

        // Get the item mod ready for factory initialization.
        foreach (string item in Items)
        {
            int itemType = GetItem(item);
            ItemModifier.RegisterItemChanges(itemType, Changes);
        }
    }

    /// <summary>
    /// Call this to manually register an <see cref="ItemMod"/>.
    /// </summary>
    public void Register() => Load(null!);
}
