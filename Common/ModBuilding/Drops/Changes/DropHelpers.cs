using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.GameContent.ItemDropRules;

namespace PackBuilder.Common.ModBuilding.Drops.Changes;

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
