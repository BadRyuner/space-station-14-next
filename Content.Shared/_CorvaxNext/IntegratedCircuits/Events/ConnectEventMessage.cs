using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.IntegratedCircuits.Events;

[Serializable, NetSerializable]
public sealed class ConnectEventMessage : BoundUserInterfaceMessage
{
    public NetEntity CircuitToConnect;

    public string EventToConnect;

    public NetEntity OwnerCircuit;

    public string OwnerEvent;

    public ConnectEventMessage(NetEntity circuitToConnect, string eventToConnect, NetEntity ownerCircuit, string ownerEvent)
    {
        CircuitToConnect = circuitToConnect;
        EventToConnect = eventToConnect;
        OwnerCircuit = ownerCircuit;
        OwnerEvent = ownerEvent;
    }
}
