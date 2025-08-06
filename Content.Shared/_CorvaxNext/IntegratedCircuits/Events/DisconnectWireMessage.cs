using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.IntegratedCircuits.Events;

[Serializable, NetSerializable]
public sealed class DisconnectWireMessage : BoundUserInterfaceMessage
{
    public NetEntity Circuit;

    public string CircuitWire;

    public NetEntity DisconnectCircuit;

    public string DisconnectWire;

    public DisconnectWireMessage(NetEntity circuit, string circuitWire, NetEntity disconnectCircuit, string disconnectWire)
    {
        Circuit = circuit;
        CircuitWire = circuitWire;
        DisconnectCircuit = disconnectCircuit;
        DisconnectWire = disconnectWire;
    }
}
