namespace Content.Shared._CorvaxNext.ModularComputers.Components;

[RegisterComponent]
public sealed partial class PciCpuComponent : BasePciComponent
{
    public const int PciBase = 0xf00_0000;
    internal const ulong PciStart = PciBase;
    internal const ulong PciEnd = PciStart + 0x100 - 0x1;

    [NonSerialized]
    public EntityUid? ModularComputer;

    [DataField("ram", required: true)]
    public int RamSize;

    [DataField("ips", required: true)]
    public int InstructionsPerSecond;

    [DataField("ppi", required: true)]
    public float PowerPerInstruction;

    public float AccumulatedTime = 0;

    public float RequiredTime => 1 / InstructionsPerSecond;

    [NonSerialized]
    public object? Cpu;

    public PciCpuComponent()
    {
        PciAddressStart = PciStart;
        PciAddressEnd = PciEnd;
    }
}
