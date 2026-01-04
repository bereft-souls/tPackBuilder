using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace PackBuilder.Common.JsonBuilding.Drops.Changes;

internal static class DropHelpers
{
    public static int ParseItemId(string itemId)
    {
        return GetItem(itemId);
    }

    public static (int min, int max) ParseAmount(string amount)
    {
        if (int.TryParse(amount, out var numAmount))
        {
            return (numAmount, numAmount);
        }

        var parts = amount.Split('-', 2);

        var min = 0;
        var max = 1;

        if (int.TryParse(parts[0], out var numMin))
        {
            min = numMin;
        }

        if (int.TryParse(parts[1], out var numMax))
        {
            max = numMax;
        }

        if (min < 0)
        {
            min = 0;
        }

        if (max < 1)
        {
            max = 1;
        }

        if (min > max)
        {
            min = max;
        }

        return (min, max);
    }

    public static float ParseChance(string chance)
    {
        return float.TryParse(chance, out var numChance)
            ? Math.Clamp(numChance, 0f, 1f)
            : 1f;
    }

    public static List<DropRateInfo> GetAllDropInfo(IItemDropRule dropRule)
    {
        var drops = new List<DropRateInfo>();
        {
            dropRule.ReportDroprates(drops, new DropRateInfoChainFeed(1f));
        }

        return drops;
    }

    public static List<DropRateInfo> GetSelfDropInfo(IItemDropRule dropRule)
    {
        var chainedRules = dropRule.ChainedRules.ToList();
        {
            dropRule.ChainedRules.Clear();
        }

        try
        {
            var drops = new List<DropRateInfo>();
            {
                dropRule.ReportDroprates(drops, new DropRateInfoChainFeed(1f));
            }

            return drops;
        }
        finally
        {
            dropRule.ChainedRules.AddRange(chainedRules);
        }
    }

    public static List<DropRateInfo> GetChainDropInfo(IItemDropRule dropRule)
    {
        var drops = new List<DropRateInfo>();
        {
            Chains.ReportDroprates(dropRule.ChainedRules, 1f, drops, new DropRateInfoChainFeed(1f));
        }

        return drops;
    }
}

internal sealed class ListLootProvider(List<IItemDropRule> loot) : ILoot
{
    public List<IItemDropRule> Get(bool includeGlobalDrops = true)
    {
        return loot.ToList();
    }

    public IItemDropRule Add(IItemDropRule entry)
    {
        loot.Add(entry);
        return entry;
    }

    public IItemDropRule Remove(IItemDropRule entry)
    {
        loot.Remove(entry);
        return entry;
    }

    public void RemoveWhere(Predicate<IItemDropRule> predicate, bool includeGlobalDrops = true)
    {
        loot.RemoveAll(predicate);
    }
}

internal sealed class ChainedRuleLootProvider(List<IItemDropRuleChainAttempt> chainedRules) : ILoot
{
    private sealed class WrappedDropRuleChainAttempt : IItemDropRuleChainAttempt
    {
        public IItemDropRule RuleToChain { get; }

        private readonly IItemDropRuleChainAttempt wrapped;

        public WrappedDropRuleChainAttempt(IItemDropRuleChainAttempt wrapped, IItemDropRule newRule)
        {
            this.wrapped = wrapped;
            RuleToChain = newRule;

            // TODO: Account for explicit interface implementations.
            if (wrapped.GetType().GetProperty(nameof(RuleToChain)) is { SetMethod: { } set })
            {
                set.Invoke(wrapped, [newRule]);
            }
        }

        public bool CanChainIntoRule(ItemDropAttemptResult parentResult)
        {
            return wrapped.CanChainIntoRule(parentResult);
        }

        public void ReportDroprates(
            float personalDropRate,
            List<DropRateInfo> drops,
            DropRateInfoChainFeed ratesInfo
        )
        {
            wrapped.ReportDroprates(personalDropRate, drops, ratesInfo);
        }
    }

    private readonly Dictionary<IItemDropRule, IItemDropRuleChainAttempt> reverseMap =
        chainedRules.ToDictionary(x => x.RuleToChain, x => x);

    public List<IItemDropRule> Get(bool includeGlobalDrops = true)
    {
        return reverseMap.Keys.ToList();
    }

    public IItemDropRule Add(IItemDropRule entry)
    {
        throw new InvalidOperationException("Cannot add a drop rule to a wrapped collection of rule chain attempts");
    }

    public IItemDropRule Replace(IItemDropRule oldRule, IItemDropRule newRule)
    {
        if (!reverseMap.TryGetValue(oldRule, out var attempt))
        {
            return newRule;
        }

        Remove(oldRule);

        var newAttempt = new WrappedDropRuleChainAttempt(attempt, newRule);
        {
            chainedRules.Add(newAttempt);
            reverseMap[newRule] = newAttempt;
        }

        return newRule;
    }

    public IItemDropRule Remove(IItemDropRule entry)
    {
        if (!reverseMap.Remove(entry, out var attempt))
        {
            return entry;
        }

        chainedRules.Remove(attempt);
        return entry;
    }

    public void RemoveWhere(Predicate<IItemDropRule> predicate, bool includeGlobalDrops = true)
    {
        var entriesToRemove = reverseMap.Keys.Where(x => predicate(x)).ToArray();
        if (entriesToRemove.Length == 0)
        {
            return;
        }

        foreach (var entry in entriesToRemove)
        {
            Remove(entry);
        }
    }
}

internal sealed class RemovedItemDropGuard(int itemId) : ItemDropGuard
{
    public override ItemDropGuardKind AllowItemDrop(
        IEntitySource source,
        int x,
        int y,
        int width,
        int height,
        Item? itemToClone,
        int type,
        int stack,
        bool noBroadcast,
        int prefix,
        bool noGrabDelay,
        bool reverseLookup
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

    protected override ItemDropGuard CreateDropGuard()
    {
        return new RemovedItemDropGuard(removedItem);
    }
}

internal sealed record AddDrop(
    string Item
) : IDropChange
{
    private sealed class MoreCommonDrop(
        int itemId,
        float dropChance,
        int minItems,
        int maxItems
    ) : IItemDropRule
    {
        public List<IItemDropRuleChainAttempt> ChainedRules { get; } = [];

        public bool CanDrop(DropAttemptInfo info)
        {
            return true;
        }

        public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo)
        {
            var relativeChance = dropChance * ratesInfo.parentDroprateChance;
            {
                drops.Add(new DropRateInfo(itemId, minItems, maxItems, relativeChance, ratesInfo.conditions));
            }

            Chains.ReportDroprates(ChainedRules, relativeChance, drops, ratesInfo);
        }

        public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info)
        {
            if (!(info.player.RollLuck(1000000) / 1000000f < dropChance))
            {
                return new ItemDropAttemptResult
                {
                    State = ItemDropAttemptResultState.FailedRandomRoll,
                };
            }

            CommonCode.DropItem(info, itemId, info.rng.Next(minItems, maxItems + 1));
            return new ItemDropAttemptResult
            {
                State = ItemDropAttemptResultState.Success,
            };
        }
    }

    private int ItemId => DropHelpers.ParseItemId(Item);

    public List<string> Conditions { get; set; } = [];

    public string Amount { get; set; } = "1";

    public string Chance { get; set; } = "1.00";

    public void ApplyTo(ILoot loot)
    {
        var (min, max) = DropHelpers.ParseAmount(Amount);

        // TODO: Add support for conditions... sigh
        loot.Add(
            new MoreCommonDrop(
                ItemId,
                DropHelpers.ParseChance(Chance),
                min,
                max
            )
        );
    }
}

internal sealed record RemoveDrop(
    string Item
) : IDropChange
{
    private int ItemId => DropHelpers.ParseItemId(Item);

    public void ApplyTo(ILoot loot)
    {
        RecursivelyModifyPartialDropRules(loot);
    }

    private void RecursivelyModifyPartialDropRules(ILoot lootProvider)
    {
        foreach (var rule in lootProvider.Get())
        {
            RecursivelyModifyPartialDropRules(new ChainedRuleLootProvider(rule.ChainedRules));

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
            if (lootProvider is ChainedRuleLootProvider chainedRuleLootProvider)
            {
                chainedRuleLootProvider.Replace(rule, newRule);
            }
            else
            {
                lootProvider.Remove(rule);
                lootProvider.Add(new RemoveItemDropRule(rule, ItemId));
            }
        }
    }
}

internal sealed record ModifyDrop(
) : IDropChange
{
    public void ApplyTo(ILoot loot) { }
}
