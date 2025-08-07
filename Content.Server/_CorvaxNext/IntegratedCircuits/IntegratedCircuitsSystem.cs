using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.PowerCell;
using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Speech;
using Content.Shared._CorvaxNext.IntegratedCircuits;
using Content.Shared._CorvaxNext.IntegratedCircuits.CircuitComponents;
using Content.Shared._CorvaxNext.IntegratedCircuits.Components;
using Content.Shared._CorvaxNext.IntegratedCircuits.Events;
using Content.Shared.Interaction.Events;
using Content.Shared.Radio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._CorvaxNext.IntegratedCircuits;

public sealed partial class IntegratedCircuitsSystem : SharedIntegratedCircuitsSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly PowerCellSystem _powerCellSystem = default!;
    [Dependency] private readonly RadioSystem _radioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntegratedCircuitComponent, ComponentStartup>(OnCircuitStartup);
        SubscribeLocalEvent<IntegratedCircuitComponent, OnCircuitEvent>(ConsumePower);

        SubscribeLocalEvent<ElectronicAssemblyComponent, UseInHandEvent>(OnInteractWithHands);
        SubscribeLocalEvent<ElectronicAssemblyComponent, DroppedEvent>(OnDropped);

        SubscribeLocalEvent<IntegratedCircuitComponent, ListenEvent>(OnMicrophoneActivated);
        SubscribeLocalEvent<SpeakerCircuitComponent, OnCircuitEvent>(ActivateSpeakerCircuit);
        SubscribeLocalEvent<ValueConverterCircuitComponent, OnCircuitEvent>(ActivateValueConverter);

        RegisterDataComponents();
        RegisterLogicComponents();
        RegisterMathComponents();

        Subs.BuiEvents<ElectronicAssemblyComponent>(ElectronicAssemblyUiKey.Key,
            subs =>
            {
                subs.Event<ConnectWireMessage>(OnConnectWireMessage);
                subs.Event<ConnectEventMessage>(OnConnectEventMessage);
                subs.Event<DisconnectWireMessage>(OnDisconnectWireMessage);
                subs.Event<DisconnectEventMessage>(OnDisconnectEventMessage);
                subs.Event<ChangeStringMemoryCircuitMessage>(OnChangeStringMemoryCircuitMessage);
                subs.Event<ChangeIntegerMemoryCircuitMessage>(OnChangeIntegerMemoryCircuitMessage);
                subs.Event<RemoveCircuitMessage>(OnRemoveCircuitMessage);
            });
    }

    private void OnCircuitStartup(EntityUid uid, IntegratedCircuitComponent component, ComponentStartup args)
    {
        var meta = EntManager.GetComponent<MetaDataComponent>(uid);
        component.Id = $"{meta.EntityName} ({uid.Id})";
        DirtyField(uid, component, nameof(component.Id));
    }

    private void ConsumePower(Entity<IntegratedCircuitComponent> ent, ref OnCircuitEvent args)
    {
        var assembly = EntManager.GetEntity(ent.Comp.AssemblyOwner);
        args.Handled = !_powerCellSystem.TryUseCharge(assembly, ent.Comp.PowerUsagePerUse);
    }

    private void OnInteractWithHands(Entity<ElectronicAssemblyComponent> ent, ref UseInHandEvent args)
    {
        if (!ent.Comp.Open && TryGetCircuit(ent, out ButtonCircuitComponent? buttonCircuit, out var buttonEntity) && TryComp(buttonEntity, out IntegratedCircuitComponent? circuitComp))
        {
            var assembly = EntManager.GetEntity(circuitComp.AssemblyOwner);
            if (_powerCellSystem.TryUseCharge(assembly, circuitComp.PowerUsagePerUse))
                return;

            var onClickEvent = circuitComp.Events[0];
            RiseEvent(onClickEvent, 0, ent.Comp.MaxLogicDepth);
        }
    }

    private void OnDropped(Entity<ElectronicAssemblyComponent> ent, ref DroppedEvent args)
    {
        if (!ent.Comp.Open && TryGetCircuit(ent, out DropDetectorCircuitComponent? dropCircuit, out var circuit) && TryComp(circuit, out IntegratedCircuitComponent? circuitComp))
        {
            var assembly = EntManager.GetEntity(circuitComp.AssemblyOwner);
            if (_powerCellSystem.TryUseCharge(assembly, circuitComp.PowerUsagePerUse))
                return;

            var onDroppedEvent = circuitComp.Events[0];
            RiseEvent(onDroppedEvent, 0, ent.Comp.MaxLogicDepth);
        }
    }

    private void OnMicrophoneActivated(EntityUid uid, IntegratedCircuitComponent component, ListenEvent args)
    {
        if (!component.AssemblyOwner.IsValid())
            return;

        if (EntManager.TryGetComponent(args.Source, out IntegratedCircuitComponent? _))
            return; // no no no mr.InfinityLoop

        var assembly = EntManager.GetEntity(component.AssemblyOwner);
        if (!_powerCellSystem.TryUseCharge(assembly, component.PowerUsagePerUse))
            return;

        WriteWire(component.Wires[0], args.Message);
        RiseEvent(component.Events[0], 0, EntManager.GetComponent<ElectronicAssemblyComponent>(EntManager.GetEntity(component.AssemblyOwner)).MaxLogicDepth);
    }

    private void ActivateSpeakerCircuit(EntityUid uid, SpeakerCircuitComponent component, OnCircuitEvent args)
    {
        if (args.Handled) // no power?
            return;

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

        if (TryComp(uid, out RadioSpeakerComponent? radioSpeaker))
            _radioSystem.SendRadioMessage(uid, text, new ProtoId<RadioChannelPrototype>(radioSpeaker.Channels.ElementAt(0)), uid);
        else
            _chat.TrySendInGameICMessage(uid, text, InGameICChatType.Speak, ChatTransmitRange.Normal);
    }

    private void ActivateValueConverter(EntityUid uid, ValueConverterCircuitComponent component, OnCircuitEvent args)
    {
        if (args.Handled) // no power?
            return;

        if (!TryComp(uid, out IntegratedCircuitComponent? circuit))
            return;

        var input = circuit.Wires[0].WireData;

        if (input == null)
            goto onError;

        var output = circuit.Wires[1];

        switch (output.WireType)
        {
            case WireType.Integer when input is int:
                WriteWire(output, input);
                break;
            case WireType.Integer when input is float @float:
                WriteWire(output, (int)@float);
                break;
            case WireType.Integer when input is string str:
                if (int.TryParse(str, out var parsedInt))
                    WriteWire(output, parsedInt);
                else
                    goto onError;
                break;
            case WireType.Float when input is float:
                WriteWire(output, input);
                break;
            case WireType.Float when input is int @int:
                WriteWire(output, (float)@int);
                break;
            case WireType.Float when input is string str:
                if (float.TryParse(str, out var parsedFloat))
                    WriteWire(output, parsedFloat);
                else
                    goto onError;
                break;
            case WireType.String when input is string:
                WriteWire(output, input);
                break;
            case WireType.String when input is int or float:
                WriteWire(output, input.ToString());
                break;
            default: // no references & lists mr.Fish
                goto onError;
        }

        return;

        onError:
        RiseEvent(circuit.Events[1], args);
    }

    private void OnChangeStringMemoryCircuitMessage(EntityUid uid, ElectronicAssemblyComponent component, ChangeStringMemoryCircuitMessage args)
    {
        var entity = EntManager.GetEntity(args.Entity);
        var strComp = EntManager.GetComponent<StringMemoryCircuitComponent>(entity);
        strComp.StringData = args.NewText;
        DirtyField(entity, strComp, nameof(strComp.StringData));
    }

    private void OnChangeIntegerMemoryCircuitMessage(EntityUid uid, ElectronicAssemblyComponent component, ChangeIntegerMemoryCircuitMessage args)
    {
        var entity = EntManager.GetEntity(args.Entity);
        var intComp = EntManager.GetComponent<IntegerMemoryCircuitComponent>(entity);
        intComp.IntegerData = args.NewInt;
        DirtyField(entity, intComp, nameof(intComp.IntegerData));
    }

    private void OnConnectWireMessage(EntityUid uid, ElectronicAssemblyComponent component, ConnectWireMessage args)
    {
        var owner = EntManager.GetEntity(args.OwnerCircuit);
        var ownerCircuit = Comp<IntegratedCircuitComponent>(owner);
        var ownerWires = ownerCircuit.Wires;
        var wire = ownerWires.First(wire => wire.Name == args.OwnerWire);

        wire.WriteLinks.Add(new(args.CircuitToConnect, args.WireToConnect));
        DirtyField(owner, ownerCircuit, nameof(ownerCircuit.Wires));
        _ui.ServerSendUiMessage(uid, ElectronicAssemblyUiKey.Key, new UpdateMenuMessage());
    }

    private void OnDisconnectWireMessage(EntityUid uid, ElectronicAssemblyComponent component, DisconnectWireMessage args)
    {
        var circuitEntity = EntManager.GetEntity(args.Circuit);
        var circuitComp = EntManager.GetComponent<IntegratedCircuitComponent>(circuitEntity);
        var links = circuitComp.Wires.First(w => w.Name == args.CircuitWire).WriteLinks;
        var link = links.First(w => w.Circuit == args.DisconnectCircuit && w.WireName == args.DisconnectWire);
        links.Remove(link);
        DirtyField(circuitEntity, circuitComp, nameof(circuitComp.Wires));
        _ui.ServerSendUiMessage(uid, ElectronicAssemblyUiKey.Key, new UpdateMenuMessage());
    }

    private void OnConnectEventMessage(EntityUid uid, ElectronicAssemblyComponent component, ConnectEventMessage args)
    {
        var owner = EntManager.GetEntity(args.OwnerCircuit);
        var ownerCircuit = Comp<IntegratedCircuitComponent>(owner);
        var ownerEvents = ownerCircuit.Events;
        var even = ownerEvents.First(ev => ev.Name == args.OwnerEvent);

        even.EventLinks.Add(new(args.CircuitToConnect, args.EventToConnect));
        DirtyField(owner, ownerCircuit, nameof(ownerCircuit.Events));
        _ui.ServerSendUiMessage(uid, ElectronicAssemblyUiKey.Key, new UpdateMenuMessage());
    }

    private void OnDisconnectEventMessage(EntityUid uid, ElectronicAssemblyComponent component, DisconnectEventMessage args)
    {
        var circuitEntity = EntManager.GetEntity(args.Circuit);
        var circuitComp = EntManager.GetComponent<IntegratedCircuitComponent>(circuitEntity);
        var links = circuitComp.Events.First(w => w.Name == args.CircuitEvent).EventLinks;
        var link = links.First(w => w.Circuit == args.DisconnectCircuit && w.EventName == args.DisconnectEvent);
        links.Remove(link);
        DirtyField(circuitEntity, circuitComp, nameof(circuitComp.Events));
        _ui.ServerSendUiMessage(uid, ElectronicAssemblyUiKey.Key, new UpdateMenuMessage());
    }

    private void OnRemoveCircuitMessage(EntityUid uid, ElectronicAssemblyComponent component, RemoveCircuitMessage args)
    {
        var entityUid = EntManager.GetEntity(args.Circuit);
        var coordinates = EntManager.GetComponent<TransformComponent>(entityUid);
        var meta = EntManager.GetComponent<MetaDataComponent>(entityUid);
        var entity = new Entity<TransformComponent?, MetaDataComponent?>(entityUid, coordinates, meta);
        Container.Remove(entity, component.CircuitContainer);

        var circuit = EntManager.GetComponent<IntegratedCircuitComponent>(entityUid);
        circuit.Wires.ForEach(w => w.WriteLinks.Clear());
        circuit.Events.ForEach(e => e.EventLinks.Clear());

        if (circuit.Wires.Any(w => w.WireAccess == WireAccess.In) ||
            circuit.Events.Any(e => e.EventType == EventType.PulseIn))
        {
            foreach (var (ent, integratedCircuitComponent) in GetAllCircuits(component))
            {
                bool updated = false;
                foreach (var circuitWire in integratedCircuitComponent.Wires)
                {
                    for (var i = 0; i < circuitWire.WriteLinks.Count; i++)
                    {
                        var link = circuitWire.WriteLinks[i];
                        if (link.Circuit == args.Circuit)
                        {
                            circuitWire.WriteLinks.RemoveAt(i);
                            updated = true;
                            i--;
                        }
                    }
                }
                if (updated)
                    DirtyField(ent, integratedCircuitComponent, nameof(integratedCircuitComponent.Wires));

                updated = false;
                foreach (var circuitEvent in integratedCircuitComponent.Events)
                {
                    for (var i = 0; i < circuitEvent.EventLinks.Count; i++)
                    {
                        var link = circuitEvent.EventLinks[i];
                        if (link.Circuit == args.Circuit)
                        {
                            circuitEvent.EventLinks.RemoveAt(i);
                            updated = true;
                            i--;
                        }
                    }
                }
                if (updated)
                    DirtyField(ent, integratedCircuitComponent, nameof(integratedCircuitComponent.Events));
            }
        }

        _ui.ServerSendUiMessage(uid, ElectronicAssemblyUiKey.Key, new UpdateMenuMessage(true));
    }
}
