namespace PackBuilder.Common.BuilderInterface.Windows;

public sealed record WindowId(
    string WindowName,
    string? ModName,
    string? ModDirectory
);

public abstract class AbstractInterfaceWindow : DraggablePanel
{
    protected AbstractInterfaceWindow(BuilderInterfaceState state)
    {
        State = state;
        OnLeftMouseDown += (_, _) => BringToFront();
    }

    public WindowId? WindowId { get; set; }

    public BuilderInterfaceState State { get; set; }

    public void BringToFront()
    {
        State.BringToFront(this);
    }

    public void CloseWindow()
    {
        State.CloseWindow(this);
    }

    public void QueueCloseWindow()
    {
        State.QueueCloseWindow(this);
    }
}
