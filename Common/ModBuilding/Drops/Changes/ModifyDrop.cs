using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.Utilities;

namespace PackBuilder.Common.ModBuilding.Drops.Changes;

internal readonly record struct ModifiedChance(
    float Original,
    float New
);

public sealed record ModifyDrop(
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

internal sealed class ModifyItemDropGuard : ItemDropGuard
{
    private enum RollKind
    {
        // Default behavior, accept the item if it comes.
        RollOnce,

        // Unfavorable behavior; if the item is rolled, roll a chance to keep
        // the item or discard it.
        RollLessThanOnce,

        // Favorable behavior; if the item is not rolled, keep requesting a
        // reroll to try and force it out.
        RollMoreThanOnce,
    }

    private readonly int itemId;
    private readonly ValueModifier amount;
    private readonly UnifiedRandom rng;

    private readonly RollKind rollKind;
    private float rollValue;

    private bool failedRoll = false;

    public ModifyItemDropGuard(
        int itemId,
        ValueModifier amount,
        ModifiedChance? chance,
        UnifiedRandom rng
    )
    {
        this.itemId = itemId;
        this.amount = amount;
        this.rng = rng;

        if (!chance.HasValue)
        {
            rollKind = RollKind.RollOnce;
            rollValue = 0f;
            return;
        }

        var rollsContinuous = MathF.Log(1f - chance.Value.New) / MathF.Log(1f - chance.Value.Original);
        if (Math.Abs(rollsContinuous - 1f) < 0.01f)
        {
            rollKind = RollKind.RollOnce;
            rollValue = 0f;
            return;
        }

        if (rollsContinuous < 1f)
        {
            rollKind = RollKind.RollLessThanOnce;
            rollValue = rollsContinuous;
        }
        else
        {
            rollKind = RollKind.RollMoreThanOnce;
            rollValue = rollsContinuous - 1f;
        }
    }

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
        var desiredItem = type == itemId;
        if (desiredItem && failedRoll)
        {
            return ItemDropGuardKind.Reroll;
        }

        if (desiredItem)
        {
            stack = (int)amount.Apply(stack);
        }

        switch (rollKind)
        {
            case RollKind.RollOnce:
                return ItemDropGuardKind.Success;

            case RollKind.RollLessThanOnce:
                {
                    if (!desiredItem || rng.NextFloat() < rollValue)
                    {
                        return ItemDropGuardKind.Success;
                    }

                    failedRoll = true;
                    return ItemDropGuardKind.Reroll;
                }

            case RollKind.RollMoreThanOnce:
                {
                    if (desiredItem)
                    {
                        return ItemDropGuardKind.Success;
                    }

                    if (rollValue > 1f)
                    {
                        rollValue -= 1f;
                        return ItemDropGuardKind.Reroll;
                    }

                    var remainingRoll = rollValue;
                    rollValue = 0f;

                    if (rng.NextFloat() < remainingRoll)
                    {
                        return ItemDropGuardKind.Reroll;
                    }

                    failedRoll = true;
                    return ItemDropGuardKind.Success;
                }

            default:
                throw new ArgumentException();
        }
    }
}

internal sealed class ModifyItemDropRule(
    IItemDropRule wrappedRule,
    int itemId,
    ValueModifier amount,
    ValueModifier chance
) : WrappedItemDropRule(wrappedRule)
{
    private ModifiedChance? CalculatedChance { get; set; }

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

        for (var i = ownRates.Count - 1; i >= 0; i--)
        {
            var drop = ownRates[i];

            if (drop.itemId != itemId)
            {
                continue;
            }

            var newRate = chance.Apply(drop.dropRate);

            // TODO: How to account for multiple of the same item in a single
            //       rule?  Probably doesn't matter too much..?
            CalculatedChance = new ModifiedChance(drop.dropRate, newRate);

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

        // Filter out 0% drops that may be generated if a drop mod makes a drop
        // a guaranteed chance.
        drops.AddRange(ownRates.Where(x => x.dropRate > 0f));
        drops.AddRange(chainedRates);
    }

    protected override ItemDropGuard CreateDropGuard(DropAttemptInfo info)
    {
        return new ModifyItemDropGuard(itemId, amount, CalculatedChance, info.rng);
    }
}
