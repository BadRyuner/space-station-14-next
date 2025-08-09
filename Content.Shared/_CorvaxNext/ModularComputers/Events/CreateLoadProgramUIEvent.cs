namespace Content.Shared._CorvaxNext.ModularComputers.Events;

public sealed class CreateLoadProgramUIEvent(NetEntity target) : EntityEventArgs
{
    public NetEntity Target = target;
}
