// ReSharper disable InconsistentNaming
namespace Content.Server._CorvaxNext.ModularComputers.Emulator;

public abstract class RiscVException(int Code) : Exception;

public sealed class InstructionAddressMisaligned() : RiscVException(0);
public sealed class InstructionAccessFault() : RiscVException(1);
public sealed class IllegalInstruction(ulong instr) : RiscVException(2);
public sealed class Breakpoint() : RiscVException(3);
public sealed class LoadAddressMisaligned() : RiscVException(4);
public sealed class LoadAccessFault() : RiscVException(5);
public sealed class StoreAMOAddressMisaligned() : RiscVException(6);
public sealed class StoreAMOAccessFault() : RiscVException(7);
public sealed class EnvironmentCallFromUMode() : RiscVException(8);
public sealed class EnvironmentCallFromSMode() : RiscVException(9);
public sealed class EnvironmentCallFromMMode() : RiscVException(11);
public sealed class InstructionPageFault() : RiscVException(12);
public sealed class LoadPageFault() : RiscVException(13);
public sealed class StoreAMOPageFault() : RiscVException(15);
