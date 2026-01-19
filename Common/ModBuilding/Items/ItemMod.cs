using PackBuilder.Core.Systems;
using System.Collections.Generic;

namespace PackBuilder.Common.ModBuilding.Items;

public sealed class ItemMod : PackBuilderType
{
    public List<string> Items = [];

    public List<IItemChange> Changes = [];

    public override void Load()
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
}
