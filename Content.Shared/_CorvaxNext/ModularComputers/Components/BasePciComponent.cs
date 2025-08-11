namespace Content.Shared._CorvaxNext.ModularComputers.Components;

public abstract partial class BasePciComponent : Component
{
    [DataField("builtin")]
    public bool BuiltIn = false;

    [NonSerialized]
    public ulong PciAddressStart;

    [NonSerialized]
    public ulong PciAddressEnd;
}
