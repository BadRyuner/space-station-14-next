using Content.Server._CorvaxNext.ModularComputers.Components;
using Content.Server._CorvaxNext.ModularComputers.Emulator;
using Content.Server.PowerCell;
using Content.Shared._CorvaxNext.ModularComputers;
using Content.Shared._CorvaxNext.ModularComputers.Components;
using Content.Shared._CorvaxNext.ModularComputers.Events;
using Content.Shared._CorvaxNext.ModularComputers.Messages;
using Content.Shared.Interaction;

namespace Content.Server._CorvaxNext.ModularComputers;

public sealed class ModularComputersSystem : SharedModularComputersSystem
{
    [Dependency] private readonly PowerCellSystem _powerCellSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<Shared._CorvaxNext.ModularComputers.Components.ModularComputerComponent, InteractUsingEvent>(OnInteract);
        SubscribeNetworkEvent<ChangeModularComputerStateEvent>(OnChangeModularComputerStateEvent);
        SubscribeNetworkEvent<CreateLoadProgramUIEvent>(OnCreateLoadProgramUIEvent);

        Subs.BuiEvents<Shared._CorvaxNext.ModularComputers.Components.ModularComputerComponent>(ModularComputerProshivkaUIKey.Key,
            t =>
            {
                t.Event<LoadProgramMessage>(OnLoadProgramMessage);
            });
    }

    private void OnInteract(Entity<Shared._CorvaxNext.ModularComputers.Components.ModularComputerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (Tool.HasQuality(args.Used, ent.Comp.OpeningTool))
        {
            ent.Comp.Open = !ent.Comp.Open;
            DirtyField(ent.Owner, ent.Comp, nameof(ent.Comp.Open));

            var sound = ent.Comp.Open ? ent.Comp.ScrewdriverOpenSound : ent.Comp.ScrewdriverCloseSound;
            Audio.PlayPredicted(sound, args.Target, args.User);
        }
        else if (ent.Comp.Open && TryComp<BasePciComponent>(args.Used, out var pciComponent))
        {
            if (pciComponent is PciCpuComponent cpu)
            {
                Container.Insert(args.Used, ent.Comp.CpuSlot);
                cpu.ModularComputer = ent.Owner;
            }
            else
                Container.Insert(args.Used, ent.Comp.PciContainer);
            Audio.PlayPredicted(ent.Comp.CircuitInsertionSound, args.Target, args.User);
        }
    }

    private void OnCreateLoadProgramUIEvent(CreateLoadProgramUIEvent msg, EntitySessionEventArgs args)
    {
        var ent = EntManager.GetEntity(msg.Target);
        if (EntityManager.TryGetComponent(ent, out UserInterfaceComponent? comp))
            UserInterfaceSystem.OpenUi((ent, comp), ModularComputerProshivkaUIKey.Key);
    }

    private void OnLoadProgramMessage(EntityUid uid, Shared._CorvaxNext.ModularComputers.Components.ModularComputerComponent component, LoadProgramMessage args)
    {
        if (EntityManager.TryGetComponent(component.CpuSlot.ContainedEntity, out PciCpuComponent? cpuComp))
        {
            if (cpuComp.Cpu is not Cpu {} cpu)
            {
                cpu = new Cpu(cpuComp.RamSize, cpuComp);
                cpuComp.Cpu = cpu;
            }

            var prog = args.Program.AsSpan();
            if (prog[1] == 0x45)
                prog = prog.Slice(0x1000); // skip ELF header
            cpu.LoadProgramm(prog);
        }
    }

    private void OnChangeModularComputerStateEvent(ChangeModularComputerStateEvent ev)
    {
        var entity = EntityManager.GetEntity(ev.Target);
        if (EntityManager.TryGetComponent(entity, out Shared._CorvaxNext.ModularComputers.Components.ModularComputerComponent? comp))
        {
            comp.IsOn = !comp.IsOn;
            DirtyField(entity, comp, nameof(comp.IsOn));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var modularComputerComponent in EntityManager.EntityQuery<Shared._CorvaxNext.ModularComputers.Components.ModularComputerComponent>())
        {
            if (modularComputerComponent is { IsOn: true, CpuSlot.ContainedEntity: { Valid: true } cpuEntity } &&
                EntityManager.TryGetComponent(cpuEntity, out PciCpuComponent? cpu))
            {
                cpu.AccumulatedTime += frameTime;
                while (cpu.AccumulatedTime >= cpu.RequiredTime)
                {
                    cpu.AccumulatedTime -= cpu.RequiredTime;
                    if (_powerCellSystem.TryUseCharge(modularComputerComponent.MyOwner, cpu.PowerPerInstruction))
                    {
                        var riscVCpu = cpu.Cpu;
                        if (riscVCpu == null)
                        {
                            riscVCpu = new Cpu(cpu.RamSize, cpu);
                            cpu.Cpu = riscVCpu;
                        }

                        if (TryGetPciComponent<PciGpuComponent>(modularComputerComponent,
                                out var gpu,
                                out var gpuEnt) && gpu.RequireSync)
                        {
                            DirtyField(gpuEnt, gpu, nameof(gpu.Commands));
                            gpu.RequireSync = false;
                        }
                        try
                        {
                            riscVCpu.Execute();
                        }
                        catch
                        {
                            // handle in future
                        }
                    }
                }
            }
        }
    }
}
