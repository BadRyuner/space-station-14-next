using System.Numerics;
using System.Runtime.InteropServices;
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
        DrawLine = 0x1,
        DrawDottedLine = 0x2,
        DrawCircle = 0x3,
        DrawRect = 0x4,
        DrawString = 0x5,
        DrawEntity = 0x6,

        Clear = 0x225,
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
                    RequireSync = true;
                    switch ((DrawCommands)Args[0])
                    {
                        case DrawCommands.DrawLine:
                        {
                            var clrAddr = Args[1];
                            var clr = new Color((byte)dram.Read8(clrAddr), (byte)dram.Read8(clrAddr+1), (byte)dram.Read8(clrAddr+2), (byte)dram.Read8(clrAddr+3));
                            Commands.Add(new DrawLine(clr, new((int)Args[2], (int)Args[3]), new((int)Args[4], (int)Args[5])));
                            return;
                        }
                        case DrawCommands.DrawDottedLine:
                        {
                            var clrAddr = Args[1];
                            var clr = new Color((byte)dram.Read8(clrAddr), (byte)dram.Read8(clrAddr+1), (byte)dram.Read8(clrAddr+2), (byte)dram.Read8(clrAddr+3));
                            var offset = (float)new Emulator.BitConverter(Args[6]).Double;
                            var dashSize = (float)new Emulator.BitConverter(Args[7]).Double;
                            var gapSize = (float)new Emulator.BitConverter(Args[8]).Double;
                            Commands.Add(new DrawDottedLine(clr, new((int)Args[2], (int)Args[3]), new((int)Args[4], (int)Args[5]), offset, dashSize, gapSize));
                            return;
                        }
                        case DrawCommands.DrawCircle:
                        {
                            var clrAddr = Args[1];
                            var clr = new Color((byte)dram.Read8(clrAddr), (byte)dram.Read8(clrAddr+1), (byte)dram.Read8(clrAddr+2), (byte)dram.Read8(clrAddr+3));
                            var radius = (float)new Emulator.BitConverter(Args[4]).Double;
                            Commands.Add(new DrawCircle(new((int)Args[2], (int)Args[3]), radius, clr, Args[5] == 1));
                            return;
                        }
                        case DrawCommands.DrawRect:
                        {
                            var clrAddr = Args[1];
                            var clr = new Color((byte)dram.Read8(clrAddr), (byte)dram.Read8(clrAddr+1), (byte)dram.Read8(clrAddr+2), (byte)dram.Read8(clrAddr+3));
                            Commands.Add(new DrawRect(new((int)Args[2], (int)Args[3], (int)Args[4], (int)Args[5]), clr, Args[6] == 1));
                            return;
                        }
                        case DrawCommands.DrawEntity:
                        {
                            Commands.Add(new DrawEntity(new((int)Args[1]), new((int)Args[2], (int)Args[3]), new((int)Args[4], (int)Args[5])));
                            return;
                        }

                        case DrawCommands.DrawString:
                        {
                            var clrAddr = Args[1];
                            var clr = new Color((byte)dram.Read8(clrAddr), (byte)dram.Read8(clrAddr+1), (byte)dram.Read8(clrAddr+2), (byte)dram.Read8(clrAddr+3));
                            var scale = (float)new Emulator.BitConverter(Args[5]).Double;
                            Commands.Add(new DrawString(new((int)Args[2], (int)Args[3]), dram.ReadUTF8NullTerminatedText(Args[4]), scale, clr));
                            return;
                        }
                        case DrawCommands.Clear:
                        {
                            Commands.Clear();
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
public sealed class DrawLine(Color color, Vector2 from, Vector2 to) : GpuCommand
{
    public Color Color { get; set; } = color;

    public Vector2 From { get; set; } = from;

    public Vector2 To { get; set; } = to;
}

[NetSerializable, Serializable]
public sealed class DrawDottedLine(Color color, Vector2 from, Vector2 to, float offset, float dashSize, float gapSize) : GpuCommand
{
    public Color Color { get; set; } = color;

    public Vector2 From { get; set; } = from;

    public Vector2 To { get; set; } = to;

    public float Offset { get; set; } = offset;

    public float DashSize { get; set; } = dashSize;

    public float GapSize { get; set; } = gapSize;
}


[NetSerializable, Serializable]
public sealed class DrawCircle(Vector2 position, float radius, Color color, bool filled) : GpuCommand
{
    public Vector2 Position { get; set; } = position;
    public float Radius { get; set; } = radius;
    public Color Color { get; set; } = color;
    public bool Filled { get; set; } = filled;
}

[NetSerializable, Serializable]
public sealed class DrawRect(UIBox2 box, Color color, bool filled) : GpuCommand
{
    public UIBox2 Box { get; set; } = box;
    public Color Color { get; set; } = color;
    public bool Filled { get; set; } = filled;
}

[NetSerializable, Serializable]
public sealed class DrawString(Vector2 position, string text, float scale, Color color) : GpuCommand
{
    public Vector2 Position { get; set; } = position;
    public string Text { get; set; } = text;
    public float Scale { get; set; } = scale;
    public Color Color { get; set; } = color;
}

[NetSerializable, Serializable]
public sealed class DrawEntity(NetEntity entity, Vector2 position, Vector2 scale) : GpuCommand
{
    public NetEntity Entity { get; set; } = entity;

    public Vector2 Position { get; set; } = position;

    public Vector2 Scale { get; set; } = scale;
}
