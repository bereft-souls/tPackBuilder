using PackBuilder.Common.BuilderInterface.Windows;
using Terraria.UI;

namespace PackBuilder.Common.BuilderInterface;

/// <summary>
///     The entire UI state encompassing the builder UI.  Individual
///     &quot;windows&quot; (panels) within it serve the real functionality.
/// </summary>
internal sealed class BuilderInterfaceState : UIState
{
    public ModControlPanelWindow? ModControlPanel { get; private set; }

    public override void OnInitialize()
    {
        base.OnInitialize();

        ModControlPanel = new ModControlPanelWindow();
        {
            ModControlPanel.Width.Set(600f, 0f);
            ModControlPanel.Height.Set(350f, 0f);
            ModControlPanel.MinWidth.Set(400f, 0f);
            ModControlPanel.MinHeight.Set(200f, 0f);
            ModControlPanel.HAlign = 0.5f;
            ModControlPanel.VAlign = 0.5f;
        }
        Append(ModControlPanel);
    }
}
