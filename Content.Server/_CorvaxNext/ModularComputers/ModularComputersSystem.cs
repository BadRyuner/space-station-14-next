using Content.Server._CorvaxNext.ModularComputers.Emulator;
using Content.Server._CorvaxNext.ModularComputers.Wrappers;
using Content.Server.PowerCell;
using Content.Shared._CorvaxNext.ModularComputers;
using Content.Shared._CorvaxNext.ModularComputers.Components;
using Content.Shared._CorvaxNext.ModularComputers.Events;
using Content.Shared._CorvaxNext.ModularComputers.Messages;

namespace Content.Server._CorvaxNext.ModularComputers;

public sealed class ModularComputersSystem : SharedModularComputersSystem
{
    [Dependency] private readonly PowerCellSystem _powerCellSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ChangeModularComputerStateEvent>(OnChangeModularComputerStateEvent);
        SubscribeNetworkEvent<CreateLoadProgramUIEvent>(OnCreateLoadProgramUIEvent);
        SubscribeLocalEvent<PciGpuComponent, ComponentStartup>(OnGpuStart);

        Subs.BuiEvents<ModularComputerComponent>(ModularComputerProshivkaUIKey.Key,
            t =>
            {
                t.Event<LoadProgramMessage>(OnLoadProgramMessage);
            });
    }

    private void OnCreateLoadProgramUIEvent(CreateLoadProgramUIEvent msg, EntitySessionEventArgs args)
    {
        var ent = EntManager.GetEntity(msg.Target);
        if (EntityManager.TryGetComponent(ent, out UserInterfaceComponent? comp))
            UserInterfaceSystem.OpenUi((ent, comp), ModularComputerProshivkaUIKey.Key);
    }

    private void OnLoadProgramMessage(EntityUid uid, ModularComputerComponent component, LoadProgramMessage args)
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

    private void OnGpuStart(Entity<PciGpuComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.Wrapper = new GpuPciWrapper(ent.Comp);
    }

    private void OnChangeModularComputerStateEvent(ChangeModularComputerStateEvent ev)
    {
        var entity = EntityManager.GetEntity(ev.Target);
        if (EntityManager.TryGetComponent(entity, out ModularComputerComponent? comp))
        {
            comp.IsOn = !comp.IsOn;
            DirtyField(entity, comp, nameof(comp.IsOn));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var modularComputerComponent in EntityManager.EntityQuery<ModularComputerComponent>())
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
                        Cpu? riscVCpu = (Cpu?)cpu.Cpu;
                        if (riscVCpu == null)
                        {
                            riscVCpu = new Cpu(cpu.RamSize, cpu);
                            cpu.Cpu = riscVCpu;
                        }
                        if (TryGetPciComponent<PciGpuComponent>(modularComputerComponent, out var gpu, out var gpuEnt) && gpu.RequireSync)
                            DirtyField(gpuEnt, gpu, nameof(gpu.Commands));
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
