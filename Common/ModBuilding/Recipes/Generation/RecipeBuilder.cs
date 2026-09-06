using Microsoft.Xna.Framework.Graphics;
using PackBuilder.Common.ModBuilding.Recipes.Generation.Properties;
using ReLogic.Content;
using System.Collections.Generic;
using Newtonsoft.Json;
using PackBuilder.Common.BuilderInterface;
using PackBuilder.Common.BuilderInterface.Windows;
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

    public override AbstractInterfaceWindow? CreateEditorWindow(BaseModifierElement element, BuilderInterfaceState state)
    {
        return new RecipeBuilderEditorWindow(element, state);
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

internal sealed class RecipeBuilderEditorWindow : AbstractEditorWindow<RecipeBuilder, RecipeBuilderEditorWindow.RecipeBuilderElement>
{
    public sealed class RecipeBuilderElement : ModifierEditorElement<RecipeBuilder>
    {
        private readonly ItemTypeSelector itemInput;
        private readonly InputField amountInput;

        public RecipeBuilderElement() : base("Recipe Builder", @static: true)
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

        public override RecipeBuilder CreateObject()
        {
            return new RecipeBuilder
            {
                Result = new RecipeResult
                {
                    Item = itemInput.Entity,
                    Count = int.TryParse(amountInput.Text, out var amount) ? amount : 1,
                },
            };
        }

        public override void Populate(RecipeBuilder obj)
        {
            itemInput.Text.Text = itemInput.Entity = obj.Result?.Item ?? "";
            amountInput.Text = obj.Result?.Count.ToString() ?? "1";
        }
    }

    private sealed class RecipeIngredientElement : ModifierEditorElement<RecipeIngredient>, IVisitor<RecipeBuilder>
    {
        private readonly ItemTypeSelector itemInput;
        private readonly InputField amountInput;

        public RecipeIngredientElement() : base("Add Item")
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

        public override RecipeIngredient CreateObject()
        {
            return new RecipeIngredient
            {
                Item = itemInput.Entity,
                Count = int.TryParse(amountInput.Text, out var amount) ? amount : 1,
            };
        }

        public override void Populate(RecipeIngredient obj)
        {
            itemInput.Text.Text = itemInput.Entity = obj.Item;
            amountInput.Text = obj.Count.ToString();
        }

        public void Visit(RecipeBuilder obj)
        {
            obj.Ingredients.Add(CreateObject());
        }
    }

    private sealed class RecipeGroupIngredientElement : ModifierEditorElement<RecipeGroupIngredient>, IVisitor<RecipeBuilder>
    {
        private readonly RecipeGroupSelector itemInput;
        private readonly InputField amountInput;

        public RecipeGroupIngredientElement() : base("Add Item Group")
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
            MakeAndAppendLabel(itemInput, "Item Group");

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

        public override RecipeGroupIngredient CreateObject()
        {
            return new RecipeGroupIngredient
            {
                Group = itemInput.Entity,
                Count = int.TryParse(amountInput.Text, out var amount) ? amount : 1,
            };
        }

        public override void Populate(RecipeGroupIngredient obj)
        {
            itemInput.Text.Text = itemInput.Entity = obj.Group;
            amountInput.Text = obj.Count.ToString();
        }

        public void Visit(RecipeBuilder obj)
        {
            var created = CreateObject();
            if (created.Group is null or "None")
            {
                return;
            }

            obj.Groups.Add(created);
        }
    }

    private sealed class TileElement : ModifierEditorElement<string>, IVisitor<RecipeBuilder>
    {
        private readonly TileTypeSelector tileInput;

        public TileElement() : base("Add Tile")
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

        public override string CreateObject()
        {
            return tileInput.Entity;
        }

        public override void Populate(string obj)
        {
            tileInput.Text.Text = tileInput.Entity = obj;
        }

        public void Visit(RecipeBuilder obj)
        {
            obj.Tiles.Add(CreateObject());
        }
    }

    public RecipeBuilderEditorWindow(BaseModifierElement element, BuilderInterfaceState state) : base(element, state) { }

    protected override IEnumerable<ModifierEditorElement> DeriveModifiers(RecipeBuilder obj)
    {
        yield return CreateAndPopulate<RecipeBuilderElement, RecipeBuilder>(obj);

        foreach (var ingredient in obj.Ingredients)
        {
            yield return CreateAndPopulate<RecipeIngredientElement, RecipeIngredient>(ingredient);
        }

        foreach (var groupIngredient in obj.Groups)
        {
            yield return CreateAndPopulate<RecipeGroupIngredientElement, RecipeGroupIngredient>(groupIngredient);
        }

        foreach (var tile in obj.Tiles)
        {
            yield return CreateAndPopulate<TileElement, string>(tile);
        }
    }

    protected override IEnumerable<ModifierEditorElement> GetAvailableModifiers()
    {
        yield return new RecipeIngredientElement();
        yield return new RecipeGroupIngredientElement();
        yield return new TileElement();
    }
}