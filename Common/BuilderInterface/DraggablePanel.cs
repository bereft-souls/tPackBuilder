using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PackBuilder.Common.BuilderInterface;

public class DraggablePanel : UIPanel
{
    private Vector2? offset;

    protected bool ClickThroughThisTime { get; set; }

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

        ClickThroughThisTime = false;
    }

    private void EnsurePanelIsVisible()
    {
        var parentDims = Parent.GetDimensions().ToRectangle();
        var selfDims = GetDimensions().ToRectangle();
        if (parentDims.Contains(selfDims))
        {
            return;
        }

        if (selfDims.Left < parentDims.Left)
        {
            Left.Pixels -= selfDims.Left - parentDims.Left;
        }
        else if (selfDims.Right > parentDims.Right)
        {
            Left.Pixels -= selfDims.Right - parentDims.Right;
        }

        if (selfDims.Top < parentDims.Top)
        {
            Top.Pixels -= selfDims.Top - parentDims.Top;
        }
        else if (selfDims.Bottom > parentDims.Bottom)
        {
            Top.Pixels -= selfDims.Bottom - parentDims.Bottom;
        }

        Recalculate();
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);

        if (evt.Target == this || ClickThroughThisTime)
        {
            DragBegin(evt);
        }
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);

        if (evt.Target == this || ClickThroughThisTime)
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
