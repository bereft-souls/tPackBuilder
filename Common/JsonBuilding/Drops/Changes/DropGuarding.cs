using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace PackBuilder.Common.JsonBuilding.Drops.Changes;

internal enum ItemDropGuardKind
{
    // Allow the item to spawn.
    Success,

    // Deny the item spawning and force parent code to re-run.
    Reroll,

    // Cancel the item spawning at all.
    Cancel,
}

internal abstract class ItemDropGuard
{
    public bool CanRun { get; set; } = true;

    public bool Failed { get; set; }

    public int Tries { get; set; }

    public bool? DidEvaluate { get; set; }

    public bool UpdateAndContinue()
    {
        if (DidEvaluate.HasValue)
        {
            if (!DidEvaluate.Value)
            {
                CanRun = false;
                Failed = true;
                return false;
            }

            Tries++;
        }

        if (Tries >= 1000)
        {
            CanRun = false;
            Failed = true;
            return false;
        }

        DidEvaluate = false;
        return CanRun;
    }

    public abstract ItemDropGuardKind ModifyItemDrop(
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
    );
}

internal sealed class ItemDropStackGuard : IDisposable
{
    public ItemDropGuard Guard { get; }

    public ItemDropStackGuard(ItemDropGuard guard)
    {
        Guard = guard;

        ItemDropGuardSystem.GuardStack.Push(Guard);
    }

    public void Dispose()
    {
        var poppedGuard = ItemDropGuardSystem.GuardStack.Pop();
        {
            Debug.Assert(poppedGuard == Guard);
        }
    }
}

internal abstract class WrappedItemDropRule(IItemDropRule wrappedRule) : IItemDropRule
{
    public virtual List<IItemDropRuleChainAttempt> ChainedRules => WrappedRule.ChainedRules;

    protected IItemDropRule WrappedRule { get; } = wrappedRule;

    public virtual bool CanDrop(DropAttemptInfo info)
    {
        return WrappedRule.CanDrop(info);
    }

    public virtual void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo)
    {
        WrappedRule.ReportDroprates(drops, ratesInfo);
    }

    public virtual ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info)
    {
        var retVal = new ItemDropAttemptResult
        {
            State = ItemDropAttemptResultState.Success,
        };

        var guard = CreateDropGuard(info);
        using (guard.Scope())
        {
            while (retVal.State == ItemDropAttemptResultState.Success && guard.UpdateAndContinue())
            {
                if (WrappedRule is INestedItemDropRule nested)
                {
                    retVal = nested.TryDroppingItem(info, ResolveRule);
                }
                else
                {
                    retVal = WrappedRule.TryDroppingItem(info);
                }
            }

            if (guard.Failed)
            {
                retVal = new ItemDropAttemptResult
                {
                    State = ItemDropAttemptResultState.DoesntFillConditions,
                };
            }

            /*
            if (guard.DidEvaluate.HasValue && !guard.DidEvaluate.Value)
            {
                retVal = new ItemDropAttemptResult
                {
                    State = ItemDropAttemptResultState.Success,
                };
            }
            else if (guard.Failed)
            {
                retVal = new ItemDropAttemptResult
                {
                    State = ItemDropAttemptResultState.DoesntFillConditions,
                };
            }
            */
        }

        return retVal;
    }

    protected abstract ItemDropGuard CreateDropGuard(DropAttemptInfo info);

    // Reimplementation of Main.ItemDropSolver that's more keen to respect our
    // guard value.
    private static ItemDropAttemptResult ResolveRule(
        IItemDropRule rule,
        DropAttemptInfo info
    )
    {
        if (!rule.CanDrop(info))
        {
            var result = new ItemDropAttemptResult
            {
                State = ItemDropAttemptResultState.DoesntFillConditions,
            };

            Main.ItemDropSolver.ResolveRuleChains(rule, info, result);
            return result;
        }
        else
        {
            var result = rule is INestedItemDropRule nestedRule ? nestedRule.TryDroppingItem(info, ResolveRule) : rule.TryDroppingItem(info);
            if (ItemDropGuardSystem.GuardStack.TryPeek(out var guard) && guard.CanRun)
            {
                // TODO: Better state?
                result.State = ItemDropAttemptResultState.DoesntFillConditions;
            }

            Main.ItemDropSolver.ResolveRuleChains(rule, info, result);
            return result;
        }
    }
}

internal sealed class ItemDropGuardSystem : ModSystem
{
    public static Stack<ItemDropGuard> GuardStack { get; } = [];

    public override void Load()
    {
        base.Load();

        On_Item.NewItem_Inner += NewItem_Inner_ApplyRestrictions;
    }

    private static int NewItem_Inner_ApplyRestrictions(
        On_Item.orig_NewItem_Inner orig,
        IEntitySource source,
        int x,
        int y,
        int width,
        int height,
        Item? itemToClone,
        int type,
        int stack,
        bool noBroadcast,
        int pfix,
        bool noGrabDelay,
        bool reverseLookup
    )
    {
        if (GuardStack.Count == 0)
        {
            return orig(
                source,
                x,
                y,
                width,
                height,
                itemToClone,
                type,
                stack,
                noBroadcast,
                pfix,
                noGrabDelay,
                reverseLookup
            );
        }

        foreach (var guard in GuardStack)
        {
            if (!guard.CanRun)
            {
                continue;
            }

            guard.DidEvaluate = true;
            var result = guard.ModifyItemDrop(
                ref source,
                ref x,
                ref y,
                ref width,
                ref height,
                ref itemToClone,
                ref type,
                ref stack,
                ref noBroadcast,
                ref pfix,
                ref noGrabDelay,
                ref reverseLookup
            );

            switch (result)
            {
                case ItemDropGuardKind.Success:
                    guard.CanRun = false;
                    continue;

                case ItemDropGuardKind.Reroll:
                    guard.CanRun = true;
                    return Main.maxItems;

                case ItemDropGuardKind.Cancel:
                    guard.CanRun = false;
                    return Main.maxItems;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return orig(
            source,
            x,
            y,
            width,
            height,
            itemToClone,
            type,
            stack,
            noBroadcast,
            pfix,
            noGrabDelay,
            reverseLookup
        );
    }
}

internal static class ItemDropGuardExtensions
{
    public static ItemDropStackGuard Scope(this ItemDropGuard guard)
    {
        return new ItemDropStackGuard(guard);
    }
}
