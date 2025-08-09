using Content.Server._CorvaxNext.ModularComputers.Wrappers;
using Content.Shared._CorvaxNext.ModularComputers.Components;
using Content.Shared._CorvaxNext.ModularComputers.Emulator;

namespace Content.Server._CorvaxNext.ModularComputers.Emulator;

public sealed class Pci
{
    public const int PciBase = 0xf00_0000;
    public const int PciEnd = PciBase + 0x100_0000;

    [Dependency] private ModularComputersSystem _modularComputersSystem = null!;

    private readonly Cpu _myCpu;

    public Pci(Cpu cpu)
    {
        _myCpu = cpu;
        IoCManager.InjectDependencies(this);
    }

    private BasePciComponent? ResolvePci(ulong addr)
    {
        foreach (var pci in _modularComputersSystem.GetAllPciComponents(_myCpu.Bus.PciCpu.ModularComputer!.Value))
        {
            if (addr >= pci.PciAddressStart && addr <= pci.PciAddressEnd)
                return pci;
        }

        return null;
    }

    public ulong Read(ulong addr, Bits size)
    {
        var pci = ResolvePci(addr);
        if (pci != null)
        {
            if (pci.Wrapper is BasePciWrapper wrapper)
            {
                if (wrapper.Cpu == null)
                    wrapper.Cpu = _myCpu;
                return wrapper.Read(addr, size);
            }
        }

        return 0; // или мб ошибку кидать?
    }

    public void Write(ulong addr, ulong value, Bits size)
    {
        var pci = ResolvePci(addr);
        if (pci != null)
        {
            if (pci.Wrapper is BasePciWrapper wrapper)
            {
                if (wrapper.Cpu == null)
                    wrapper.Cpu = _myCpu;
                wrapper.Write(addr, value, size);
            }
        }
        // или мб ошибку кидать?
    }
}
