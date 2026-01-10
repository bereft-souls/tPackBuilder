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

    private Vector2? offset;

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

        if (ContainsPoint(Main.MouseScreen))
        {
            Main.LocalPlayer.mouseInterface = true;
        }

        if (offset.HasValue)
        {
            var delta = Main.MouseScreen - offset.Value;
            UpdateDimensions(delta);
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

        Parent.Width.Pixels -= deltaWidth;
        Parent.Height.Pixels -= deltaHeight;

        Recalculate();
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);

        if (evt.Target == this)
        {
            ResizeBegin(evt);
        }
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);

        if (evt.Target == this)
        {
            ResizeEnd(evt);
        }
    }

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

        UpdateDimensions(delta);
    }

    private void UpdateDimensions(Vector2 delta)
    {
        Parent.Width.Set(Parent.Width.Pixels + delta.X, Parent.Width.Percent);
        Parent.Height.Set(Parent.Height.Pixels + delta.Y, Parent.Height.Percent);

        EnsureConstrainedDimensions();
    }
}
