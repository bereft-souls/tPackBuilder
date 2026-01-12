using PackBuilder.Common.Project;
using System.Linq;
using Terraria.UI;

namespace PackBuilder.Common.BuilderInterface.Windows;

internal sealed class ModControlPanelWindow : AbstractInterfaceWindow
{
    private ModNameSelectionGrid? projectViewThing;

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

        var projects = ModProjectProvider.ModSourcesViews.ToList();
        projectViewThing = new ModNameSelectionGrid(projects);
        {
            projectViewThing.OnLeftClick += ProjectViewThing_OnLeftClick;
            projectViewThing.OnClickingOption += ProjectViewThing_OnClickingOption;
        }
        Append(projectViewThing);
    }

    private void ProjectViewThing_OnLeftClick(UIMouseEvent evt, UIElement listeningElement)
    {
        if (evt.Target == projectViewThing)
        {
            CloseModNameGrid();
        }
    }

    private void ProjectViewThing_OnClickingOption()
    {
    }

    private void OpenOrCloseModNameGrid(UIMouseEvent evt, UIElement listeningElement)
    {
        if (projectViewThing?.Parent is not null)
        {
            CloseModNameGrid();
            return;
        }

        projectViewThing?.Remove();
        Append(projectViewThing);
    }

    private void CloseModNameGrid()
    {
        projectViewThing?.Remove();
    }
}
