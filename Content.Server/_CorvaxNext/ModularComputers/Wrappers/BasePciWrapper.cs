using Content.Server._CorvaxNext.ModularComputers.Emulator;
using Content.Shared._CorvaxNext.ModularComputers.Emulator;

namespace Content.Server._CorvaxNext.ModularComputers.Wrappers;
public abstract class BasePciWrapper
{
    public Cpu Cpu = null!;

    public abstract ulong Read(ulong address, Bits size);

    public abstract void Write(ulong address, ulong value, Bits size);
}
