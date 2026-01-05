using System;
using System.Collections.Generic;
using System.Linq;
using PackBuilder.Common.JsonBuilding.DataStructures;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;

namespace PackBuilder.Common.JsonBuilding.Drops.Changes;

internal sealed record ModifyDrop(
    string Item
) : IDropChange
{
    private int ItemId => DropHelpers.ParseItemId(Item);

    public ValueModifier Amount { get; set; } = ValueModifier.NoOperation;

    public ValueModifier Chance { get; set; } = ValueModifier.NoOperation;

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

            /*
            if (rates.All(x => x.itemId == ItemId))
            {
                lootProvider.Remove(rule);
                continue;
            }
            */

            // Skip it if there's nothing we want...
            if (rates.All(x => x.itemId != ItemId))
            {
                continue;
            }

            var newRule = new ModifyItemDropRule(rule, ItemId, Amount, Chance);
            {
                lootProvider.Replace(rule, newRule);
            }
        }
    }
}

internal sealed class ModifyItemDropGuard(int itemId, ValueModifier amount, ValueModifier chance) : ItemDropGuard
{
    public bool DesiredItem { get; set; }

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
        DesiredItem = type == itemId;

        if (DesiredItem)
        {
            stack = (int)amount.Apply(stack);
        }

        return ItemDropGuardKind.Success;
    }
}

internal sealed class ModifyItemDropRule(IItemDropRule wrappedRule, int itemId, ValueModifier amount, ValueModifier chance) : WrappedItemDropRule(wrappedRule)
{
    public override void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo)
    {
        // base.ReportDroprates(drops, ratesInfo);

        var ownRates = DropHelpers.GetSelfDropInfo(WrappedRule);
        var chainedRates = DropHelpers.GetChainDropInfo(WrappedRule);

        var total = 0f;
        var targetSum = 0f;

        foreach (var drop in ownRates)
        {
            total += drop.dropRate;

            if (drop.itemId == itemId)
            {
                targetSum += drop.dropRate;
            }
        }

        if (targetSum <= 0f || total <= 0f)
        {
            drops.AddRange(ownRates);
            drops.AddRange(chainedRates);
            return;
        }

        var newTargetSum = 0f;

        for (int i = ownRates.Count - 1; i >= 0; i--)
        {
            var drop = ownRates[i];

            if (drop.itemId != itemId)
            {
                continue;
            }

            var newRate = chance.Apply(drop.dropRate);
            if (newRate <= 0f)
            {
                ownRates.RemoveAt(i);
                continue;
            }

            drop.dropRate = newRate;
            drop.stackMin = (int)amount.Apply(drop.stackMin);
            drop.stackMax = (int)amount.Apply(drop.stackMax);

            newTargetSum += drop.dropRate;
            ownRates[i] = drop;
        }

        if (ownRates.Count == 0)
        {
            drops.AddRange(chainedRates);
            return;
        }

        newTargetSum = MathF.Min(newTargetSum, total);

        var otherSum = total - targetSum;
        var newOtherSum = total - newTargetSum;

        if (otherSum <= 0f)
        {
            drops.AddRange(ownRates);
            drops.AddRange(chainedRates);
            return;
        }

        var scaleOthers = newOtherSum / otherSum;
        for (var i = 0; i < ownRates.Count; i++)
        {
            var drop = ownRates[i];

            if (drop.itemId != itemId)
            {
                drop.dropRate *= scaleOthers;
            }

            ownRates[i] = drop;
        }

        drops.AddRange(ownRates);
        drops.AddRange(chainedRates);
    }

    protected override ItemDropGuard CreateDropGuard()
    {
        return new ModifyItemDropGuard(itemId, amount, chance);
    }
}
