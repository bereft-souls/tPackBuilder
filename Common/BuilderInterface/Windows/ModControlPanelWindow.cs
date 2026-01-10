using Terraria.UI;

namespace PackBuilder.Common.BuilderInterface.Windows;

internal sealed class ModControlPanelWindow : AbstractInterfaceWindow
{
    public override void OnInitialize()
    {
        base.OnInitialize();

        var resizablePanelButton = new ResizablePanelButton();
        {
            resizablePanelButton.Left.Pixels += PaddingLeft;
            resizablePanelButton.Top.Pixels += PaddingTop;
        }
        Append(resizablePanelButton);

        var containerElement = new UIElement();
        {
            containerElement.Width.Set(0f, 1f);
            containerElement.Height.Set(0f, 1f);

            containerElement.IgnoresMouseInteraction = true;
        }
        Append(containerElement);
        // ClickThroughElements.Add(containerElement);
    }
}
