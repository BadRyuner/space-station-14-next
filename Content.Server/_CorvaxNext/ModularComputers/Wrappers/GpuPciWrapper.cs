using Content.Shared._CorvaxNext.ModularComputers.Components;
using Content.Shared._CorvaxNext.ModularComputers.Emulator;

namespace Content.Server._CorvaxNext.ModularComputers.Wrappers;
public sealed class GpuPciWrapper(PciGpuComponent Gpu) : BasePciWrapper
{
    private enum DrawCommands : ulong
    {
        DrawLine = 0x1
    }

    public ulong[] Args = new ulong[16];

    public override ulong Read(ulong address, Bits size)
    {
        return 1;
    }

    public override void Write(ulong address, ulong value, Bits size)
    {
        if (address == PciGpuComponent.Start)
        {
            switch (value)
            {
                case 0x10: // Draw
                    {
                        switch ((DrawCommands)Args[0])
                        {
                            case DrawCommands.DrawLine:
                                {
                                    Gpu.RequireSync = true;
                                    var dram = Cpu.Bus.Dram;
                                    var clrAddr = Args[1];
                                    var clr = new Color((byte)dram.Read8(clrAddr), (byte)dram.Read8(clrAddr+1), (byte)dram.Read8(clrAddr+2), (byte)dram.Read8(clrAddr+3));
                                    Gpu.Commands.Add(new DrawLine(clr, (int)Args[2], (int)Args[3], (int)Args[4], (int)Args[5]));
                                    return;
                                }
                        }
                        return;
                    }
            }
        }
        else if (address >= PciGpuComponent.GpuSetArgStart && address <= PciGpuComponent.GpuSetArgEnd)
        {
            Args[address - PciGpuComponent.GpuSetArgStart] = value;
        }
    }
}
