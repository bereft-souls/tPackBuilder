using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace PackBuilder.Common.JsonBuilding.Drops.Changes;

internal interface IIterableLoot
{
    int Count { get; }

    IItemDropRule this[int index] { get; }

    void Add(
        IItemDropRule entry
    );

    void Replace(
        IItemDropRule oldRule,
        IItemDropRule newRule
    );

    void Remove(
        IItemDropRule entry
    );

    void RemoveWhere(
        Predicate<IItemDropRule> predicate
    );
}

internal abstract class AbstractIterableListLoot : IIterableLoot
{
    public int Count => GetListReference().Count;

    public IItemDropRule this[int index] => GetListReference()[index];

    public void Add(
        IItemDropRule entry
    )
    {
        GetListReference().Add(entry);
    }

    public void Replace(
        IItemDropRule oldRule,
        IItemDropRule newRule
    )
    {
        var idx = GetListReference().IndexOf(oldRule);
        if (idx == -1)
        {
            return;
        }

        GetListReference()[idx] = newRule;
    }

    public void Remove(
        IItemDropRule entry
    )
    {
        GetListReference().Remove(entry);
    }

    public void RemoveWhere(
        Predicate<IItemDropRule> predicate
    )
    {
        GetListReference().RemoveAll(predicate);
    }

    protected abstract List<IItemDropRule> GetListReference();
}

internal sealed class IterableGlobalLoot(GlobalLoot loot) : AbstractIterableListLoot
{
    protected override List<IItemDropRule> GetListReference()
    {
        return loot.itemDropDatabase._globalEntries;
    }
}

internal sealed class IterableNpcLoot(NPCLoot loot) : AbstractIterableListLoot
{
    protected override List<IItemDropRule> GetListReference()
    {
        if (!loot.itemDropDatabase._entriesByNpcNetId.TryGetValue(loot.npcNetId, out var list))
        {
            loot.itemDropDatabase._entriesByNpcNetId[loot.npcNetId] = list = [];
        }

        return list;
    }
}

internal sealed class IterableItemLoot(ItemLoot loot) : AbstractIterableListLoot
{
    protected override List<IItemDropRule> GetListReference()
    {
        if (!loot.itemDropDatabase._entriesByItemId.TryGetValue(loot.itemType, out var list))
        {
            loot.itemDropDatabase._entriesByItemId[loot.itemType] = list = [];
        }

        return list;
    }
}

internal sealed class IterableListLoot(List<IItemDropRule> loot) : AbstractIterableListLoot
{
    protected override List<IItemDropRule> GetListReference()
    {
        return loot;
    }
}

internal sealed class ChainedRuleLootProvider(List<IItemDropRuleChainAttempt> chainedRules) : IIterableLoot
{
    private sealed class WrappedDropRuleChainAttempt : IItemDropRuleChainAttempt
    {
        // Private setter to allow stacking these wrapped instances.
        public IItemDropRule RuleToChain
        {
            get => wrapped.RuleToChain;

            // ReSharper disable once PropertyCanBeMadeInitOnly.Local
            private set
            {
                // TODO: Account for explicit interface implementations.
                if (wrapped.GetType().GetProperty(nameof(RuleToChain)) is { SetMethod: { } set })
                {
                    set.Invoke(wrapped, [value]);
                }
                else
                {
                    throw new InvalidOperationException($"Attempted to set RuleToChain property of IItemDropRuleChainAttempt that does not have a setter: {GetType().FullName}");
                }
            }
        }

        private readonly IItemDropRuleChainAttempt wrapped;

        public WrappedDropRuleChainAttempt(IItemDropRuleChainAttempt wrapped, IItemDropRule newRule)
        {
            this.wrapped = wrapped;
            RuleToChain = newRule;
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

    public int Count => chainedRules.Count;

    public IItemDropRule this[int index] => chainedRules[index].RuleToChain;

    public void Add(IItemDropRule entry)
    {
        throw new InvalidOperationException("Cannot directly add a drop rule to a chained rule loot provider; use Replace");
    }

    public void Replace(
        IItemDropRule oldRule,
        IItemDropRule newRule
    )
    {
        if (!reverseMap.TryGetValue(oldRule, out var attempt))
        {
            return;
        }

        var idx = chainedRules.IndexOf(attempt);
        if (idx == -1)
        {
            return;
        }

        var newAttempt = new WrappedDropRuleChainAttempt(attempt, newRule);
        {
            chainedRules[idx] = newAttempt;
            reverseMap[newRule] = newAttempt;
        }
    }

    public void Remove(IItemDropRule entry)
    {
        if (!reverseMap.Remove(entry, out var attempt))
        {
            return;
        }

        chainedRules.Remove(attempt);
    }

    public void RemoveWhere(Predicate<IItemDropRule> predicate)
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
