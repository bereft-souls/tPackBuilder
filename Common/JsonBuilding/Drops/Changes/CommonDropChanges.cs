using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace PackBuilder.Common.JsonBuilding.Drops.Changes;

internal sealed record AddDrop(
    List<Condition> Conditions,
    int ItemId
) : IDropChange
{
    public void ApplyTo(ILoot loot) { }
}

internal sealed record RemoveDrop(
    int ItemId
) : IDropChange
{
    public void ApplyTo(ILoot loot) { }
}

internal sealed record ModifyDrop(
) : IDropChange
{
    public void ApplyTo(ILoot loot) { }
}
