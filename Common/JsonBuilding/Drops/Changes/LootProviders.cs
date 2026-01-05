using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace PackBuilder.Common.JsonBuilding.Drops.Changes;

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
