using Content.Shared._CorvaxNext.ModularComputers.Messages;
using Robust.Client.UserInterface;

namespace Content.Client._CorvaxNext.ModularComputers.UI;

public sealed class ProshivkaBoundUI(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ProshivkaUI? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ProshivkaUI>();
        _menu.Initialize(this);
        _menu.OpenCentered();
        _menu.OnClose += Close;
    }

    public void SendProgramm(byte[] data)
    {
        SendMessage(new LoadProgramMessage(data));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Dispose();
    }
}
