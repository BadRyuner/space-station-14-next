using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.ModularComputers.Events;

[NetSerializable, Serializable]
public sealed class ChangeModularComputerStateEvent(NetEntity target, bool newState) : EntityEventArgs
{
    public NetEntity Target = target;
    public bool NewState = newState;
}
