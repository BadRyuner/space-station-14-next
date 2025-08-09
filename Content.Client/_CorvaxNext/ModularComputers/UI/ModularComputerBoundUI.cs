using Robust.Client.UserInterface;

namespace Content.Client._CorvaxNext.ModularComputers.UI;

public sealed class ModularComputerBoundUI(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ModularComputerUI? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ModularComputerUI>();
        _menu.Initialize(Owner);
        _menu.OpenCentered();
        _menu.OnClose += Close;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Dispose();
    }
}
