using System.Runtime.CompilerServices;

namespace Content.Server._CorvaxNext.ModularComputers.Emulator;

[InlineArray(32)]
public unsafe struct XRegisters
{
    private ulong _first;

    public void Initialize(int dramSize)
    {
        this[2] = Dram.DramBase + (ulong)dramSize;
        this[10] = 0;
        this[11] = 0x1020; // ptr to DTB
    }

    public ulong this[ulong at]
    {
        get => at == 0 ? 0 : this[(int)at];
        set => this[(int)at] = value;
    }

    public T Read<T>(int at) where T : unmanaged => Unsafe.As<ulong, T>(ref this[at]);

    public void Write<T>(int at, T value) where T : unmanaged => Unsafe.As<ulong, T>(ref this[at]) = value;

    public T Read<T>(ulong at) where T : unmanaged => Unsafe.As<ulong, T>(ref this[(int)at]);

    public void Write<T>(ulong at, T value) where T : unmanaged => Unsafe.As<ulong, T>(ref this[(int)at]) = value;


    public void Reset()
    {
        new Span<ulong>(Unsafe.AsPointer(ref _first), 32).Fill(0);
    }
}
