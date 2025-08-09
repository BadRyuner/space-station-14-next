using Content.Shared._CorvaxNext.ModularComputers.Emulator;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.ModularComputers.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class PciGpuComponent : BasePciComponent
{
    public const ulong Start = PciCpuComponent.PciEnd + 0x1;
    public const ulong End = Start + 0x99;
    public const ulong GpuSetArgStart = Start + 1;
    public const ulong GpuSetArgEnd = GpuSetArgStart + 15;

    public bool RequireSync = false;

    [DataField, AutoNetworkedField]
    public List<GpuCommand> Commands = new();

    public PciGpuComponent()
    {
        PciAddressStart = Start;
        PciAddressStart = End;
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
