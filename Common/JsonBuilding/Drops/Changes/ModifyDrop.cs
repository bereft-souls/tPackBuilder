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
        if (type == itemId)
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
        var deltaTotal = 0f;

        for (var i = 0; i < ownRates.Count; i++)
        {
            var drop = ownRates[i];
            {
                total += drop.dropRate;
            }

            if (drop.itemId == itemId)
            {
                targetSum += drop.dropRate;

                var rawNew = chance.Apply(drop.dropRate);
                var newRate = MathF.Max(0f, rawNew);
                {
                    deltaTotal += newRate - drop.dropRate;
                }
            }
        }

        // ?! How did we get here
        if (targetSum <= 0f)
        {
            drops.AddRange(ownRates);
            drops.AddRange(chainedRates);
            return;
        }

        var newTargetSum = targetSum + deltaTotal;
        var otherSum = total - targetSum;
        var newOtherSum = total - newTargetSum;

        if (otherSum <= 0f || newOtherSum < 0f)
        {
            // TODO: !?
            ownRates.Clear();
            return;
        }

        var scaleOthers = newOtherSum / otherSum;
        for (var i = 0; i < ownRates.Count; i++)
        {
            var drop = ownRates[i];

            if (drop.itemId == itemId)
            {
                var rawNew = chance.Apply(drop.dropRate);
                {
                    drop.dropRate = MathF.Max(0f, rawNew);
                }

                drop.stackMin = (int)amount.Apply(drop.stackMin);
                drop.stackMax = (int)amount.Apply(drop.stackMax);
            }
            else
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
