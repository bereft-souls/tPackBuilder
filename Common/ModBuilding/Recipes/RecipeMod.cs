using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using PackBuilder.Common.BuilderInterface;
using PackBuilder.Common.BuilderInterface.Windows;
using PackBuilder.Common.ModBuilding.Recipes.Changes;
using PackBuilder.Common.ModBuilding.Recipes.Conditions;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PackBuilder.Common.ModBuilding.Recipes;

public sealed class RecipeMod : PackBuilderType
{
    // Either All or Any.
    // If "All" is specified, ALL of the conditions will need to be met in order to activate the changes of this mod.
    // If "Any" is specified, ANY of the conditions being met will activate the changes of this mod.
    public RecipeCriteria Criteria { get; set; } = RecipeCriteria.All;

    // The condition(s) needing to be met in order for this mod to activate.
    public List<IRecipeCondition> Conditions = [];

    // The change(s) that will be applied to each of the recipes where conditions are met.
    public List<IRecipeChange> Changes = [];

    // Run in PostAddRecipes instead of PostSetupContent
    public override string? LoadingMethod => nameof(ModSystem.PostAddRecipes);

    public override Asset<Texture2D> GetIcon()
    {
        return ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/ModifierIcons/Recipes", AssetRequestMode.ImmediateLoad);
    }

    public override AbstractInterfaceWindow? CreateEditorWindow(BaseModifierElement element, BuilderInterfaceState state)
    {
        return new RecipeModEditorWindow(element, state);
    }

    public override void Load()
    {
        if (Conditions.Count == 0)
            throw new NoConditionsException();

        var recipeLoader_CurrentMod = typeof(RecipeLoader).GetProperty("CurrentMod", BindingFlags.Static | BindingFlags.NonPublic)!;

        try
        {
            recipeLoader_CurrentMod.SetValue(null, Mod);

            foreach (var recipe in Main.recipe)
            {
                // Do not apply recipe mods to recipes added by the same mod pack.
                if (recipe.Mod == Mod)
                    continue;

                // 'applies' will be true in any of the following cases:
                //      - There are no specified conditions.
                //      - The specified criteria is "all" and ALL specified conditions are met.
                //      - The specified criteria is "any" and ANY single specified condition is met.
                bool applies = Criteria == RecipeCriteria.Any ?
                    Conditions.Any(c => c.AppliesTo(recipe)) :
                    Conditions.All(c => c.AppliesTo(recipe));

                // If this mod does not apply to a given recipe, move to the next.
                if (!applies)
                    continue;

                // Apply this recipe mod's changes.
                if (Changes.Count == 0)
                    throw new NoChangesException();

                foreach (var change in Changes)
                    change.ApplyTo(recipe);
            }
        }
        catch (Exception ex)
        {
            ex.Data["mod"] = Mod.Name;
            throw;
        }
        finally
        {
            recipeLoader_CurrentMod.SetValue(null, null);
        }
    }
}

// Recipe Criteria: Either All or Any.
// If "All" is specified, ALL of the conditions will need to be met in order to activate the changes of this mod.
// If "Any" is specified, ANY of the conditions being met will activate the changes of this mod.
[JsonConverter(typeof(RecipeCriteriaConverter))]
public enum RecipeCriteria
{
    All,
    Any
}

internal sealed class RecipeCriteriaConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is RecipeCriteria criteria)
            writer.WriteValue(Enum.GetName(typeof(RecipeCriteria), criteria));
    }

    public override object? ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer
    )
    {
        var recipeCriteria = typeof(RecipeCriteria);
        var options = Enum.GetValues(recipeCriteria);
        var value = reader.Value?.ToString() ?? Enum.GetName(recipeCriteria, 0);

        foreach (var option in options)
        {
            if (Enum.GetName(recipeCriteria, option)!.Equals(value, StringComparison.OrdinalIgnoreCase))
                return option;
        }

        return null;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(RecipeCriteria);
    }
}

internal sealed class RecipeModEditorWindow : AbstractEditorWindow<RecipeMod, RecipeModEditorWindow.RecipeModElement>
{
    public sealed class RecipeModElement : ModifierEditorElement<RecipeMod>
    {
        private readonly RecipeCriteriaWrapper criteriaButton;

        public RecipeModElement() : base("Recipe Mod", @static: true)
        {
            criteriaButton = new RecipeCriteriaWrapper("Criteria Requirement", false);
            {
                criteriaButton.Width.Set(0f, 0.5f);
                criteriaButton.Left.Set(0f, 0f);
                // criteriaButton.Top.Set(16f, 0f);
                criteriaButton.VAlign = 1f;
            }
            Append(criteriaButton);
        }

        public override RecipeMod CreateObject()
        {
            return new RecipeMod
            {
                Criteria = criteriaButton.AsCriteria(),
            };
        }

        public override void Populate(RecipeMod obj)
        {
            criteriaButton.SetAsCriteria(obj.Criteria);
        }
    }

    // Conditions
    private sealed class CreatesResultElement : ModifierEditorElement<CreatesResult>, IVisitor<RecipeMod>
    {
        private readonly ItemTypeSelector itemInput;
        private readonly InputField amountInput; // optional

        public CreatesResultElement() : base("Condition: Creates Result")
        {
            itemInput = new ItemTypeSelector();
            {
                itemInput.Left.Set(0f, 0f);
                // itemInput.VAlign = 1f;
                itemInput.Width.Set(140f, 0f);
                itemInput.Height.Set(24f, 0f);
                itemInput.Top.Set(-itemInput.Height.Pixels, 1f);
            }
            Append(itemInput);
            MakeAndAppendLabel(itemInput, "Item");

            amountInput = new InputField("1");
            {
                amountInput.TextScale = 0.66f;
                amountInput.Left.Set(itemInput.Width.Pixels + 2f, 0f);
                // amountInput.VAlign = 1f;
                amountInput.Width.Set(90f, 0f);
                amountInput.Height.Set(24f, 0f);
                amountInput.Top.Set(-amountInput.Height.Pixels, 1f);
            }
            Append(amountInput);
            MakeAndAppendLabel(amountInput, "Amount");
        }

        public override CreatesResult CreateObject()
        {
            return new CreatesResult
            {
                Item = itemInput.Entity,
                Count = int.TryParse(amountInput.Text, out int count) ? count : -1,
            };
        }

        public override void Populate(CreatesResult obj)
        {
            itemInput.Text.Text = itemInput.Entity = obj.Item;
            amountInput.Text = string.IsNullOrEmpty(obj.Count.ToString()) ? "-1" : obj.Count.ToString();
        }

        public void Visit(RecipeMod obj)
        {
            obj.Conditions.Add(CreateObject());
        }
    }

    private sealed class RequiresIngredientElement : ModifierEditorElement<RequiresIngredient>, IVisitor<RecipeMod>
    {
        private readonly ItemTypeSelector itemInput;
        private readonly InputField amountInput; // optional

        public RequiresIngredientElement() : base("Condition: Requires Ingredient")
        {
            itemInput = new ItemTypeSelector();
            {
                itemInput.Left.Set(0f, 0f);
                // itemInput.VAlign = 1f;
                itemInput.Width.Set(140f, 0f);
                itemInput.Height.Set(24f, 0f);
                itemInput.Top.Set(-itemInput.Height.Pixels, 1f);
            }
            Append(itemInput);
            MakeAndAppendLabel(itemInput, "Item");

            amountInput = new InputField("-1");
            {
                amountInput.TextScale = 0.66f;
                amountInput.Left.Set(itemInput.Width.Pixels + 2f, 0f);
                // amountInput.VAlign = 1f;
                amountInput.Width.Set(90f, 0f);
                amountInput.Height.Set(24f, 0f);
                amountInput.Top.Set(-amountInput.Height.Pixels, 1f);
            }
            Append(amountInput);
            MakeAndAppendLabel(amountInput, "Amount");
        }

        public override RequiresIngredient CreateObject()
        {
            return new RequiresIngredient
            {
                Item = itemInput.Entity,
                Count = int.TryParse(amountInput.Text, out int count) ? count : -1,
            };
        }

        public override void Populate(RequiresIngredient obj)
        {
            itemInput.Text.Text = itemInput.Entity = obj.Item;
            amountInput.Text = string.IsNullOrEmpty(obj.Count.ToString()) ? "-1" : obj.Count.ToString();
        }

        public void Visit(RecipeMod obj)
        {
            obj.Conditions.Add(CreateObject());
        }
    }

    private sealed class RequiresRecipeGroupElement : ModifierEditorElement<RequiresRecipeGroup>, IVisitor<RecipeMod>
    {
        private readonly RecipeGroupSelector itemInput;
        private readonly InputField amountInput; // optional

        public RequiresRecipeGroupElement() : base("Condition: Requires Group")
        {
            itemInput = new RecipeGroupSelector();
            {
                itemInput.Left.Set(0f, 0f);
                // itemInput.VAlign = 1f;
                itemInput.Width.Set(140f, 0f);
                itemInput.Height.Set(24f, 0f);
                itemInput.Top.Set(-itemInput.Height.Pixels, 1f);
            }
            Append(itemInput);
            MakeAndAppendLabel(itemInput, "Group");

            amountInput = new InputField("-1");
            {
                amountInput.TextScale = 0.66f;
                amountInput.Left.Set(itemInput.Width.Pixels + 2f, 0f);
                // amountInput.VAlign = 1f;
                amountInput.Width.Set(90f, 0f);
                amountInput.Height.Set(24f, 0f);
                amountInput.Top.Set(-amountInput.Height.Pixels, 1f);
            }
            Append(amountInput);
            MakeAndAppendLabel(amountInput, "Amount");
        }

        public override RequiresRecipeGroup CreateObject()
        {
            return new RequiresRecipeGroup
            {
                Group = itemInput.Entity,
                Count = int.TryParse(amountInput.Text, out int count) ? count : -1,
            };
        }

        public override void Populate(RequiresRecipeGroup obj)
        {
            itemInput.Text.Text = itemInput.Entity = obj.Group;
            amountInput.Text = string.IsNullOrEmpty(obj.Count.ToString()) ? "-1" : obj.Count.ToString();
        }

        public void Visit(RecipeMod obj)
        {
            obj.Conditions.Add(CreateObject());
        }
    }

    private sealed class RequiresTileElement : ModifierEditorElement<RequiresTile>, IVisitor<RecipeMod>
    {
        private readonly TileTypeSelector tileInput;

        public RequiresTileElement() : base("Condition: Requires Tile")
        {
            tileInput = new TileTypeSelector();
            {
                tileInput.Left.Set(0f, 0f);
                // tileInput.VAlign = 1f;
                tileInput.Width.Set(140f, 0f);
                tileInput.Height.Set(24f, 0f);
                tileInput.Top.Set(-tileInput.Height.Pixels, 1f);
            }
            Append(tileInput);
            MakeAndAppendLabel(tileInput, "Tile");
        }

        public override RequiresTile CreateObject()
        {
            return new RequiresTile
            {
                Tile = tileInput.Entity,
            };
        }

        public override void Populate(RequiresTile obj)
        {
            tileInput.Text.Text = tileInput.Entity = obj.Tile;
        }

        public void Visit(RecipeMod obj)
        {
            obj.Conditions.Add(CreateObject());
        }
    }

    // Changes
    private sealed class AddIngredientElement : ModifierEditorElement<AddIngredient>, IVisitor<RecipeMod>
    {
        private readonly ItemTypeSelector itemInput;
        private readonly InputField amountInput;

        public AddIngredientElement() : base("Change: Add Ingredient")
        {
            itemInput = new ItemTypeSelector();
            {
                itemInput.Left.Set(0f, 0f);
                // itemInput.VAlign = 1f;
                itemInput.Width.Set(140f, 0f);
                itemInput.Height.Set(24f, 0f);
                itemInput.Top.Set(-itemInput.Height.Pixels, 1f);
            }
            Append(itemInput);
            MakeAndAppendLabel(itemInput, "Item");

            amountInput = new InputField("1");
            {
                amountInput.TextScale = 0.66f;
                amountInput.Left.Set(itemInput.Width.Pixels + 2f, 0f);
                // amountInput.VAlign = 1f;
                amountInput.Width.Set(90f, 0f);
                amountInput.Height.Set(24f, 0f);
                amountInput.Top.Set(-amountInput.Height.Pixels, 1f);
            }
            Append(amountInput);
            MakeAndAppendLabel(amountInput, "Amount");
        }

        public override AddIngredient CreateObject()
        {
            return new AddIngredient
            {
                Item = itemInput.Entity,
                Count = int.TryParse(amountInput.Text, out int count) ? count : -1,
            };
        }

        public override void Populate(AddIngredient obj)
        {
            itemInput.Text.Text = itemInput.Entity = obj.Item;
            amountInput.Text = string.IsNullOrEmpty(obj.Count.ToString()) ? "1" : obj.Count.ToString();
        }

        public void Visit(RecipeMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    private sealed class AddRecipeGroupElement : ModifierEditorElement<AddRecipeGroup>, IVisitor<RecipeMod>
    {
        private readonly RecipeGroupSelector itemInput;
        private readonly InputField amountInput;

        public AddRecipeGroupElement() : base("Change: Add Group")
        {
            itemInput = new RecipeGroupSelector();
            {
                itemInput.Left.Set(0f, 0f);
                // itemInput.VAlign = 1f;
                itemInput.Width.Set(140f, 0f);
                itemInput.Height.Set(24f, 0f);
                itemInput.Top.Set(-itemInput.Height.Pixels, 1f);
            }
            Append(itemInput);
            MakeAndAppendLabel(itemInput, "Group");

            amountInput = new InputField("1");
            {
                amountInput.TextScale = 0.66f;
                amountInput.Left.Set(itemInput.Width.Pixels + 2f, 0f);
                // amountInput.VAlign = 1f;
                amountInput.Width.Set(90f, 0f);
                amountInput.Height.Set(24f, 0f);
                amountInput.Top.Set(-amountInput.Height.Pixels, 1f);
            }
            Append(amountInput);
            MakeAndAppendLabel(amountInput, "Amount");
        }

        public override AddRecipeGroup CreateObject()
        {
            return new AddRecipeGroup
            {
                Group = itemInput.Entity,
                Count = int.TryParse(amountInput.Text, out int count) ? count : -1,
            };
        }

        public override void Populate(AddRecipeGroup obj)
        {
            itemInput.Text.Text = itemInput.Entity = obj.Group;
            amountInput.Text = string.IsNullOrEmpty(obj.Count.ToString()) ? "1" : obj.Count.ToString();
        }

        public void Visit(RecipeMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    private sealed class AddTileElement : ModifierEditorElement<AddTile>, IVisitor<RecipeMod>
    {
        private readonly TileTypeSelector tileInput;

        public AddTileElement() : base("Change: Add Tile")
        {
            tileInput = new TileTypeSelector();
            {
                tileInput.Left.Set(0f, 0f);
                // tileInput.VAlign = 1f;
                tileInput.Width.Set(140f, 0f);
                tileInput.Height.Set(24f, 0f);
                tileInput.Top.Set(-tileInput.Height.Pixels, 1f);
            }
            Append(tileInput);
            MakeAndAppendLabel(tileInput, "Tile");
        }

        public override AddTile CreateObject()
        {
            return new AddTile
            {
                Tile = tileInput.Entity,
            };
        }

        public override void Populate(AddTile obj)
        {
            tileInput.Text.Text = tileInput.Entity = obj.Tile;
        }

        public void Visit(RecipeMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    private sealed class ChangeIngredientElement : ModifierEditorElement<ChangeIngredient>, IVisitor<RecipeMod>
    {
        private readonly ItemTypeSelector itemInput;
        private readonly ItemTypeSelector newItemInput; // optional
        private readonly InputField newAmountInput;     // optional

        public ChangeIngredientElement() : base("Change: Modify Ingredient")
        {
            itemInput = new ItemTypeSelector();
            {
                itemInput.Left.Set(0f, 0f);
                // itemInput.VAlign = 1f;
                itemInput.Width.Set(140f, 0f);
                itemInput.Height.Set(24f, 0f);
                itemInput.Top.Set(-itemInput.Height.Pixels, 1f);
            }
            Append(itemInput);
            MakeAndAppendLabel(itemInput, "Old Item");

            newItemInput = new ItemTypeSelector();
            {
                newItemInput.Left.Set(itemInput.Width.Pixels + 2f, 0f);
                // itemInput.VAlign = 1f;
                newItemInput.Width.Set(140f, 0f);
                newItemInput.Height.Set(24f, 0f);
                newItemInput.Top.Set(-itemInput.Height.Pixels, 1f);
            }
            Append(newItemInput);
            MakeAndAppendLabel(newItemInput, "New Item");

            newAmountInput = new InputField("1");
            {
                newAmountInput.TextScale = 0.66f;
                newAmountInput.Left.Set(newItemInput.Left.Pixels + newItemInput.Width.Pixels + 2f, 0f);
                // amountInput.VAlign = 1f;
                newAmountInput.Width.Set(90f, 0f);
                newAmountInput.Height.Set(24f, 0f);
                newAmountInput.Top.Set(-newAmountInput.Height.Pixels, 1f);
            }
            Append(newAmountInput);
            MakeAndAppendLabel(newAmountInput, "Amount");
        }

        public override ChangeIngredient CreateObject()
        {
            return new ChangeIngredient
            {
                Item = itemInput.Entity,
                NewItem = (string.IsNullOrEmpty(newItemInput.Entity) || newItemInput.Entity == "None") ? null : newItemInput.Entity,
                NewCount = int.TryParse(newAmountInput.Text, out int count) ? count : -1,
            };
        }

        public override void Populate(ChangeIngredient obj)
        {
            itemInput.Text.Text = itemInput.Entity = obj.Item;
            newItemInput.Text.Text = newItemInput.Entity = obj.NewItem ?? "";
            newAmountInput.Text = string.IsNullOrEmpty(obj.NewCount.ToString()) ? "-1" : obj.NewCount.ToString();
        }

        public void Visit(RecipeMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    private sealed class ChangeRecipeGroupElement : ModifierEditorElement<ChangeRecipeGroup>, IVisitor<RecipeMod>
    {
        private readonly RecipeGroupSelector itemInput;
        private readonly RecipeGroupSelector newItemInput; // optional
        private readonly InputField newAmountInput;        // optional

        public ChangeRecipeGroupElement() : base("Change: Modify Group")
        {
            itemInput = new RecipeGroupSelector();
            {
                itemInput.Left.Set(0f, 0f);
                // itemInput.VAlign = 1f;
                itemInput.Width.Set(140f, 0f);
                itemInput.Height.Set(24f, 0f);
                itemInput.Top.Set(-itemInput.Height.Pixels, 1f);
            }
            Append(itemInput);
            MakeAndAppendLabel(itemInput, "Old Group");

            newItemInput = new RecipeGroupSelector();
            {
                newItemInput.Left.Set(itemInput.Width.Pixels + 2f, 0f);
                // itemInput.VAlign = 1f;
                newItemInput.Width.Set(140f, 0f);
                newItemInput.Height.Set(24f, 0f);
                newItemInput.Top.Set(-itemInput.Height.Pixels, 1f);
            }
            Append(newItemInput);
            MakeAndAppendLabel(newItemInput, "New Group");

            newAmountInput = new InputField("1");
            {
                newAmountInput.TextScale = 0.66f;
                newAmountInput.Left.Set(newItemInput.Left.Pixels + newItemInput.Width.Pixels + 2f, 0f);
                // amountInput.VAlign = 1f;
                newAmountInput.Width.Set(90f, 0f);
                newAmountInput.Height.Set(24f, 0f);
                newAmountInput.Top.Set(-newAmountInput.Height.Pixels, 1f);
            }
            Append(newAmountInput);
            MakeAndAppendLabel(newAmountInput, "Amount");
        }

        public override ChangeRecipeGroup CreateObject()
        {
            return new ChangeRecipeGroup
            {
                Group = itemInput.Entity,
                NewGroup = (string.IsNullOrEmpty(newItemInput.Entity) || newItemInput.Entity == "None") ? null : newItemInput.Entity,
                NewCount = int.TryParse(newAmountInput.Text, out int count) ? count : 1,
            };
        }

        public override void Populate(ChangeRecipeGroup obj)
        {
            itemInput.Text.Text = itemInput.Entity = obj.Group;
            newItemInput.Text.Text = newItemInput.Entity = obj.NewGroup ?? "";
            newAmountInput.Text = string.IsNullOrEmpty(obj.NewCount.ToString()) ? "-1" : obj.NewCount.ToString();
        }

        public void Visit(RecipeMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    private sealed class ChangeResultElement : ModifierEditorElement<ChangeResult>, IVisitor<RecipeMod>
    {
        private readonly ItemTypeSelector newItemInput; // optional
        private readonly InputField newAmountInput;     // optional

        public ChangeResultElement() : base("Change: Modify Result")
        {
            newItemInput = new ItemTypeSelector();
            {
                newItemInput.Left.Set(0f, 0f);
                // itemInput.VAlign = 1f;
                newItemInput.Width.Set(140f, 0f);
                newItemInput.Height.Set(24f, 0f);
                newItemInput.Top.Set(-newItemInput.Height.Pixels, 1f);
            }
            Append(newItemInput);
            MakeAndAppendLabel(newItemInput, "Item");

            newAmountInput = new InputField("-1");
            {
                newAmountInput.TextScale = 0.66f;
                newAmountInput.Left.Set(newItemInput.Width.Pixels + 2f, 0f);
                // amountInput.VAlign = 1f;
                newAmountInput.Width.Set(90f, 0f);
                newAmountInput.Height.Set(24f, 0f);
                newAmountInput.Top.Set(-newAmountInput.Height.Pixels, 1f);
            }
            Append(newAmountInput);
            MakeAndAppendLabel(newAmountInput, "Amount");
        }

        public override ChangeResult CreateObject()
        {
            return new ChangeResult
            {
                Item = (string.IsNullOrEmpty(newItemInput.Entity) || newItemInput.Entity == "None") ? null : newItemInput.Entity,
                Count = int.TryParse(newAmountInput.Text, out int count) ? count : -1,
            };
        }

        public override void Populate(ChangeResult obj)
        {
            newItemInput.Text.Text = newItemInput.Entity = obj.Item ?? "";
            newAmountInput.Text = string.IsNullOrEmpty(obj.Count.ToString()) ? "-1" : obj.Count.ToString();
        }

        public void Visit(RecipeMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    private sealed class ChangeTileElement : ModifierEditorElement<ChangeTile>, IVisitor<RecipeMod>
    {
        private readonly TileTypeSelector tileInput; // optional
        private readonly TileTypeSelector newTileInput;

        public ChangeTileElement() : base("Change: Modify Tile")
        {
            tileInput = new TileTypeSelector();
            {
                tileInput.Left.Set(0f, 0f);
                // itemInput.VAlign = 1f;
                tileInput.Width.Set(140f, 0f);
                tileInput.Height.Set(24f, 0f);
                tileInput.Top.Set(-tileInput.Height.Pixels, 1f);
            }
            Append(tileInput);
            MakeAndAppendLabel(tileInput, "Old Tile");

            newTileInput = new TileTypeSelector();
            {
                newTileInput.Left.Set(tileInput.Width.Pixels + 2f, 0f);
                // amountInput.VAlign = 1f;
                newTileInput.Width.Set(140f, 0f);
                newTileInput.Height.Set(24f, 0f);
                newTileInput.Top.Set(-newTileInput.Height.Pixels, 1f);
            }
            Append(newTileInput);
            MakeAndAppendLabel(newTileInput, "New Tile");
        }

        public override ChangeTile CreateObject()
        {
            return new ChangeTile
            {
                Tile = (string.IsNullOrEmpty(tileInput.Entity) || tileInput.Entity == "None") ? null : tileInput.Entity,
                NewTile = newTileInput.Entity,
            };
        }

        public override void Populate(ChangeTile obj)
        {
            tileInput.Text.Text = tileInput.Entity = obj.Tile ?? "";
            newTileInput.Text.Text = newTileInput.Entity = obj.NewTile;
        }

        public void Visit(RecipeMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    private sealed class DisableRecipeElement : ModifierEditorElement<DisableRecipe>, IVisitor<RecipeMod>
    {
        private readonly BoolWrapper disabledButton;

        public DisableRecipeElement() : base("Change: Disable Recipe")
        {
            disabledButton = new BoolWrapper("Disable Recipe", false);
            {
                disabledButton.Width.Set(0f, 0.5f);
                disabledButton.Left.Set(0f, 0f);
                // disabledButton.Top.Set(16f, 0f);
                disabledButton.VAlign = 1f;
            }
            Append(disabledButton);
        }

        public override DisableRecipe CreateObject()
        {
            return new DisableRecipe
            {
                Disabled = disabledButton.Value,
            };
        }

        public override void Populate(DisableRecipe obj)
        {
            disabledButton.Value = obj.Disabled;
        }

        public void Visit(RecipeMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    private sealed class RemoveIngredientElement : ModifierEditorElement<RemoveIngredient>, IVisitor<RecipeMod>
    {
        private readonly ItemTypeSelector itemInput;

        public RemoveIngredientElement() : base("Change: Remove Ingredient")
        {
            itemInput = new ItemTypeSelector();
            {
                itemInput.Left.Set(0f, 0f);
                // itemInput.VAlign = 1f;
                itemInput.Width.Set(140f, 0f);
                itemInput.Height.Set(24f, 0f);
                itemInput.Top.Set(-itemInput.Height.Pixels, 1f);
            }
            Append(itemInput);
            MakeAndAppendLabel(itemInput, "Item");
        }

        public override RemoveIngredient CreateObject()
        {
            return new RemoveIngredient
            {
                Item = itemInput.Entity,
            };
        }

        public override void Populate(RemoveIngredient obj)
        {
            itemInput.Text.Text = itemInput.Entity = obj.Item;
        }

        public void Visit(RecipeMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    private sealed class RemoveRecipeGroupElement : ModifierEditorElement<RemoveRecipeGroup>, IVisitor<RecipeMod>
    {
        private readonly RecipeGroupSelector itemInput;

        public RemoveRecipeGroupElement() : base("Change: Remove Group")
        {
            itemInput = new RecipeGroupSelector();
            {
                itemInput.Left.Set(0f, 0f);
                // itemInput.VAlign = 1f;
                itemInput.Width.Set(140f, 0f);
                itemInput.Height.Set(24f, 0f);
                itemInput.Top.Set(-itemInput.Height.Pixels, 1f);
            }
            Append(itemInput);
            MakeAndAppendLabel(itemInput, "Group");
        }

        public override RemoveRecipeGroup CreateObject()
        {
            return new RemoveRecipeGroup
            {
                Group = itemInput.Entity,
            };
        }

        public override void Populate(RemoveRecipeGroup obj)
        {
            itemInput.Text.Text = itemInput.Entity = obj.Group;
        }

        public void Visit(RecipeMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    private sealed class RemoveTileElement : ModifierEditorElement<RemoveTile>, IVisitor<RecipeMod>
    {
        private readonly TileTypeSelector tileInput;

        public RemoveTileElement() : base("Change: Remove Tile")
        {
            tileInput = new TileTypeSelector();
            {
                tileInput.Left.Set(0f, 0f);
                // itemInput.VAlign = 1f;
                tileInput.Width.Set(140f, 0f);
                tileInput.Height.Set(24f, 0f);
                tileInput.Top.Set(-tileInput.Height.Pixels, 1f);
            }
            Append(tileInput);
            MakeAndAppendLabel(tileInput, "Tile");
        }

        public override RemoveTile CreateObject()
        {
            return new RemoveTile
            {
                Tile = tileInput.Entity,
            };
        }

        public override void Populate(RemoveTile obj)
        {
            tileInput.Text.Text = tileInput.Entity = obj.Tile;
        }

        public void Visit(RecipeMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    public RecipeModEditorWindow(BaseModifierElement element, BuilderInterfaceState state) : base(element, state) { }

    protected override IEnumerable<ModifierEditorElement> DeriveModifiers(RecipeMod obj)
    {
        yield return CreateAndPopulate<RecipeModElement, RecipeMod>(obj);

        foreach (var condition in obj.Conditions)
        {
            if (condition is CreatesResult createsResult)
            {
                yield return CreateAndPopulate<CreatesResultElement, CreatesResult>(createsResult);
            }

            if (condition is RequiresIngredient requiresIngredient)
            {
                yield return CreateAndPopulate<RequiresIngredientElement, RequiresIngredient>(requiresIngredient);
            }

            if (condition is RequiresRecipeGroup requiresRecipeGroup)
            {
                yield return CreateAndPopulate<RequiresRecipeGroupElement, RequiresRecipeGroup>(requiresRecipeGroup);
            }

            if (condition is RequiresTile requiresTile)
            {
                yield return CreateAndPopulate<RequiresTileElement, RequiresTile>(requiresTile);
            }
        }

        foreach (var change in obj.Changes)
        {
            if (change is AddIngredient addIngredient)
            {
                yield return CreateAndPopulate<AddIngredientElement, AddIngredient>(addIngredient);
            }

            if (change is AddRecipeGroup addRecipeGroup)
            {
                yield return CreateAndPopulate<AddRecipeGroupElement, AddRecipeGroup>(addRecipeGroup);
            }

            if (change is AddTile addTile)
            {
                yield return CreateAndPopulate<AddTileElement, AddTile>(addTile);
            }

            if (change is ChangeIngredient changeIngredient)
            {
                yield return CreateAndPopulate<ChangeIngredientElement, ChangeIngredient>(changeIngredient);
            }

            if (change is ChangeRecipeGroup changeRecipeGroup)
            {
                yield return CreateAndPopulate<ChangeRecipeGroupElement, ChangeRecipeGroup>(changeRecipeGroup);
            }

            if (change is ChangeResult changeResult)
            {
                yield return CreateAndPopulate<ChangeResultElement, ChangeResult>(changeResult);
            }

            if (change is ChangeTile changeTile)
            {
                yield return CreateAndPopulate<ChangeTileElement, ChangeTile>(changeTile);
            }

            if (change is DisableRecipe disableRecipe)
            {
                yield return CreateAndPopulate<DisableRecipeElement, DisableRecipe>(disableRecipe);
            }

            if (change is RemoveIngredient removeIngredient)
            {
                yield return CreateAndPopulate<RemoveIngredientElement, RemoveIngredient>(removeIngredient);
            }

            if (change is RemoveRecipeGroup removeRecipeGroup)
            {
                yield return CreateAndPopulate<RemoveRecipeGroupElement, RemoveRecipeGroup>(removeRecipeGroup);
            }

            if (change is RemoveTile removeTile)
            {
                yield return CreateAndPopulate<RemoveTileElement, RemoveTile>(removeTile);
            }
        }
    }

    protected override IEnumerable<ModifierEditorElement> GetAvailableModifiers()
    {
        yield return new CreatesResultElement();
        yield return new RequiresIngredientElement();
        yield return new RequiresRecipeGroupElement();
        yield return new RequiresTileElement();

        yield return new AddIngredientElement();
        yield return new AddRecipeGroupElement();
        yield return new AddTileElement();
        yield return new ChangeIngredientElement();
        yield return new ChangeRecipeGroupElement();
        yield return new ChangeResultElement();
        yield return new ChangeTileElement();
        yield return new DisableRecipeElement();
        yield return new RemoveIngredientElement();
        yield return new RemoveRecipeGroupElement();
        yield return new RemoveTileElement();
    }
}