using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.IntegratedCircuits.Events;

[NetSerializable, Serializable]
public sealed class UpdateMenuMessage : BoundUserInterfaceMessage
{
    public bool FullRebuild;

    public UpdateMenuMessage(bool fullRebuild = false)
    {
        FullRebuild = fullRebuild;
    }
}
