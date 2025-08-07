using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._CorvaxNext.IntegratedCircuits.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class ElectronicAssemblyComponent : Component
{
    [DataField("maxlogicdepth", readOnly: true, required: true)]
    public int MaxLogicDepth;

    [DataField, AutoNetworkedField]
    public bool Open = false;

    [DataField]
    public ProtoId<ToolQualityPrototype> OpeningTool = "Screwing";

    [DataField("screwdriverOpenSound")]
    public SoundSpecifier ScrewdriverOpenSound = new SoundPathSpecifier("/Audio/Machines/screwdriveropen.ogg");

    [DataField("screwdriverCloseSound")]
    public SoundSpecifier ScrewdriverCloseSound = new SoundPathSpecifier("/Audio/Machines/screwdriverclose.ogg");

    [DataField("circuitExtractionSound")]
    public SoundSpecifier CircuitExtractionSound = new SoundPathSpecifier("/Audio/Items/pistol_magout.ogg");

    [DataField("circuitInsertionSound")]
    public SoundSpecifier CircuitInsertionSound = new SoundPathSpecifier("/Audio/Items/pistol_magin.ogg");

    [ViewVariables]
    public Container CircuitContainer;
    public const string DefaultContainerName = "circuits";
}
