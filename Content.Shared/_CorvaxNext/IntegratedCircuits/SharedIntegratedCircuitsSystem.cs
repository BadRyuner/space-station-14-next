using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Content.Shared._CorvaxNext.IntegratedCircuits.CircuitComponents;
using Content.Shared._CorvaxNext.IntegratedCircuits.Components;
using Content.Shared._CorvaxNext.IntegratedCircuits.Events;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Tools.Systems;
using Content.Shared.UserInterface;
using Content.Shared.Wires;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.IntegratedCircuits;

public abstract class SharedIntegratedCircuitsSystem : EntitySystem
{
    [Robust.Shared.IoC.Dependency] protected readonly SharedContainerSystem Container = default!;
    [Robust.Shared.IoC.Dependency] protected readonly IEntityManager EntManager = default!;
    [Robust.Shared.IoC.Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Robust.Shared.IoC.Dependency] protected readonly SharedToolSystem Tool = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ElectronicAssemblyComponent, ComponentStartup>(OnAssemblyStartup);
        SubscribeLocalEvent<ElectronicAssemblyComponent, InteractUsingEvent>(OnInteract);
    }

    private void OnAssemblyStartup(EntityUid uid, ElectronicAssemblyComponent component, ComponentStartup args)
    {
        component.CircuitContainer = Container.EnsureContainer<Container>(uid, ElectronicAssemblyComponent.DefaultContainerName);
    }

    private void OnInteract(Entity<ElectronicAssemblyComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (Tool.HasQuality(args.Used, ent.Comp.OpeningTool))
        {
            ent.Comp.Open = !ent.Comp.Open;
            DirtyField(ent.Owner, ent.Comp, nameof(ent.Comp.Open), null);

            var ev = new PanelChangedEvent(ent.Comp.Open);
            RaiseLocalEvent(ent, ref ev);

            var sound = ent.Comp.Open ? ent.Comp.ScrewdriverOpenSound : ent.Comp.ScrewdriverCloseSound;
            Audio.PlayPredicted(sound, args.Target, args.User);
        }
        else if (ent.Comp.Open && TryComp<IntegratedCircuitComponent>(args.Used, out var circuitComp))
        {
            circuitComp.AssemblyOwner = EntManager.GetNetEntity(args.Target);
            DirtyField(args.Used, circuitComp, nameof(circuitComp.AssemblyOwner), null);

            Container.Insert(args.Used, ent.Comp.CircuitContainer);
            Audio.PlayPredicted(ent.Comp.CircuitInsertionSound, args.Target, args.User);
        }
        else
            return;

        args.Handled = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected List<(EntityUid, IntegratedCircuitComponent)> GetAllCircuits(ElectronicAssemblyComponent component)
    {
        var list = new List<(EntityUid, IntegratedCircuitComponent)>(component.CircuitContainer.Count);
        foreach (var circuitEntity in component.CircuitContainer.ContainedEntities)
        {
            if (EntManager.TryGetComponent(circuitEntity, out IntegratedCircuitComponent? integratedCircuit))
            {
                list.Add((circuitEntity, integratedCircuit));
            }
        }
        return list;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryGetCircuit<TC>(Entity<ElectronicAssemblyComponent> ent, [NotNullWhen(true)] out TC? component, [NotNullWhen(true)] out EntityUid? circuit) where TC : Component
    {
        foreach (var circuitEntity in ent.Comp.CircuitContainer.ContainedEntities)
        {
            if (EntManager.TryGetComponent(circuitEntity, out component))
            {
                circuit = circuitEntity;
                return true;
            }
        }

        component = null;
        circuit = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void WriteWire(CircuitWire outputWire, object? data)
    {
        foreach (var writeTarget in outputWire.WriteLinks)
        {
            var targetEntity = EntManager.GetEntity(writeTarget.Circuit);
            if (!EntManager.TryGetComponent(targetEntity, out IntegratedCircuitComponent? targetCircuit))
                continue;

            var wire = targetCircuit.Wires.FirstOrDefault(wire => wire.Name == writeTarget.WireName);
            if (wire == null)
                continue;

            wire.WireData = data;
            DirtyField(targetEntity, targetCircuit, nameof(targetCircuit.Wires), null);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void RiseEvent(CircuitEvent even, int currentDepth, int maxDepth)
    {
        foreach (var eventLink in even.EventLinks)
        {
            var subscribedCircuit = EntManager.GetEntity(eventLink.Circuit);
            var raisedEvent = new OnCircuitEvent(eventLink.EventName, currentDepth, maxDepth);
            RaiseLocalEvent(subscribedCircuit, raisedEvent);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void RiseEvent(CircuitEvent even, OnCircuitEvent prevEvent)
    {
        if (prevEvent.CurrentDepth == prevEvent.MaxDepth)
            return;

        RiseEvent(even, prevEvent.CurrentDepth, prevEvent.MaxDepth);
    }

    protected void SetDefaultValueIfNull(CircuitWire wire)
    {
        if (wire.WireData == null)
        {
            // мистер интегральщик поленился/забыл установить дефолтные значения
            SetDefaultValue(wire);
        }
    }

    protected void SetDefaultValue(CircuitWire wire)
    {
        wire.WireData = wire.WireType switch
        {
            WireType.Integer => 0,
            WireType.Float => 0,
            WireType.String => string.Empty,
            _ => null, // or maybe throw demencia error?
        };
    }
}

[NetSerializable, Serializable]
public enum ElectronicAssemblyUiKey : byte
{
    Key
}
