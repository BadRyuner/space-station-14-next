using Content.Shared._CorvaxNext.IntegratedCircuits.CircuitComponents;
using Content.Shared._CorvaxNext.IntegratedCircuits.Components;
using Content.Shared._CorvaxNext.IntegratedCircuits.Events;

namespace Content.Server._CorvaxNext.IntegratedCircuits;

public sealed partial class IntegratedCircuitsSystem
{
    private void RegisterLogicComponents()
    {
        SubscribeLocalEvent<CompareValueCircuitComponent, OnCircuitEvent>(ActivateCompareValueCircuit);
    }

    private void ActivateCompareValueCircuit(EntityUid uid, CompareValueCircuitComponent component, OnCircuitEvent args)
    {
        if (args.Handled) // no power?
            return;

        if (!TryComp(uid, out IntegratedCircuitComponent? circuit))
            return;

        var left = circuit.Wires[0].WireData;
        var right = circuit.Wires[1].WireData;

        if (left is string leftStr && right is string rightStr)
        {
            if (leftStr == rightStr)
                RiseEvent(circuit.Events[3], args);
            else
                RiseEvent(circuit.Events[4], args);
            return;
        }

        if (left is IComparable compLeft && right is IComparable compRight && left.GetType() == right.GetType())
        {
            var result = compLeft.CompareTo(compRight);
            if (result == 0) // eq
                RiseEvent(circuit.Events[3], args);
            else if (result > 1) // left gt
                RiseEvent(circuit.Events[1], args);
            else // right gt
                RiseEvent(circuit.Events[2], args);
            return;
        }

        // err
        RiseEvent(circuit.Events[5], args);
    }
}
