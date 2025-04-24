using System.Collections.Generic;

namespace PackBuilder.Common.JsonBuilding.Items
{
    internal class ItemMod
    {
        public List<string> Items = [];

        public required string Item { set => Items.Add(value); }

        public required ItemChanges Changes { get; set; }
    }
}
