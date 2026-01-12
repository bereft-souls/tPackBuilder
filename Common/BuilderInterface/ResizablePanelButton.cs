using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PackBuilder.Common.BuilderInterface;

internal sealed class ResizablePanelButton : UIElement
{
    public static int VisibleWidth => 28;

    public static int VisibleHeight => 28;

    private static readonly Asset<Texture2D> asset = ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/DraggablePanelCorner");

    private Vector2? mouseRelativeToTopLeftOfParent;

    public ResizablePanelButton()
    {
        Width.Pixels = VisibleHeight;
        Height.Pixels = VisibleHeight;
        Left.Set(-VisibleWidth, 1f);
        Top.Set(-VisibleHeight, 1f);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var dims = GetDimensions();
        spriteBatch.Draw(asset.Value, dims.Position(), Color.White);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        var mouseCurrent = Main.MouseScreen;
        if (ContainsPoint(mouseCurrent))
        {
            Main.LocalPlayer.mouseInterface = true;
        }

        if (mouseRelativeToTopLeftOfParent.HasValue)
        {
            var newPos = Main.MouseScreen - GetDimensions().Position();
            ApplyDimensionsDelta(mouseRelativeToTopLeftOfParent.Value - newPos);
            // mouseRelativeToTopLeftOfParent = newPos;
        }

        EnsureConstrainedDimensions();
    }

    private void EnsureConstrainedDimensions()
    {
        var parentDims = Parent.Parent?.GetDimensions() ?? UserInterface.ActiveInstance.GetDimensions();
        var scopedDims = Parent.GetDimensionsBasedOnParentDimensions(parentDims);

        var minWidth = Parent.MinWidth.GetValue(scopedDims.Width);
        var maxWidth = Parent.MaxWidth.GetValue(scopedDims.Width);
        var minHeight = Parent.MinHeight.GetValue(scopedDims.Height);
        var maxHeight = Parent.MaxHeight.GetValue(scopedDims.Height);

        var realWidth = Parent.Width.GetValue(scopedDims.Width);
        var realHeight = Parent.Height.GetValue(scopedDims.Height);

        var clampedWidth = Math.Clamp(realWidth, minWidth, maxWidth);
        var clampedHeight = Math.Clamp(realHeight, minHeight, maxHeight);

        var deltaWidth = realWidth - clampedWidth;
        var deltaHeight = realHeight - clampedHeight;

        ApplyDimensionsDelta(new Vector2(deltaWidth, deltaHeight));

        Recalculate();
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);

        mouseRelativeToTopLeftOfParent = Main.MouseScreen - GetDimensions().Position();
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);

        mouseRelativeToTopLeftOfParent = null;
    }

    /*
    private void ResizeBegin(UIMouseEvent evt)
    {
        offset = new Vector2(evt.MousePosition.X - Left.Pixels, evt.MousePosition.Y - Top.Pixels);
    }

    private void ResizeEnd(UIMouseEvent evt)
    {
        if (!offset.HasValue)
        {
            return;
        }

        var delta = evt.MousePosition - offset.Value;
        offset = null;

        ApplyDimensionsDelta(delta);
    }
    */

    private void ApplyDimensionsDelta(Vector2 delta)
    {
        Parent.Width.Pixels -= delta.X;
        Parent.Height.Pixels -= delta.Y;
        Parent.Left.Pixels -= delta.X * Parent.HAlign;
        Parent.Top.Pixels -= delta.Y * Parent.VAlign;
    }
}
