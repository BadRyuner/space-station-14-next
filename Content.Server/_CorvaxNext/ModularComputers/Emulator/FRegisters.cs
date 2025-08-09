using System.Runtime.CompilerServices;

namespace Content.Server._CorvaxNext.ModularComputers.Emulator;

[InlineArray(32)]
public unsafe struct FRegisters
{
    private double _first;

    public ref double this[ulong at]
    {
        get => ref Unsafe.AddByteOffset(ref _first, (nint)at);
    }

    public T Read<T>(int at) where T : unmanaged => Unsafe.As<double, T>(ref this[at]);

    public void Write<T>(int at, T value) where T : unmanaged => Unsafe.As<double, T>(ref this[at]) = value;

    public void Reset()
    {
        new Span<double>(Unsafe.AsPointer(ref _first), 32).Fill(0);
    }
}
