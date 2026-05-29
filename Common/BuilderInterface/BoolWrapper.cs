using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PackBuilder.Common.ModBuilding.Recipes;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PackBuilder.Common.BuilderInterface;

internal sealed class BoolWrapper : UIPanel
{
    private sealed class ToggleImage : UIElement
    {
        public Asset<Texture2D>? Texture { get; set; }

        public Rectangle? Frame { get; set; }

        public Color Color { get; set; }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            if (Texture is null)
            {
                return;
            }

            /*
            var dims = this.InnerDimensions;
            {
                dims.Width = Frame.Value.Width;
                dims.Height = Frame.Value.Height;
            }

            spriteBatch.Draw(
                Texture.Value,
                dims,
                Frame,
                Color
            );
            */
            
            spriteBatch.Draw(
                Texture.Value,
                GetDimensions().Center(),
                Frame,
                Color,
                0f,
                (Frame?.Size() ?? Texture.Size()) / 2f,
                Vector2.One,
                SpriteEffects.None,
                0f
            );
        }
    }

    private readonly UIText text;

    private readonly ToggleImage toggle;
    private readonly UIText label;

    public bool Value { get; set; }

    public BoolWrapper(string name, bool value)
    {
        Value = value;

        Width.Set(0f, 1f);
        Height.Set(30f, 0f);
        MinHeight.Set(30f, 0f);
        
        SetPadding(6);

        _backgroundTexture = ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/EmptyPanel", AssetRequestMode.ImmediateLoad);
        _borderTexture = ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/SmallPanelOutline", AssetRequestMode.ImmediateLoad);

        label = new UIText(name, 0.8f);
        {
            label.Width.Set(0f, 0.4f);
            label.Height.Set(0f, 1f);

            // label.MaxHeight.Set(32f, 0f);

            // label.Left.Set(4f, 0f);

            label.TextOriginX = 0f;
            label.TextOriginY = 0.5f;

            label.HAlign = 0;
        }
        Append(label);

        var container = new UIElement();
        {
            container.Height.Set(0f, 1f);

            container.Width.Set(0, 0.35f);

            container.VAlign = 0.5f;
            container.HAlign = 1f;

            container.OnLeftClick += OnLeftClick_UpdateValue;

            container.OnMouseOver +=
                (evt, lst) =>
                {
                    SoundEngine.PlaySound(in SoundID.MenuTick);
                    UpdateColors(evt, lst);
                };

            container.OnMouseOut += UpdateColors;
        }
        Append(container);

        text = new UIText(GetText(), 0.8f);
        {
            text.Width.Set(-30f, 1f);
            text.Height.Set(0f, 1f);

            text.Left.Set(-30, 0f);

            text.HAlign = 1f;
            text.VAlign = 0.5f;

            text.TextOriginX = 1f;
            text.TextOriginY = 0.5f;

            text.TextColor = GetColor();

            text.IgnoresMouseInteraction = true;
        }
        container.Append(text);

        toggle = new ToggleImage();
        {
            var asset = ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/Toggle", AssetRequestMode.ImmediateLoad);

            asset.Wait();

            toggle.Texture = asset;

            toggle.Frame = GetButtonFrame();

            toggle.Color = GetColor();

            toggle.HAlign = 1f;

            toggle.Width.Set(30f, 0f);
            toggle.Height.Set(30f, 0f);

            toggle.IgnoresMouseInteraction = true;
        }
        container.Append(toggle);

        return;

        void UpdateColors(UIMouseEvent evt, UIElement listeningElement)
        {
            text?.TextColor = GetColor();
            toggle?.Color = GetColor();
        }

        void OnLeftClick_UpdateValue(UIMouseEvent evt, UIElement listeningElement)
        {
            Value = !Value;

            text?.SetText(GetText());

            toggle?.Frame = GetButtonFrame();

            SoundEngine.PlaySound(in SoundID.MenuTick);
        }

        LocalizedText GetText()
        {
            return Value ? Lang.menu[126] : Lang.menu[124];
        }

        Rectangle? GetButtonFrame()
        {
            return toggle.Texture?.Value.Frame(1, 2, 0, Value.ToInt());
        }

        Color GetColor()
        {
            return container.IsMouseHovering ? Color.White : (Color.White * 0.75f);
        }
    }
}

internal sealed class RecipeCriteriaWrapper : UIPanel
{
    private sealed class ToggleImage : UIElement
    {
        public Asset<Texture2D>? Texture { get; set; }

        public Rectangle? Frame { get; set; }

        public Color Color { get; set; }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            if (Texture is null)
            {
                return;
            }

            spriteBatch.Draw(
                Texture.Value,
                GetDimensions().Center(),
                Frame,
                Color,
                0f,
                (Frame?.Size() ?? Texture.Size()) / 2f,
                Vector2.One,
                SpriteEffects.None,
                0f
            );
        }
    }

    private readonly UIText text;

    private readonly ToggleImage toggle;
    private readonly UIText label;

    public bool Value { get; set; }

    public RecipeCriteriaWrapper(string name, bool value)
    {
        Value = value;

        Width.Set(0f, 1f);
        Height.Set(30f, 0f);
        MinHeight.Set(30f, 0f);
        
        SetPadding(6);

        _backgroundTexture = ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/EmptyPanel", AssetRequestMode.ImmediateLoad);
        _borderTexture = ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/SmallPanelOutline", AssetRequestMode.ImmediateLoad);

        label = new UIText(name, 0.8f);
        {
            label.Width.Set(0f, 0.4f);
            label.Height.Set(0f, 1f);

            // label.MaxHeight.Set(32f, 0f);

            // label.Left.Set(4f, 0f);

            label.TextOriginX = 0f;
            label.TextOriginY = 0.5f;

            label.HAlign = 0;
        }
        Append(label);

        var container = new UIElement();
        {
            container.Height.Set(0f, 1f);

            container.Width.Set(0, 0.35f);

            container.VAlign = 0.5f;
            container.HAlign = 1f;

            container.OnLeftClick += OnLeftClick_UpdateValue;

            container.OnMouseOver +=
                (evt, lst) =>
                {
                    SoundEngine.PlaySound(in SoundID.MenuTick);
                    UpdateColors(evt, lst);
                };

            container.OnMouseOut += UpdateColors;
        }
        Append(container);

        text = new UIText(GetText(), 0.8f);
        {
            text.Width.Set(-30f, 1f);
            text.Height.Set(0f, 1f);

            text.Left.Set(-30, 0f);

            text.HAlign = 1f;
            text.VAlign = 0.5f;

            text.TextOriginX = 1f;
            text.TextOriginY = 0.5f;

            text.TextColor = GetColor();

            text.IgnoresMouseInteraction = true;
        }
        container.Append(text);

        toggle = new ToggleImage();
        {
            var asset = ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/Toggle", AssetRequestMode.ImmediateLoad);

            asset.Wait();

            toggle.Texture = asset;

            toggle.Frame = GetButtonFrame();

            toggle.Color = GetColor();

            toggle.HAlign = 1f;

            toggle.Width.Set(30f, 0f);
            toggle.Height.Set(30f, 0f);

            toggle.IgnoresMouseInteraction = true;
        }
        container.Append(toggle);

        return;

        void UpdateColors(UIMouseEvent evt, UIElement listeningElement)
        {
            text?.TextColor = GetColor();
            toggle?.Color = GetColor();
        }

        void OnLeftClick_UpdateValue(UIMouseEvent evt, UIElement listeningElement)
        {
            Value = !Value;

            text?.SetText(GetText());

            toggle?.Frame = GetButtonFrame();

            SoundEngine.PlaySound(in SoundID.MenuTick);
        }

        string GetText()
        {
            return Value ? "All" : "Any";
        }

        Rectangle? GetButtonFrame()
        {
            return toggle.Texture?.Value.Frame(1, 2, 0, Value.ToInt());
        }

        Color GetColor()
        {
            return container.IsMouseHovering ? Color.White : (Color.White * 0.75f);
        }
    }

    public RecipeCriteria AsCriteria()
    {
        return Value ? RecipeCriteria.All : RecipeCriteria.Any;
    }
    
    public void SetAsCriteria(RecipeCriteria criteria)
    {
        Value = criteria == RecipeCriteria.All;
    }
}
