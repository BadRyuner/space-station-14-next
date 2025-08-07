using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.IntegratedCircuits.Events;

[Serializable, NetSerializable]
public sealed class RemoveCircuitMessage : BoundUserInterfaceMessage
{
    public NetEntity Circuit;

    public RemoveCircuitMessage(NetEntity circuit)
    {
        Circuit = circuit;
    }
}
