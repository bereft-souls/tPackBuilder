using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using PackBuilder.Common.BuilderInterface;
using PackBuilder.Common.BuilderInterface.Windows;
using PackBuilder.Common.BuilderInterface.Windows.EditorModals;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

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
        return new RecipeGroupModModal(element, state);
    }

    public override void Load()
    {
        if (AddItems.Count == 0 && RemoveItems.Count == 0)
            throw new NoGroupChangesException();

        foreach (var groupId in Groups.Select(GetRecipeGroup))
        {
            var recipeGroup = RecipeGroup.recipeGroups[groupId];

            foreach (var item in AddItems.Select(GetItem))
            {
                recipeGroup.ValidItems.Add(item);
                recipeGroup.ValidItemsLookup?[item] = true;
            }

            foreach (var item in RemoveItems.Select(GetItem))
            {
                recipeGroup.ValidItems.Remove(item);
                recipeGroup.ValidItemsLookup?[item] = false;
            }
        }
    }
}
