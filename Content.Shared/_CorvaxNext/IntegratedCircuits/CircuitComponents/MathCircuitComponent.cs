namespace Content.Shared._CorvaxNext.IntegratedCircuits.CircuitComponents;

[RegisterComponent]
public sealed partial class MathCircuitComponent : Component
{
    [DataField("mode", readOnly: true, required: true)]
    public MathCircuitMode Mode;
}

public enum MathCircuitMode : byte
{
    Add,
    Sub,
    Mul,
    Div,
    Sqrt,
    Pow,
}
