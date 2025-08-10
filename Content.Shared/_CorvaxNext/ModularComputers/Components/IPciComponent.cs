using Content.Shared._CorvaxNext.ModularComputers.Emulator;

namespace Content.Shared._CorvaxNext.ModularComputers.Components;

public interface IPciComponent
{
    public ulong Read(Dram dram, ulong address, Bits size);

    public void Write(Dram dram, ulong address, ulong value, Bits size);
}
