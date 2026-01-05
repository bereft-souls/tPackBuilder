using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;

namespace PackBuilder.Common.JsonBuilding.Drops.Changes;

internal sealed record RemoveDrop(
    string Item
) : IDropChange
{
    private int ItemId => DropHelpers.ParseItemId(Item);

    public void ApplyTo(IIterableLoot loot)
    {
        RecursivelyModifyPartialDropRules(loot);
    }

    private void RecursivelyModifyPartialDropRules(IIterableLoot lootProvider)
    {
        for (int i = lootProvider.Count - 1; i >= 0; i--)
        {
            RecursivelyModifyPartialDropRules(new ChainedRuleLootProvider(lootProvider[i].ChainedRules));

            var rule = lootProvider[i];

            var rates = DropHelpers.GetSelfDropInfo(rule);
            if (rates.Count == 0)
            {
                continue;
            }

            // Remove the entire rule if it solely contains the item we want.
            // TODO: Do we *always* want to remove the rule when there are
            //       unchecked chained rules..?
            if (rates.All(x => x.itemId == ItemId))
            {
                lootProvider.Remove(rule);
                continue;
            }

            // Skip it if there's nothing we want...
            if (rates.All(x => x.itemId != ItemId))
            {
                continue;
            }

            var newRule = new RemoveItemDropRule(rule, ItemId);
            {
                lootProvider.Replace(rule, newRule);
            }
        }
    }
}

internal sealed class RemovedItemDropGuard(int itemId) : ItemDropGuard
{
    public override ItemDropGuardKind ModifyItemDrop(
        ref IEntitySource source,
        ref int x,
        ref int y,
        ref int width,
        ref int height,
        ref Item? itemToClone,
        ref int type,
        ref int stack,
        ref bool noBroadcast,
        ref int prefix,
        ref bool noGrabDelay,
        ref bool reverseLookup
    )
    {
        return type == itemId ? ItemDropGuardKind.Reroll : ItemDropGuardKind.Success;
    }
}

internal sealed class RemoveItemDropRule(IItemDropRule wrappedRule, int removedItem) : WrappedItemDropRule(wrappedRule)
{
    public override void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo)
    {
        // base.ReportDroprates(drops, ratesInfo);

        var ownRates = DropHelpers.GetSelfDropInfo(WrappedRule);
        var chainedRates = DropHelpers.GetChainDropInfo(WrappedRule);

        var total = ownRates.Sum(x => x.dropRate);

        var removed = 0f;
        for (var i = 0; i < ownRates.Count; i++)
        {
            var drop = ownRates[i];
            if (drop.itemId != removedItem)
            {
                continue;
            }

            removed += drop.dropRate;
            ownRates.RemoveAt(i);
        }

        var newTotal = total - removed;
        if (newTotal <= 0f)
        {
            // TODO: !?
            ownRates.Clear();
            return;
        }

        var scale = total / newTotal;
        for (var i = 0; i < ownRates.Count; i++)
        {
            var drop = ownRates[i];
            {
                drop.dropRate *= scale;
            }
            ownRates[i] = drop;
        }

        drops.AddRange(ownRates);
        drops.AddRange(chainedRates);
    }

    protected override ItemDropGuard CreateDropGuard(DropAttemptInfo info)
    {
        return new RemovedItemDropGuard(removedItem);
    }
}
