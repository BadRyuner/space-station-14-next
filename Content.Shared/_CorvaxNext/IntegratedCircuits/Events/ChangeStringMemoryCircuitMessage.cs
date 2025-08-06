using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.IntegratedCircuits.Events;

[Serializable, NetSerializable]
public sealed class ChangeStringMemoryCircuitMessage : BoundUserInterfaceMessage
{
    public NetEntity Entity;

    public string NewText;

    public ChangeStringMemoryCircuitMessage(NetEntity entity, string newText)
    {
        Entity = entity;
        NewText = newText;
    }
}
