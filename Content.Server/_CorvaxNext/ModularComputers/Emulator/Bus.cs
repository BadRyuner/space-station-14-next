using Content.Shared._CorvaxNext.ModularComputers.Components;
using Content.Shared._CorvaxNext.ModularComputers.Emulator;

namespace Content.Server._CorvaxNext.ModularComputers.Emulator;

public sealed class Bus(Cpu cpu, PciCpuComponent pciCpuComponent, int dramSize)
{
    public PciCpuComponent PciCpu = pciCpuComponent;

    public Pci Pci = new(cpu);

    public Dram Dram = new(dramSize);

    public ulong Read(ulong addr, Bits size)
    {
        return addr switch
        {
            < Pci.PciBase => 0, // no no no no
            >= Pci.PciBase and <= Pci.PciEnd => Pci.Read(addr, size),
            >= Dram.DramBase when addr <= (ulong)(Dram.Size + Dram.DramBase) => Dram.Read(addr, size),
            _ => throw new LoadAccessFault() // Out Of Mem
        };
    }

    public void Write(ulong addr, ulong value, Bits size)
    {
        if (addr is >= Pci.PciBase and <= Pci.PciEnd)
            Pci.Write(addr, value, size);
        else if (addr >= Dram.DramBase && addr <= (ulong)(Dram.Size + Dram.DramBase))
            Dram.Write(addr, value, size);
        else
            throw new StoreAMOAccessFault();
    }
}
