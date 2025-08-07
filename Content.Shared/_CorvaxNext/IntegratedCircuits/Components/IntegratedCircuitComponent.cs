using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared._CorvaxNext.IntegratedCircuits.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class IntegratedCircuitComponent : Component
{
    [DataField("powerperuse", readOnly: true, required: true)]
    public float PowerUsagePerUse;

    [DataField, AutoNetworkedField]
    public string Id = string.Empty;

    [DataField, AutoNetworkedField]
    public NetEntity AssemblyOwner;

    [DataField("wires"), AutoNetworkedField]
    public List<CircuitWire> Wires = [];

    [DataField("events"), AutoNetworkedField]
    public List<CircuitEvent> Events = [];
}
