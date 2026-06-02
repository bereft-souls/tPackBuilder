using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace PackBuilder.Common.BuilderInterface;

public sealed class UITooltipImageButton : UIImageButton
{
    public UITooltipImageButton(Asset<Texture2D> texture, string text) : base(texture)
    {
        Text = text;
    }

    public string Text { get; }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (IsMouseHovering)
        {
            Main.instance.MouseTextHackZoom(Text);
        }
    }
}
