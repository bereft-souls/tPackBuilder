using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;

namespace PackBuilder.Common.JsonBuilding.Drops.Changes;

internal interface IDropChange
{
    void Apply(List<IItemDropRule> dropRules);
}
