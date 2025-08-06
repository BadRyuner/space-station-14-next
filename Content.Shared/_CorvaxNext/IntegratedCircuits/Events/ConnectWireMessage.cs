using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.IntegratedCircuits.Events;

[Serializable, NetSerializable]
public sealed class ConnectWireMessage : BoundUserInterfaceMessage
{
    public NetEntity CircuitToConnect;

    public string WireToConnect;

    public NetEntity OwnerCircuit;

    public string OwnerWire;

    public ConnectWireMessage(NetEntity circuitToConnect, string wireToConnect, NetEntity ownerCircuit, string ownerWire)
    {
        CircuitToConnect = circuitToConnect;
        WireToConnect = wireToConnect;
        OwnerCircuit = ownerCircuit;
        OwnerWire = ownerWire;
    }
}
