using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.ModularComputers.Events;

[NetSerializable, Serializable]
public sealed class CreateLoadProgramUIEvent(NetEntity target) : EntityEventArgs
{
    public NetEntity Target = target;
}
