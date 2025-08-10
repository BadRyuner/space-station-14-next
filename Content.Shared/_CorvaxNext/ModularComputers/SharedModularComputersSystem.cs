using Content.Shared._CorvaxNext.ModularComputers.Components;
using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using System.Diagnostics.CodeAnalysis;
using Content.Shared._CorvaxNext.ModularComputers.Events;
using Content.Shared.Verbs;

namespace Content.Shared._CorvaxNext.ModularComputers;

public abstract class SharedModularComputersSystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly IEntityManager EntManager = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedToolSystem Tool = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UserInterfaceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<Components.ModularComputerComponent, ComponentStartup>(OnModularStart);
        SubscribeLocalEvent<Components.ModularComputerComponent, GetVerbsEvent<Verb>>(GetModularComputerVerbs);
    }

    private void GetModularComputerVerbs(Entity<Components.ModularComputerComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        args.Verbs.Add(new Verb()
        {
            Text = "Переключить",
            Act = () => RaiseNetworkEvent(new ChangeModularComputerStateEvent(EntManager.GetNetEntity(ent.Owner))),
        });
        args.Verbs.Add(new Verb()
        {
            Text = "Перепрошить",
            Act = () => RaiseNetworkEvent(new CreateLoadProgramUIEvent(EntManager.GetNetEntity(ent.Owner))),
        });
    }

    private void OnModularStart(EntityUid uid, Components.ModularComputerComponent component, ComponentStartup args)
    {
        component.MyOwner = uid;
        component.PciContainer = Container.EnsureContainer<Container>(uid, "pcis");
        component.CpuSlot = Container.EnsureContainer<ContainerSlot>(uid, "cpu");
    }

    public bool TryGetPciComponent<T>(Components.ModularComputerComponent modularComputer, [NotNullWhen(true)] out T? comp, out EntityUid entity) where T : BasePciComponent
    {
        foreach (var c in modularComputer.PciContainer.ContainedEntities)
        {
            if (EntManager.TryGetComponent<T>(c, out comp))
            {
                entity = c;
                return true;
            }
        }
        entity = default;
        comp = null;
        return false;
    }

    public List<BasePciComponent> GetAllPciComponents(EntityUid modularComp)
    {
        var list = new List<BasePciComponent>();
        if (EntManager.TryGetComponent<Components.ModularComputerComponent>(modularComp, out var comp))
        {
            foreach (var c in comp.PciContainer.ContainedEntities)
                if (EntManager.TryGetComponent<BasePciComponent>(c, out var pci))
                    list.Add(pci);
        }
        return list;
    }
}
