using Content.Shared._CorvaxNext.IntegratedCircuits.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxNext.IntegratedCircuits.CircuitComponents;

[RegisterComponent]
public sealed partial class DataStorageCircuitComponent : Component
{
    [DataField("count", readOnly: true, required: true)]
    public int Count;

    [DataField("datatype", readOnly: true, required: true)]
    public WireType DataType;
}
