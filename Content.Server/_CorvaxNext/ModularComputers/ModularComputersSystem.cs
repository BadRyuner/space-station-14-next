using Content.Server._CorvaxNext.ModularComputers.Components;
using Content.Server._CorvaxNext.ModularComputers.Emulator;
using Content.Server.PowerCell;
using Content.Shared._CorvaxNext.ModularComputers;
using Content.Shared._CorvaxNext.ModularComputers.Components;
using Content.Shared._CorvaxNext.ModularComputers.Events;
using Content.Shared._CorvaxNext.ModularComputers.Messages;
using Content.Shared.Interaction;
using Robust.Server.Audio;

namespace Content.Server._CorvaxNext.ModularComputers;

public sealed class ModularComputersSystem : SharedModularComputersSystem
{
    [Dependency] private readonly PowerCellSystem _powerCellSystem = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<CreateLoadProgramUIEvent>(OnCreateLoadProgramUIEvent);
        SubscribeLocalEvent<ModularComputerComponent, InteractUsingEvent>(OnInteract);
        SubscribeNetworkEvent<ChangeModularComputerStateEvent>(OnChangeModularComputerStateEvent);

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
            UserInterfaceSystem.OpenUi((ent, comp), ModularComputerProshivkaUIKey.Key, args.SenderSession);
    }

    private void OnInteract(Entity<ModularComputerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (Tool.HasQuality(args.Used, ent.Comp.OpeningTool))
        {
            ent.Comp.Open = !ent.Comp.Open;
            DirtyField(ent.Owner, ent.Comp, nameof(ent.Comp.Open));

            var sound = ent.Comp.Open ? ent.Comp.ScrewdriverOpenSound : ent.Comp.ScrewdriverCloseSound;
            _audio.PlayPvs(sound, args.Target);
            return;
        }

        BasePciComponent? pciComponent = null;

        // ДА ДАВАЙ ДАВАЙ ДАВАЙ МЫ УЖЕ НЕ УМЕЕМ В НАСЛЕДОВАНИЕ в TRYCOMP
        if (TryComp(args.Used, out PciCpuComponent? cpu))
            pciComponent = cpu;
        else if (TryComp(args.Used, out PciGpuComponent? gpu))
            pciComponent = gpu;

        if (ent.Comp.Open && pciComponent != null)
        {
            if (cpu != null)
            {
                Container.Insert(args.Used, ent.Comp.CpuSlot);
                cpu.ModularComputer = ent.Owner;
            }
            else
                Container.Insert(args.Used, ent.Comp.PciContainer);
            _audio.PlayPvs(ent.Comp.CircuitInsertionSound, args.Target);
        }
    }

    private void OnLoadProgramMessage(EntityUid uid, ModularComputerComponent component, LoadProgramMessage args)
    {
        if (EntityManager.TryGetComponent(component.CpuSlot.ContainedEntity, out PciCpuComponent? cpuComp))
        {
            Cpu cpu;
            if (cpuComp.Cpu == null)
            {
                cpu = new Cpu(cpuComp.RamSize, cpuComp);
                cpuComp.Cpu = cpu;
            }
            else
                cpu = cpuComp.Cpu;

            var prog = args.Program.AsSpan();
            if (prog[1] == 0x45)
                prog = prog.Slice(0x1000); // skip ELF header
            cpu.LoadProgramm(prog);
            Log.Debug($"Loaded program with size {prog.Length}");
        }
    }

    private void OnChangeModularComputerStateEvent(ChangeModularComputerStateEvent ev)
    {
        var entity = EntityManager.GetEntity(ev.Target);
        if (EntityManager.TryGetComponent(entity, out ModularComputerComponent? comp))
        {
            if (comp.IsOn != ev.NewState)
            {
                Log.Debug($"Changed mod.comp state from {comp.IsOn} to {ev.NewState}");
                comp.IsOn = ev.NewState;
                DirtyField(entity, comp, nameof(comp.IsOn));
            }
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
                if (cpu.Cpu is { } riscVCpu)
                {
                    cpu.AccumulatedTime += frameTime;
                    while (cpu.AccumulatedTime >= cpu.RequiredTime)
                    {
                        cpu.AccumulatedTime -= cpu.RequiredTime;
                        if (!riscVCpu.Idle) // _powerCellSystem.TryUseCharge(modularComputerComponent.MyOwner, cpu.PowerPerInstruction)
                        {
                            try
                            {
                                riscVCpu.Execute();
                            }
                            catch
                            {
                                // handle in future
                            }

                            if (TryGetPciComponent<PciGpuComponent>(modularComputerComponent,
                                    out var gpu,
                                    out var gpuEnt) && gpu.RequireSync)
                            {
                                DirtyField(gpuEnt, gpu, nameof(gpu.Commands));
                                gpu.RequireSync = false;
                                Log.Debug("Sync GPU!");
                            }
                        }
                    }
                }
            }
        }
    }
}
