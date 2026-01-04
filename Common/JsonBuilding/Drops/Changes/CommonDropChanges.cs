using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;

namespace PackBuilder.Common.JsonBuilding.Drops.Changes;

internal sealed record AddDrop(
    List<Condition> Conditions,
    int ItemId
) : IDropChange
{
    public void Apply(List<IItemDropRule> dropRules)
    {
        throw new System.NotImplementedException();
    }
}

internal sealed record RemoveDrop(
    int ItemId
) : IDropChange
{
    public void Apply(List<IItemDropRule> dropRules)
    {
        
    }
}

internal sealed record ModifyDrop(
) : IDropChange
{
    public void Apply(List<IItemDropRule> dropRules)
    {
        throw new System.NotImplementedException();
    }
}
