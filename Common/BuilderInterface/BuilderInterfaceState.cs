using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using PackBuilder.Common.BuilderInterface.Windows;
using PackBuilder.Common.Project;
using Terraria.UI;

namespace PackBuilder.Common.BuilderInterface;

/// <summary>
///     The entire UI state encompassing the builder UI.  Individual
///     &quot;windows&quot; (panels) within it serve the real functionality.
/// </summary>
public sealed class BuilderInterfaceState : UIState
{
    private readonly List<AbstractInterfaceWindow> windowsToBringToFront = [];
    private readonly List<AbstractInterfaceWindow> windowsToClose = [];
    internal ModControlPanelWindow? ModControlPanel { get; private set; }

    internal PropertyEditorWindow? PropertyEditorPanel { get; private set; }

    public override void OnInitialize()
    {
        base.OnInitialize();

        ModControlPanel = new ModControlPanelWindow(this);
        {
            ModControlPanel.HAlign = 0.5f;
            ModControlPanel.VAlign = 0.5f;
            ModControlPanel.OnEdit += OnEdit_OpenPropertyEditor;
        }
        Append(ModControlPanel);
    }

    public override void Update(GameTime gameTime)
    {
        foreach (var window in windowsToBringToFront)
        {
            if (Elements.Remove(window))
            {
                Append(window);
            }
        }

        windowsToBringToFront.Clear();

        foreach (var window in windowsToClose)
        {
            CloseWindow(window);
        }

        windowsToClose.Clear();

        base.Update(gameTime);
    }

    private void OnEdit_OpenPropertyEditor(ModProjectView view)
    {
        var id = new WindowId("PropertyEditor", view.InternalName, view.Directory);
        if (TryOpenWindowById(id))
        {
            return;
        }

        PropertyEditorPanel = new PropertyEditorWindow(view, this);
        {
            PropertyEditorPanel.WindowId = id;
            PropertyEditorPanel.Width.Set(600f, 0f);
            PropertyEditorPanel.Height.Set(350f, 0f);
            PropertyEditorPanel.MinWidth.Set(400f, 0f);
            PropertyEditorPanel.MinHeight.Set(200f, 0f);

            if (ModControlPanel is not null)
            {
                var dimensions = ModControlPanel.GetDimensions();
                PropertyEditorPanel.Left.Set(dimensions.X + dimensions.Width + 16, 0f);
                PropertyEditorPanel.Top.Set(dimensions.Y, 0f);
            }
            else
            {
                PropertyEditorPanel.HAlign = 0.5f;
                PropertyEditorPanel.VAlign = 0.5f;
            }
        }
        Append(PropertyEditorPanel);
        PropertyEditorPanel.Activate();
    }

    public bool TryOpenWindowById(WindowId? uniqueId)
    {
        var candidate = GetWindowById(uniqueId);
        if (candidate is not null)
        {
            BringToFront(candidate);
            return true;
        }

        return false;
    }

    public void AddWindowOrBringToFront(AbstractInterfaceWindow window)
    {
        var candidate = GetWindowById(window.WindowId);
        if (candidate is not null)
        {
            BringToFront(candidate);
        }
        else
        {
            Append(window);
        }
    }

    public void CloseWindowById(WindowId? uniqueId)
    {
        var candidate = GetWindowById(uniqueId);
        if (candidate is not null)
        {
            CloseWindow(candidate);
        }
    }

    public void CloseWindow(AbstractInterfaceWindow window)
    {
        window.WindowId = null;
        window.Remove();
    }

    public void QueueCloseWindow(AbstractInterfaceWindow window)
    {
        windowsToClose.Add(window);
    }

    public void BringToFront(AbstractInterfaceWindow window)
    {
        windowsToBringToFront.Add(window);
    }

    private AbstractInterfaceWindow? GetWindowById(WindowId? uniqueId)
    {
        if (uniqueId is null)
        {
            return null;
        }

        return Elements.OfType<AbstractInterfaceWindow>().FirstOrDefault(x => x.WindowId == uniqueId);
    }
}
