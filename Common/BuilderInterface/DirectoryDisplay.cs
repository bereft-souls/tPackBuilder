using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PackBuilder.Common.BuilderInterface;

internal sealed class DirectoryDisplay : UIElement
{
    private sealed class DirectoryContainer : UIPanel
    {
        private readonly DirectoryDisplay display;

        public DirectoryContainer(DirectoryDisplay display)
        {
            this.display = display;

            _backgroundTexture = ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/EmptyPanel", AssetRequestMode.ImmediateLoad);
            _borderTexture = ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/SmallPanelOutline", AssetRequestMode.ImmediateLoad);

            OverflowHidden = true;
        }

        private static Color PathColor => Color.White;

        private static Color EmptyColor => Color.Gray;

        private float Scale => 0.8f;

        public float TextHAlign { get; } = 0f;

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            DrawText(spriteBatch);
        }

        private void DrawText(SpriteBatch sb)
        {
            var text = display.Directory;
            var color = PathColor;
            if (string.IsNullOrEmpty(text))
            {
                text = Language.GetTextValue("Mods.PackBuilder.UI.RootDirectory");
                color = EmptyColor;
            }

            var font = FontAssets.MouseText.Value;
            var textSize = ChatManager.GetStringSize(font, display.Directory, Vector2.One);
            textSize.Y = 16f;
            textSize *= Scale;
            textSize = textSize.Floor();

            var innerDims = GetInnerDimensions();
            var pos = innerDims.Position();
            pos.Y += 2f;
            pos.X += (innerDims.Width - textSize.X) * TextHAlign;

            Utils.DrawBorderString(sb, text, pos, color, scale: Scale, anchory: 0.3f);
        }
    }

    private sealed class SlashElement : UIElement
    {
        public SlashElement()
        {
            TextSize = FontAssets.MouseText.Value.MeasureString("/");
            Width.Set(TextSize.X, 0f);
        }

        private float Scale => 1f;

        private Vector2 TextSize { get; }

        private float TextHAlign => 0.5f;

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            var innerDims = GetInnerDimensions();
            var pos = innerDims.Position();
            pos.Y += 4f;
            pos.X += (innerDims.Width - TextSize.X) * TextHAlign;

            Utils.DrawBorderString(spriteBatch, "/", pos, Color.White, scale: Scale);
        }
    }

    private readonly DirectoryContainer directoryContainer;

    private readonly UIGrid grid;
    private readonly ModNameDropDown modNameDropDown;
    private readonly SlashElement slashElement;

    public DirectoryDisplay(ModNameDropDown modNameDropDown)
    {
        this.modNameDropDown = modNameDropDown;

        OverflowHidden = true;

        grid = new UIGrid();
        {
            grid.ListPadding = 4f;
            grid.ManualSortMethod = _ => { };
            grid.Width.Set(0f, 1f);
            grid.Height.Set(0f, 1f);
        }
        Append(grid);

        slashElement = new SlashElement();
        {
            // slashElement.Width.Set(0f, 1f);
            slashElement.Height.Set(0f, 1f);
        }

        directoryContainer = new DirectoryContainer(this);
        {
            directoryContainer.Width.Set(0f, 1f);
            directoryContainer.Height.Set(0f, 1f);
        }

        grid.Add(modNameDropDown);
        grid.Add(slashElement);
        grid.Add(directoryContainer);
    }

    public string Directory { get; set; } = string.Empty;

    public override void RecalculateChildren()
    {
        base.RecalculateChildren();

        var gridDims = grid.GetDimensions();
        var totalWidth = gridDims.Width;
        var totalPadding = grid.ListPadding * 3f;
        var takenElementWidth =
            modNameDropDown.GetDimensions().Width
          + slashElement.GetDimensions().Width;
        var targetWidth = totalWidth - (takenElementWidth + totalPadding);
        directoryContainer.Width.Set(targetWidth, 0f);

        base.RecalculateChildren();
    }
}
