using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.ModularComputers.Emulator;

[NetSerializable, Serializable]
public enum Bits : byte
{
    Byte = 8,
    HalfWord = 16,
    Word = 32,
    DoubleWord = 64,
}
