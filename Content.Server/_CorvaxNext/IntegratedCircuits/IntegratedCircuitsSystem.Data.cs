using Content.Shared._CorvaxNext.IntegratedCircuits.CircuitComponents;
using Content.Shared._CorvaxNext.IntegratedCircuits.Components;
using Content.Shared._CorvaxNext.IntegratedCircuits.Events;

namespace Content.Server._CorvaxNext.IntegratedCircuits;

public sealed partial class IntegratedCircuitsSystem
{
    private void RegisterDataComponents()
    {
        SubscribeLocalEvent<StringMemoryCircuitComponent, OnCircuitEvent>(ActivateStringMemoryCircuit);
        SubscribeLocalEvent<IntegerMemoryCircuitComponent, OnCircuitEvent>(ActivateIntegerMemoryCircuit);
        SubscribeLocalEvent<DataStorageCircuitComponent, OnCircuitEvent>(ActivateDataStorageCircuit);
    }

    private void ActivateStringMemoryCircuit(EntityUid uid, StringMemoryCircuitComponent component, OnCircuitEvent args)
    {
        if (args.Handled) // no power?
            return;

        if (!TryComp(uid, out IntegratedCircuitComponent? circuit))
            return;

        WriteWire(circuit.Wires[0], component.StringData);
    }

    private void ActivateIntegerMemoryCircuit(EntityUid uid, IntegerMemoryCircuitComponent component, OnCircuitEvent args)
    {
        if (args.Handled) // no power?
            return;

        if (!TryComp(uid, out IntegratedCircuitComponent? circuit))
            return;

        WriteWire(circuit.Wires[0], component.IntegerData);
    }

    private void ActivateDataStorageCircuit(EntityUid uid, DataStorageCircuitComponent component, OnCircuitEvent args)
    {
        if (args.Handled) // no power?
            return;

        if (!TryComp(uid, out IntegratedCircuitComponent? circuit))
            return;

        var isReset = args.EventName == "Reset";

        var count = component.Count;
        var wires = circuit.Wires;
        // на случай если кто-нибудь додумается записать не int
        // ставим по дефолту запись во все провода
        var maxState = int.MaxValue >> (31 - count);
        var mode = wires[0].WireData is int circMode ? circMode : maxState;

        if (mode == 0)
            return;

        var readWiresStart = 1;
        var writeWiresStart = readWiresStart + count;

        var currentState = 1;
        var wireId = 0;
        while (currentState != maxState)
        {
            if ((mode & currentState) == currentState)
            {
                var valueWire = wires[readWiresStart + wireId];

                if (isReset)
                {
                    SetDefaultValue(valueWire);
                }
                else
                {
                    SetDefaultValueIfNull(valueWire);
                    WriteWire(wires[writeWiresStart + wireId], valueWire.WireData);
                }
            }
            currentState <<= 1;
            wireId += 1;
        }
    }
}
