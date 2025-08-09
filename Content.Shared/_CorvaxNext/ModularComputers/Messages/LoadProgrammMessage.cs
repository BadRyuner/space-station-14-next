using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.ModularComputers.Messages;

[NetSerializable, Serializable]
public sealed class LoadProgramMessage(byte[] program) : BoundUserInterfaceMessage
{
    public byte[] Program = program;
}
