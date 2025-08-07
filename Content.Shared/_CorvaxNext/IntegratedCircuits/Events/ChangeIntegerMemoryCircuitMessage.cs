using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.IntegratedCircuits.Events;

[Serializable, NetSerializable]
public sealed class ChangeIntegerMemoryCircuitMessage : BoundUserInterfaceMessage
{
    public NetEntity Entity;

    public int NewInt;

    public ChangeIntegerMemoryCircuitMessage(NetEntity entity, int newInt)
    {
        Entity = entity;
        NewInt = newInt;
    }
}
