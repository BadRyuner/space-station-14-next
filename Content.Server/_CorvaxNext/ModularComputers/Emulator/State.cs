namespace Content.Server._CorvaxNext.ModularComputers.Emulator;

public struct State
{
    public const int FCSR = 0x003;
    public const int SSTATUS = 0x100;
    public const int SIE = 0x104;
    public const int SPEC = 0x141;
    public const int SIP = 0x144;
    public const int SATP = 0x180;
    public const int MSTATUS = 0x300;
    public const int MEPC = 0x341;
    public const ulong SSTATUS_SIE_MASK = 0x2; // sstatus[1]
    public const ulong SSTATUS_SPIE_MASK = 0x20; // sstatus[5]
    public const ulong SSTATUS_UBE_MASK = 0x40; // sstatus[6]
    public const ulong SSTATUS_SPP_MASK = 0x100; // sstatus[8]
    public const ulong SSTATUS_FS_MASK = 0x6000; // sstatus[14:13]
    public const ulong SSTATUS_XS_MASK = 0x18000; // sstatus[16:15]
    public const ulong SSTATUS_SUM_MASK = 0x40000; // sstatus[18]
    public const ulong SSTATUS_MXR_MASK = 0x80000; // sstatus[19]
    public const ulong SSTATUS_UXL_MASK = 0x3_00000000; // sstatus[33:32]
    public const ulong SSTATUS_SD_MASK = 0x80000000_00000000; // sstatus[63]
    public const ulong SSTATUS_MASK = SSTATUS_SIE_MASK
                                      | SSTATUS_SPIE_MASK
                                      | SSTATUS_UBE_MASK
                                      | SSTATUS_SPP_MASK
                                      | SSTATUS_FS_MASK
                                      | SSTATUS_XS_MASK
                                      | SSTATUS_SUM_MASK
                                      | SSTATUS_MXR_MASK
                                      | SSTATUS_UXL_MASK
                                      | SSTATUS_SD_MASK;
    public const int MIE = 0x304;
    public const ulong MIDELEG = 0x303;
    public const int MIP = 0x344;
    public const int MVENDORID = 0xf11;
    public const int MARCHID = 0xf12;
    public const int MIMPID = 0xf13;
    public const int MHARTID = 0xf14;

    public const ulong SSIP_BIT = 1 << 1;

    public static readonly Range XSTATUS_SIE = 1..1;
    public static readonly Range XSTATUS_SPIE = 5..5;
    public static readonly Range XSTATUS_SPP = 8..8;

    public static readonly Range MSTATUS_MIE = 3..3;
    public static readonly Range MSTATUS_MPIE = 7..7;
    public static readonly Range MSTATUS_MPP = 11..12;
    public static readonly Range MSTATUS_MPRV = 17..17;

    public const int MisaAddress = 0x301;

    private const ulong MisaDefaultValue = ((ulong)2 << 62) | (1 << 20) |
                                           (1 << 18) | (1 << 12) |
                                           (1 << 8) | (1 << 5) |
                                           (1 << 3) | (1 << 2) | 1;

    private readonly ulong[] _csr;

    public State()
    {
        _csr = new ulong[4096];
        _csr[MisaAddress] = MisaDefaultValue;
    }

    public ulong this[int at]
    {
        get => _csr[at];
        set => _csr[at] = value;
    }

    public void Reset()
    {
        _csr.AsSpan().Fill(0);
        _csr[MisaAddress] = MisaDefaultValue;
    }

    public void WriteBit(int addr, int bit, int val)
    {
        if (val == 1)
            Write(addr, Read(addr) | (uint)(1 << bit));
        else if (val == 0)
            Write(addr, Read(addr) | ~(uint)(1 << bit));
    }

    public void WriteBits(int addr, Range range, ulong val)
    {
        var bitmask = (~0ul << range.End.Value) | ~(~0ul << range.Start.Value);

        Write(addr, (Read(addr) & bitmask) | (val << range.Start.Value));
    }

    public void WriteSStatus(Range range, ulong value)
    {
        WriteBits(SSTATUS, range, value);
    }

    public void WriteMStatus(Range range, ulong value)
    {
        WriteBits(MSTATUS, range, value);
    }

    public ulong Read(int addr)
    {
        return addr switch
        {
            SSTATUS => _csr[MSTATUS] & SSTATUS_MASK,
            SIE => _csr[MIE] & _csr[MIDELEG],
            SIP => _csr[MIP] & _csr[MIDELEG],
            _ => _csr[addr],
        };
    }

    public void Write(int addr, ulong value)
    {
        switch (addr)
        {
            case MVENDORID:
            case MARCHID:
            case MIMPID:
            case MHARTID:
            {
                return;
            }
            case SSTATUS:
            {
                _csr[MSTATUS] = (_csr[MSTATUS] & ~SSTATUS_MASK) | (value & SSTATUS_MASK);
                return;
            }
            case SIE:
            {
                _csr[MIE] = (_csr[MIE] & ~MIDELEG) | (value & MIDELEG);
                return;
            }
            case SIP:
            {
                var mask = SSIP_BIT & _csr[MIDELEG];
                _csr[MIP] = (_csr[MIP] & ~mask) | (value & mask);
                return;
            }
            default:
            {
                _csr[addr] = value;
                return;
            }
        }
    }

    public ulong ReadBits(int addr, Range range)
    {
        // пупупу надеюсь будет без ошибок работать
        // в rvemu на Rust это реализовано очень страшна
        var bitmask = 0u;
        if (range.End.Value != 64) {
            bitmask = ~0u << range.End.Value;
        }

        return (Read(addr) & ~bitmask) >> range.Start.Value;
    }

    public ulong ReadSStatus(Range range)
    {
        return ReadBits(SSTATUS, range);
    }

    public ulong ReadMStatus(Range range)
    {
        return ReadBits(MSTATUS, range);
    }
}
