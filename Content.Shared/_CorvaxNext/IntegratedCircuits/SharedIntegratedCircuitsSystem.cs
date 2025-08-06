using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._CorvaxNext.IntegratedCircuits.CircuitComponents;
using Content.Shared._CorvaxNext.IntegratedCircuits.Components;
using Content.Shared._CorvaxNext.IntegratedCircuits.Events;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Tools.Systems;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.IntegratedCircuits;

public abstract class SharedIntegratedCircuitsSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ActivatableUISystem _activatableUI = default!;
    [Dependency] protected readonly IEntityManager _entManager = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedToolSystem Tool = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ElectronicAssemblyComponent, ComponentStartup>(OnAssemblyStartup);
        SubscribeLocalEvent<ElectronicAssemblyComponent, InteractUsingEvent>(OnInteract);
        SubscribeLocalEvent<ElectronicAssemblyComponent, UseInHandEvent>(OnInteractWithHands);

        SubscribeLocalEvent<StringMemoryCircuitComponent, OnCircuitEvent>(ActivateStringMemoryCircuit);
    }

    private void OnInteractWithHands(Entity<ElectronicAssemblyComponent> ent, ref UseInHandEvent args)
    {
        if (!ent.Comp.Open && TryGetCircuit(ent, out ButtonCircuitComponent? buttonCircuit, out var buttonEntity) && TryComp(buttonEntity, out IntegratedCircuitComponent? circuitComp))
        {
            var onClickEvent = circuitComp.Events[0];
            foreach (var eventLink in onClickEvent.EventLinks)
            {
                var subscribedCircuit = _entManager.GetEntity(eventLink.Circuit);
                var raisedEvent = new OnCircuitEvent(eventLink.EventName);
                RaiseLocalEvent(subscribedCircuit, raisedEvent);
            }
        }
    }

    private void ActivateStringMemoryCircuit(EntityUid uid, StringMemoryCircuitComponent component, OnCircuitEvent args)
    {
        if (args.EventName != "Write")
            return;

        if (!TryComp(uid, out IntegratedCircuitComponent? circuit))
            return;

        var outputWire = circuit.Wires[0];
        foreach (var writeTarget in outputWire.WriteLinks)
        {
            var targetEntity = _entManager.GetEntity(writeTarget.Circuit);
            if (!_entManager.TryGetComponent(targetEntity, out IntegratedCircuitComponent? targetCircuit))
                continue;

            var wire = targetCircuit.Wires.FirstOrDefault(wire => wire.Name == writeTarget.WireName);
            if (wire == null)
                continue;

            wire.WireData = component.StringData;
            DirtyField(targetEntity, targetCircuit, nameof(targetCircuit.Wires), null);
        }

        args.Handled = true;
    }

    private void OnAssemblyStartup(EntityUid uid, ElectronicAssemblyComponent component, ComponentStartup args)
    {
        component.CircuitContainer = _container.EnsureContainer<Container>(uid, ElectronicAssemblyComponent.DefaultContainerName);
    }

    private void OnInteract(Entity<ElectronicAssemblyComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (Tool.HasQuality(args.Used, ent.Comp.OpeningTool))
        {
            ent.Comp.Open = !ent.Comp.Open;
            DirtyField(ent.Owner, ent.Comp, nameof(ent.Comp.Open), null);

            var sound = ent.Comp.Open ? ent.Comp.ScrewdriverOpenSound : ent.Comp.ScrewdriverCloseSound;
            Audio.PlayPredicted(sound, args.Target, args.User);
        }
        else if (ent.Comp.Open && TryComp<IntegratedCircuitComponent>(args.Used, out var circuitComp))
        {
            circuitComp.AssemblyOwner = _entManager.GetNetEntity(args.Used);
            DirtyField(args.Used, circuitComp, nameof(circuitComp.AssemblyOwner), null);

            _container.Insert(args.Used, ent.Comp.CircuitContainer);
            Audio.PlayPredicted(ent.Comp.CircuitInsertionSound, args.Target, args.User);
        }
        else
            return;

        args.Handled = true;
    }

    private bool TryGetCircuit<TC>(Entity<ElectronicAssemblyComponent> ent, [NotNullWhen(true)] out TC? component, [NotNullWhen(true)] out EntityUid? circuit) where TC : Component
    {
        foreach (var circuitEntity in ent.Comp.CircuitContainer.ContainedEntities)
        {
            if (_entManager.TryGetComponent(circuitEntity, out component))
            {
                circuit = circuitEntity;
                return true;
            }
        }

        component = null;
        circuit = null;
        return false;
    }
}

[NetSerializable, Serializable]
public enum ElectronicAssemblyUiKey : byte
{
    Key
}
