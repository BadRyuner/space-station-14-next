using Content.Shared._CorvaxNext.IntegratedCircuits.Events;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._CorvaxNext.IntegratedCircuits.UI;

[UsedImplicitly]
public sealed class ElectronicAssemblyBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ElectronicAssemblyMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ElectronicAssemblyMenu>();
        _menu.SetupWindow(Owner, this);
        _menu.OpenCentered();

        _menu.OnClose += Close;
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (_menu is null
            || message is not UpdateMenuMessage)
            return;

        _menu.BuildCircuitUI();
    }

    public void SendConnectWireMessage(NetEntity circuitToConnect, string wireToConnect, NetEntity ownerCircuit, string ownerWire)
    {
        SendMessage(new ConnectWireMessage(circuitToConnect, wireToConnect, ownerCircuit, ownerWire));
    }

    public void SendConnectEventMessage(NetEntity circuitToConnect, string eventToConnect, NetEntity ownerCircuit, string ownerEvent)
    {
        SendMessage(new ConnectEventMessage(circuitToConnect, eventToConnect, ownerCircuit, ownerEvent));
    }

    public void SendChangeStringMemoryCircuitMessage(NetEntity entity, string newText)
    {
        SendMessage(new ChangeStringMemoryCircuitMessage(entity, newText));
    }

    public void SendDisconnectWireMessage(NetEntity circuit, string circuitWire, NetEntity disconnectCircuit, string disconnectWire)
    {
        SendMessage(new DisconnectWireMessage(circuit, circuitWire, disconnectCircuit, disconnectWire));
    }

    public void SendDisconnectEventMessage(NetEntity circuit, string circuitEvent, NetEntity disconnectCircuit, string disconnectEvent)
    {
        SendMessage(new DisconnectEventMessage(circuit, circuitEvent, disconnectCircuit, disconnectEvent));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Dispose();
    }
}
