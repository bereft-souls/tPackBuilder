using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PackBuilder.Common.BuilderInterface;

internal class DraggablePanel : UIPanel
{
    private Vector2? offset;

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
            UpdateOffset(delta);
        }

        EnsurePanelIsVisible();
    }

    private void EnsurePanelIsVisible()
    {
        var parentDims = Parent.GetDimensions().ToRectangle();
        var selfDims = GetDimensions().ToRectangle();
        if (selfDims.Intersects(parentDims))
        {
            return;
        }

        Left.Pixels = Math.Clamp(Left.Pixels, 0f, parentDims.Right - Width.Pixels);
        Top.Pixels = Math.Clamp(Top.Pixels, 0f, parentDims.Bottom - Height.Pixels);
            
        Recalculate();
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);

        if (evt.Target == this)
        {
            DragBegin(evt);
        }
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);

        if (evt.Target == this)
        {
            DragEnd(evt);
        }
    }

    private void DragBegin(UIMouseEvent evt)
    {
        offset = new Vector2(evt.MousePosition.X - Left.Pixels, evt.MousePosition.Y - Top.Pixels);
    }

    private void DragEnd(UIMouseEvent evt)
    {
        if (!offset.HasValue)
        {
            return;
        }

        var delta = evt.MousePosition - offset.Value;
        offset = null;

        UpdateOffset(delta);
    }

    private void UpdateOffset(Vector2 delta)
    {
        Left.Set(delta.X, Left.Percent);
        Top.Set(delta.Y, Top.Percent);

        Recalculate();
    }
}
