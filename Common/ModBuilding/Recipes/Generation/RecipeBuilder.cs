using Microsoft.Xna.Framework.Graphics;
using PackBuilder.Common.ModBuilding.Recipes.Generation.Properties;
using ReLogic.Content;
using System.Collections.Generic;
using Newtonsoft.Json;
using PackBuilder.Common.BuilderInterface;
using PackBuilder.Common.BuilderInterface.Windows;
using PackBuilder.Common.BuilderInterface.Windows.EditorModals;
using Terraria.ModLoader;
using Terraria.UI;

namespace PackBuilder.Common.ModBuilding.Recipes.Generation;

public sealed class RecipeBuilder : PackBuilderType
{
    [JsonRequired]
    public RecipeResult Result { get; set; }
    public List<RecipeIngredient> Ingredients = [];
    public List<RecipeGroupIngredient> Groups = [];
    public List<string> Tiles = [];

    public override string? LoadingMethod => nameof(ModSystem.AddRecipes);

    public override Asset<Texture2D> GetIcon()
    {
        return ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/ModifierIcons/Recipes", AssetRequestMode.ImmediateLoad);
    }

    public override AbstractInterfaceWindow? CreateEditorModal(BaseModifierElement element, BuilderInterfaceState state)
    {
        return new RecipeBuilderModal(element, state);
    }

    public override void Load()
    {
        var recipe = NewRecipe(Mod);

        Result.AddTo(recipe);

        foreach (var ingredient in Ingredients)
            ingredient.AddTo(recipe);

        foreach (var group in Groups)
            group.AddTo(recipe);

        foreach (var tile in Tiles)
            recipe.AddTile(GetTile(tile));

        recipe.Register();
    }
}
