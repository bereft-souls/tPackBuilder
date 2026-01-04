using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace PackBuilder.Common.JsonBuilding.Drops.Changes;

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
        return type != itemId ? ItemDropGuardKind.Reroll : ItemDropGuardKind.Success;
    }
}

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

internal abstract class WrappedItemDropRule(IItemDropRule wrappedRule) : IItemDropRule
{
    public virtual List<IItemDropRuleChainAttempt> ChainedRules => wrappedRule.ChainedRules;

    public virtual bool CanDrop(DropAttemptInfo info)
    {
        return wrappedRule.CanDrop(info);
    }

    public virtual void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo)
    {
        wrappedRule.ReportDroprates(drops, ratesInfo);
    }

    public virtual ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info)
    {
        return wrappedRule.TryDroppingItem(info);
    }
}

internal sealed class RemoveItemDropRule(IItemDropRule wrappedRule, int removedItem) : WrappedItemDropRule(wrappedRule)
{
    public override void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo)
    {
        base.ReportDroprates(drops, ratesInfo);

        var total = drops.Sum(x => x.dropRate);

        var removed = 0f;
        for (var i = 0; i < drops.Count; i++)
        {
            var drop = drops[i];
            if (drop.itemId != removedItem)
            {
                continue;
            }

            removed += drop.dropRate;
            drops.RemoveAt(i);
        }

        var newTotal = total - removed;
        if (newTotal <= 0f)
        {
            // TODO: !?
            drops.Clear();
            return;
        }

        var scale = total / newTotal;
        for (var i = 0; i < drops.Count; i++)
        {
            var drop = drops[i];
            {
                drop.dropRate *= scale;
            }
            drops[i] = drop;
        }
    }

    public override ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info)
    {
        var retVal = new ItemDropAttemptResult
        {
            State = ItemDropAttemptResultState.DidNotRunCode,
        };

        var guard = new RemovedItemDropGuard(removedItem);
        using (guard.Scope())
        {
            while (guard.UpdateAndContinue())
            {
                retVal = base.TryDroppingItem(info);
            }

            if (guard.Failed)
            {
                retVal = new ItemDropAttemptResult
                {
                    State = ItemDropAttemptResultState.DoesntFillConditions,
                };
            }
        }

        return retVal;
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

        var modifiedRules = loot.Get().Where(
            rule =>
            {
                var rates = DropHelpers.GetDropInfo(rule);

                return rates.Any(x => x.itemId == ItemId);
            }
        );

        foreach (var modifiedRule in modifiedRules)
        {
            loot.Remove(modifiedRule);
            loot.Add(new RemoveItemDropRule(modifiedRule, ItemId));
        }
    }
}

internal sealed record ModifyDrop(
) : IDropChange
{
    public void ApplyTo(ILoot loot) { }
}
