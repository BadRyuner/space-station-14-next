using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxNext.ModularComputers.Components;

[NetworkedComponent]
public abstract partial class BasePciComponent : Component
{
    [DataField("builtin")]
    public bool BuiltIn = false;

    [NonSerialized]
    public ulong PciAddressStart;

    [NonSerialized]
    public ulong PciAddressEnd;
}
