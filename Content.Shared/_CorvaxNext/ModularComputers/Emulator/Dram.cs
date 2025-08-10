namespace Content.Shared._CorvaxNext.ModularComputers.Emulator;

// мб стоит BinaryPrimitives.ReadLittleEndian использовать?

public struct Dram(int initialSize)
{
    public const int DramBase = 0x2000_0000;

    public int Size = initialSize;

    private byte[] _dram = new byte[initialSize];

    public void Resize(int newSize)
    {
        Size = newSize;

        var newDram = new byte[newSize];
        _dram.AsSpan().CopyTo(newDram.AsSpan());
        _dram = newDram;
    }

    public void WriteCode(ReadOnlySpan<byte> code)
    {
        if (Size < code.Length)
            throw new ArgumentException("Попытка записи длинного кода в маленькую память");

        // blazing fast ultra simd mega optimized ctrl+c ctrl+v
        code.CopyTo(_dram.AsSpan());
    }

    public ulong Read(ulong addr, Bits size)
    {
        return size switch
        {
            Bits.Byte => Read8(addr),
            Bits.HalfWord => Read16(addr),
            Bits.Word => Read32(addr),
            Bits.DoubleWord => Read64(addr),
            _ => throw new Exception(), // LoadAccessFault пупупупу
        };
    }

    public void Write(ulong addr, ulong value, Bits size)
    {
        switch (size)
        {
            case Bits.Byte:
                Write8(addr, value);
                break;
            case Bits.HalfWord:
                Write16(addr, value);
                break;
            case Bits.Word:
                Write32(addr, value);
                break;
            case Bits.DoubleWord:
                Write64(addr, value);
                break;
            default:
                throw new Exception(); // Smoloadexc что-то там
        }
    }

    public void Write64(ulong addr, ulong value)
    {
        var index = addr - DramBase;
        _dram[index] = (byte)value;
        _dram[index + 1] = (byte)((value >> 8) & 0xFF);
        _dram[index + 2] = (byte)((value >> 16) & 0xFF);
        _dram[index + 3] = (byte)((value >> 24) & 0xFF);
        _dram[index + 4] = (byte)((value >> 32) & 0xFF);
        _dram[index + 5] = (byte)((value >> 40) & 0xFF);
        _dram[index + 6] = (byte)((value >> 48) & 0xFF);
        _dram[index + 7] = (byte)((value >> 56) & 0xFF);
    }

    public void Write32(ulong addr, ulong value)
    {
        var index = addr - DramBase;
        _dram[index] = (byte)value;
        _dram[index + 1] = (byte)((value >> 8) & 0xFF);
        _dram[index + 2] = (byte)((value >> 16) & 0xFF);
        _dram[index + 3] = (byte)((value >> 24) & 0xFF);
    }

    public void Write16(ulong addr, ulong value)
    {
        var index = addr - DramBase;
        _dram[index] = (byte)value;
        _dram[index + 1] = (byte)((value >> 8) & 0xFF);
    }

    public void Write8(ulong addr, ulong value)
    {
        _dram[addr - DramBase] = (byte)value;
    }

    public ulong Read64(ulong addr)
    {
        var index = addr - DramBase;
        return _dram[index] | ((ulong)_dram[index + 1] << 8)
                            | ((ulong)_dram[index + 2] << 16)
                            | ((ulong)_dram[index + 3] << 24)
                            | ((ulong)_dram[index + 4] << 32)
                            | ((ulong)_dram[index + 5] << 40)
                            | ((ulong)_dram[index + 6] << 48)
                            | ((ulong)_dram[index + 7] << 56);
    }

    public ulong Read32(ulong addr)
    {
        var index = addr - DramBase;
        return _dram[index] | ((ulong)_dram[index + 1] << 8)
                            | ((ulong)_dram[index + 2] << 16)
                            | ((ulong)_dram[index + 3] << 24);
    }

    public ulong Read16(ulong addr)
    {
        var index = addr - DramBase;
        return _dram[index] | ((ulong)_dram[index + 1] << 8);
    }

    public ulong Read8(ulong addr)
    {
        return _dram[addr - DramBase];
    }
}
