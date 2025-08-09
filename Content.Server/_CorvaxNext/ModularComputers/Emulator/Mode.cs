namespace Content.Server._CorvaxNext.ModularComputers.Emulator;

public enum Mode : byte
{
    User = 0b00,
    Supervisor = 0b01,
    Machine = 0b11,
    Debug,
}
