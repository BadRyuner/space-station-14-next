using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxNext.IntegratedCircuits.CircuitComponents;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class StringMemoryCircuitComponent : Component
{
    [DataField, AutoNetworkedField]
    public string StringData = string.Empty;
}
