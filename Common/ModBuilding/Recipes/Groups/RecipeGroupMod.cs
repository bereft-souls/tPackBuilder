using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using PackBuilder.Common.BuilderInterface;
using PackBuilder.Common.BuilderInterface.Windows;
using PackBuilder.Common.BuilderInterface.Windows.EditorModals;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace PackBuilder.Common.ModBuilding.Recipes.Groups;

public sealed class RecipeGroupMod : PackBuilderType
{
    public List<string> Groups = [];
    public List<string> AddItems = [];
    public List<string> RemoveItems = [];

    public override string? LoadingMethod => nameof(ModSystem.PostSetupRecipes);

    public override Asset<Texture2D> GetIcon()
    {
        return ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/ModifierIcons/Recipes", AssetRequestMode.ImmediateLoad);
    }

    public override AbstractInterfaceWindow? CreateEditorModal(BaseModifierElement element, BuilderInterfaceState state)
    {
        return new RecipeGroupModEditorWindow(element, state);
    }

    public override void Load()
    {
        if (AddItems.Count == 0 && RemoveItems.Count == 0)
            throw new NoGroupChangesException();

        foreach (var groupId in Groups.Select(GetRecipeGroup))
        {
            var recipeGroup = RecipeGroup.recipeGroups[groupId];

            foreach (int itemType in AddItems.Select(item => GetItem(item, Mod)))
            {
                if (itemType == ItemID.None)
                    continue;

                recipeGroup.ValidItems.Add(itemType);
                recipeGroup.ValidItemsLookup?[itemType] = true;
            }

            foreach (int itemType in RemoveItems.Select(item => GetItem(item, Mod)))
            {
                if (itemType == ItemID.None)
                    continue;

                recipeGroup.ValidItems.Remove(itemType);
                recipeGroup.ValidItemsLookup?[itemType] = false;
            }
        }
    }
}
