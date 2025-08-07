using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxNext.IntegratedCircuits.CircuitComponents;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class IntegerMemoryCircuitComponent : Component
{
    [DataField, AutoNetworkedField]
    public int IntegerData = 0;
}
