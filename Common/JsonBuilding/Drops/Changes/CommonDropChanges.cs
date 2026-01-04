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
    public static int ProcessItemId(string itemId)
    {
        return GetItem(itemId);
    }

    public static List<DropRateInfo> GetDropInfo(IItemDropRule dropRule)
    {
        var drops = new List<DropRateInfo>();
        {
            dropRule.ReportDroprates(drops, new DropRateInfoChainFeed(1f));
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

        var ownRates = new List<DropRateInfo>();
        var chainedRates = new List<DropRateInfo>();

        var chainedRules = WrappedRule.ChainedRules.ToList();
        try
        {
            WrappedRule.ChainedRules.Clear();
            WrappedRule.ReportDroprates(ownRates, ratesInfo);
        }
        finally
        {
            WrappedRule.ChainedRules.AddRange(chainedRules);
        }

        Chains.ReportDroprates(chainedRules, 1f, chainedRates, ratesInfo);

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
    List<Condition> Conditions,
    string Item
) : IDropChange
{
    public void ApplyTo(ILoot loot) { }
}

internal sealed record RemoveDrop(
    string Item
) : IDropChange
{
    private int ItemId => DropHelpers.ProcessItemId(Item);

    public void ApplyTo(ILoot loot)
    {
        // Remove every rule that solely produces the drop we want to remove.
        loot.RemoveWhere(
            rule =>
            {
                var rates = DropHelpers.GetDropInfo(rule);

                return rates.All(x => x.itemId == ItemId);
            }
        );

        RecursivelyModifyPartialDropRules(loot);
    }

    private void RecursivelyModifyPartialDropRules(ILoot lootProvider)
    {
        foreach (var rule in lootProvider.Get())
        {
            RecursivelyModifyPartialDropRules(new ChainedRuleLootProvider(rule.ChainedRules));

            var rates = DropHelpers.GetDropInfo(rule);
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
