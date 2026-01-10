using Terraria.UI;

namespace PackBuilder.Common.BuilderInterface;

/// <summary>
///     The entire UI state encompassing the builder UI.  Individual
///     &quot;windows&quot; (panels) within it serve the real functionality.
/// </summary>
internal sealed class BuilderInterfaceState : UIState
{
    private DraggablePanel? samplePanel;

    public override void OnInitialize()
    {
        base.OnInitialize();

        samplePanel = new DraggablePanel();
        {
            samplePanel.Width.Set(400f, 0f);
            samplePanel.Height.Set(200f, 0f);
            // samplePanel.Left.Set(0f, 0.5f);
            // samplePanel.Top.Set(0f, 0.5f);
            samplePanel.HAlign = 0.5f;
            samplePanel.VAlign = 0.5f;
        }
        Append(samplePanel);
    }
}
