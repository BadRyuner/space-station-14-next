using Content.Shared._CorvaxNext.ModularComputers.Emulator;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.ModularComputers.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class PciGpuComponent : BasePciComponent, IPciComponent
{
    public const ulong Start = 0xf00_0100;
    public const ulong End = Start + 0x99;
    public const ulong GpuSetArgStart = Start + 1;
    public const ulong GpuSetArgEnd = GpuSetArgStart + 15;

    [NonSerialized]
    public bool RequireSync = false;

    [DataField, AutoNetworkedField]
    public List<GpuCommand> Commands = new();

    public PciGpuComponent()
    {
        PciAddressStart = Start;
        PciAddressEnd = End;
    }

    private enum DrawCommands : ulong
    {
        DrawLine = 0x1
    }

    [NonSerialized]
    public ulong[] Args = new ulong[16];

    public ulong Read(Dram dram, ulong address, Bits size)
    {
        return 1;
    }

    public void Write(Dram dram, ulong address, ulong value, Bits size)
    {
        if (address == Start)
        {
            switch (value)
            {
                case 0x10: // Draw
                {
                    switch ((DrawCommands)Args[0])
                    {
                        case DrawCommands.DrawLine:
                        {
                            RequireSync = true;
                            var clrAddr = Args[1];
                            var clr = new Color((byte)dram.Read8(clrAddr), (byte)dram.Read8(clrAddr+1), (byte)dram.Read8(clrAddr+2), (byte)dram.Read8(clrAddr+3));
                            Commands.Add(new DrawLine(clr, (int)Args[2], (int)Args[3], (int)Args[4], (int)Args[5]));
                            return;
                        }
                    }
                    return;
                }
            }
        }
        else if (address >= GpuSetArgStart && address <= GpuSetArgEnd)
        {
            Args[address - GpuSetArgStart] = value;
        }
    }
}

[NetSerializable, Serializable]
public abstract class GpuCommand;

[NetSerializable, Serializable]
public sealed class DrawLine(Color color, int x1, int y1, int x2, int y2) : GpuCommand
{
    public Color Color { get; set; } = color;
    public int X1 { get; set; } = x1;
    public int X2 { get; set; } = x2;
    public int Y1 { get; set; } = y1;
    public int Y2 { get; set; } = y2;
}
