using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared._CorvaxNext.IntegratedCircuits;
using Content.Shared._CorvaxNext.IntegratedCircuits.CircuitComponents;
using Content.Shared._CorvaxNext.IntegratedCircuits.Components;
using Content.Shared._CorvaxNext.IntegratedCircuits.Events;
using Robust.Server.GameObjects;

namespace Content.Server._CorvaxNext.IntegratedCircuits;

public sealed class IntegratedCircuitsSystem : SharedIntegratedCircuitsSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntegratedCircuitComponent, ComponentStartup>(OnCircuitStartup);

        SubscribeLocalEvent<SpeakerCircuitComponent, OnCircuitEvent>(ActivateSpeakerCircuit);

        Subs.BuiEvents<ElectronicAssemblyComponent>(ElectronicAssemblyUiKey.Key,
            subs =>
            {
                subs.Event<ConnectWireMessage>(OnConnectWireMessage);
                subs.Event<ConnectEventMessage>(OnConnectEventMessage);
                subs.Event<DisconnectWireMessage>(OnDisconnectWireMessage);
                subs.Event<DisconnectEventMessage>(OnDisconnectEventMessage);
                subs.Event<ChangeStringMemoryCircuitMessage>(OnChangeStringMemoryCircuitMessage);
            });
    }

    private void OnChangeStringMemoryCircuitMessage(EntityUid uid, ElectronicAssemblyComponent component, ChangeStringMemoryCircuitMessage args)
    {
        var entity = _entManager.GetEntity(args.Entity);
        var strComp = _entManager.GetComponent<StringMemoryCircuitComponent>(entity);
        strComp.StringData = args.NewText;
        DirtyField(entity, strComp, nameof(strComp.StringData));
    }

    private void OnConnectWireMessage(EntityUid uid, ElectronicAssemblyComponent component, ConnectWireMessage args)
    {
        var owner = _entManager.GetEntity(args.OwnerCircuit);
        var ownerCircuit = Comp<IntegratedCircuitComponent>(owner);
        var ownerWires = ownerCircuit.Wires;
        var wire = ownerWires.First(wire => wire.Name == args.OwnerWire);

        wire.WriteLinks.Add(new(args.CircuitToConnect, args.WireToConnect));
        DirtyField(owner, ownerCircuit, nameof(ownerCircuit.Wires));
        _ui.ServerSendUiMessage(uid, ElectronicAssemblyUiKey.Key, new UpdateMenuMessage());
    }

    private void OnDisconnectWireMessage(EntityUid uid, ElectronicAssemblyComponent component, DisconnectWireMessage args)
    {
        var circuitEntity = _entManager.GetEntity(args.Circuit);
        var circuitComp = _entManager.GetComponent<IntegratedCircuitComponent>(circuitEntity);
        var links = circuitComp.Wires.First(w => w.Name == args.CircuitWire).WriteLinks;
        var link = links.First(w => w.Circuit == args.DisconnectCircuit && w.WireName == args.DisconnectWire);
        links.Remove(link);
        DirtyField(circuitEntity, circuitComp, nameof(circuitComp.Wires));
        _ui.ServerSendUiMessage(uid, ElectronicAssemblyUiKey.Key, new UpdateMenuMessage());
    }

    private void OnConnectEventMessage(EntityUid uid, ElectronicAssemblyComponent component, ConnectEventMessage args)
    {
        var owner = _entManager.GetEntity(args.OwnerCircuit);
        var ownerCircuit = Comp<IntegratedCircuitComponent>(owner);
        var ownerEvents = ownerCircuit.Events;
        var even = ownerEvents.First(ev => ev.Name == args.OwnerEvent);

        even.EventLinks.Add(new(args.CircuitToConnect, args.EventToConnect));
        DirtyField(owner, ownerCircuit, nameof(ownerCircuit.Events));
        _ui.ServerSendUiMessage(uid, ElectronicAssemblyUiKey.Key, new UpdateMenuMessage());
    }

    private void OnDisconnectEventMessage(EntityUid uid, ElectronicAssemblyComponent component, DisconnectEventMessage args)
    {
        var circuitEntity = _entManager.GetEntity(args.Circuit);
        var circuitComp = _entManager.GetComponent<IntegratedCircuitComponent>(circuitEntity);
        var links = circuitComp.Events.First(w => w.Name == args.CircuitEvent).EventLinks;
        var link = links.First(w => w.Circuit == args.DisconnectCircuit && w.EventName == args.DisconnectEvent);
        links.Remove(link);
        DirtyField(circuitEntity, circuitComp, nameof(circuitComp.Events));
        _ui.ServerSendUiMessage(uid, ElectronicAssemblyUiKey.Key, new UpdateMenuMessage());
    }

    private void OnCircuitStartup(EntityUid uid, IntegratedCircuitComponent component, ComponentStartup args)
    {
        var meta = _entManager.GetComponent<MetaDataComponent>(uid);
        component.Id = $"{meta.EntityName} ({uid.Id})";
        DirtyField(uid, component, nameof(component.Id));
    }

    private void ActivateSpeakerCircuit(EntityUid uid, SpeakerCircuitComponent component, OnCircuitEvent args)
    {
        if (args.EventName != "Say")
            return;

        if (!TryComp(uid, out IntegratedCircuitComponent? circuit))
            return;

        if (circuit.Wires[0].WireData is not string text || string.IsNullOrWhiteSpace(text))
            return;

        if (circuit.Wires[1].WireData is string name)
        {
            _metaDataSystem.SetEntityName(uid, name);
        }

        var assembly = _entManager.GetEntity(circuit.AssemblyOwner);

        _chat.TrySendInGameICMessage(uid, text, InGameICChatType.Speak, ChatTransmitRange.Normal);

        args.Handled = true;
    }
}
