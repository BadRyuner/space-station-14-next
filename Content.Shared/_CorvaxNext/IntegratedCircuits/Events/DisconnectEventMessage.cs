using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.IntegratedCircuits.Events;

[Serializable, NetSerializable]
public sealed class DisconnectEventMessage : BoundUserInterfaceMessage
{
    public NetEntity Circuit;

    public string CircuitEvent;

    public NetEntity DisconnectCircuit;

    public string DisconnectEvent;

    public DisconnectEventMessage(NetEntity circuit, string circuitEvent, NetEntity disconnectCircuit, string disconnectEvent)
    {
        Circuit = circuit;
        CircuitEvent = circuitEvent;
        DisconnectCircuit = disconnectCircuit;
        DisconnectEvent = disconnectEvent;
    }
}
