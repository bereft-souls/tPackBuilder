using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace PackBuilder.Common.JsonBuilding.Drops.Changes;

public sealed record AddDrop(
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

    public void ApplyTo(IIterableLoot loot)
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
