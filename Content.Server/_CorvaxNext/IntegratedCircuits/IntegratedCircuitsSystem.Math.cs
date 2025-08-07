using Content.Shared._CorvaxNext.IntegratedCircuits.CircuitComponents;
using Content.Shared._CorvaxNext.IntegratedCircuits.Components;
using Content.Shared._CorvaxNext.IntegratedCircuits.Events;

namespace Content.Server._CorvaxNext.IntegratedCircuits;

public sealed partial class IntegratedCircuitsSystem
{
    private void RegisterMathComponents()
    {
        SubscribeLocalEvent<MathCircuitComponent, OnCircuitEvent>(ActivateMathCircuitComponent);
    }

    private void ActivateMathCircuitComponent(EntityUid uid, MathCircuitComponent component, OnCircuitEvent args)
    {
        if (args.Handled) // no power?
            return;

        if (!TryComp(uid, out IntegratedCircuitComponent? circuit))
            return;

        var left = circuit.Wires[0];
        var right = circuit.Wires[1];
        if (left.WireType == WireType.Integer)
        {
            var leftInt = (int)left.WireData!;
            var rightInt = (int?)right.WireData ?? 0;

            if (component.Mode == MathCircuitMode.Sqrt)
            {
                WriteWire(circuit.Wires[1], (int)MathF.Sqrt(leftInt));
            }
            else
            {
                WriteWire(circuit.Wires[2],
                    component.Mode switch
                {
                    MathCircuitMode.Add => leftInt + rightInt!,
                    MathCircuitMode.Sub => leftInt - rightInt!,
                    MathCircuitMode.Mul => leftInt * rightInt!,
                    MathCircuitMode.Div => leftInt / rightInt!,
                    MathCircuitMode.Pow => (int)MathF.Pow(leftInt, rightInt),
                    _ => throw new ArgumentOutOfRangeException()
                });
            }
        }
        else if (left.WireType == WireType.Float)
        {
            var leftFloat = (float)left.WireData!;
            var rightFloat = (float?)right.WireData ?? 0;

            if (component.Mode == MathCircuitMode.Sqrt)
            {
                WriteWire(circuit.Wires[1], MathF.Sqrt(leftFloat));
            }
            else
            {
                WriteWire(circuit.Wires[2],
                    component.Mode switch
                {
                    MathCircuitMode.Add => leftFloat + rightFloat!,
                    MathCircuitMode.Sub => leftFloat - rightFloat!,
                    MathCircuitMode.Mul => leftFloat * rightFloat!,
                    MathCircuitMode.Div => leftFloat / rightFloat!,
                    MathCircuitMode.Pow => MathF.Pow(leftFloat, rightFloat),
                    _ => throw new ArgumentOutOfRangeException()
                });
            }
        }
    }
}
