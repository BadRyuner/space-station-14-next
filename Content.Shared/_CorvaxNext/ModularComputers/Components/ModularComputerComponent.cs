using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CorvaxNext.ModularComputers;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class ModularComputerComponent : Component
{
    [NonSerialized]
    public EntityUid MyOwner;

    [DataField, AutoNetworkedField]
    public bool IsOn = false;

    [DataField, AutoNetworkedField]
    public bool Open = false;

    [DataField("pcislots", required: true)]
    public int PciSlots;

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

    [NonSerialized]
    public Container PciContainer = null!;

    [NonSerialized]
    public ContainerSlot CpuSlot = null!;
}
