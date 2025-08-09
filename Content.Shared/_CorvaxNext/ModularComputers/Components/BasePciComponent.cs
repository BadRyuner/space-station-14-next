using Content.Shared._CorvaxNext.ModularComputers.Emulator;

namespace Content.Shared._CorvaxNext.ModularComputers.Components;

public abstract partial class BasePciComponent : Component
{
    [DataField("builtin")]
    public bool BuiltIn = false;

    [NonSerialized]
    public object? LinkedCpu;

    [NonSerialized]
    public object? Wrapper;

    public ulong PciAddressStart;
    public ulong PciAddressEnd;
}
