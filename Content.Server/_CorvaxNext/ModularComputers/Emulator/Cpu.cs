using Content.Shared._CorvaxNext.ModularComputers.Emulator;
using System.Runtime.CompilerServices;
using Content.Server._CorvaxNext.ModularComputers.Components;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

namespace Content.Server._CorvaxNext.ModularComputers.Emulator;

public sealed class Cpu
{
    public XRegisters XRegisters = new();
    public FRegisters FRegisters = new();
    public ulong PC = 0;
    public State State = new();
    public Mode Mode = Mode.Machine;
    public readonly Bus Bus;
    public bool EnablePaging = false;
    public ulong PageTable = 0;
    public readonly List<ulong> ReservationSet = new();
    public bool Idle = false;

    public Cpu(int dramSize, PciCpuComponent pciCpuComponent)
    {
        XRegisters.Initialize(dramSize);
        Bus = new(this, pciCpuComponent, dramSize);
    }

    public void LoadProgramm(ReadOnlySpan<byte> programm)
    {
        PC = Dram.DramBase;
        Bus.Dram.WriteCode(programm);
    }

    public void Execute()
    {
        if (Idle)
            return;

        ulong inst16 = Fetch(Bits.HalfWord);

        if (inst16 == 0)
            throw new IllegalInstruction(inst16);
        switch (inst16 & 0b11)
        {
            case 0:
            case 1:
            case 2:
                ExecuteCompressed(inst16);
                PC += 2;
                break;
            default:
                ulong inst = Fetch(Bits.Word);
                ExecuteGeneral(inst);
                PC += 4;
                break;
        }
    }

    private void ExecuteCompressed(ulong inst)
    {
        var opcode = inst & 0x3;
        var funct3 = (inst >> 13) & 0x7;

        switch (opcode)
        {
            case 0x0:
            {
                // Quadrant 0
                switch (funct3)
                {
                    case 0x0: // c.addi4spn
                    {
                        var rd = ((inst >> 2) & 0x7) + 8;
                        var nzuimm = ((inst >> 1) & 0x3c0) // znuimm[9:6]
                                     | ((inst >> 7) & 0x30) // znuimm[5:4]
                                     | ((inst >> 2) & 0x8) // znuimm[3]
                                     | ((inst >> 4) & 0x4); // znuimm[2]
                        if (nzuimm == 0)
                        {
                            throw new IllegalInstruction(inst);
                        }

                        XRegisters[rd] = XRegisters[2] + nzuimm;
                        return;
                    }
                    case 0x1: // c.fld
                    {
                        var rd = ((inst >> 2) & 0x7) + 8;
                        var rs1 = ((inst >> 7) & 0x7) + 8;
                        // offset[5:3|7:6] = isnt[12:10|6:5]
                        var offset = ((inst << 1) & 0xc0) // imm[7:6]
                                     | ((inst >> 7) & 0x38); // imm[5:3]

                        var readed = Read(XRegisters[rs1] + offset, Bits.DoubleWord);
                        var val = Unsafe.As<ulong, double>(ref readed);
                        FRegisters[rd] = val;
                        return;
                    }
                    case 0x2: // c.lw
                    {
                        var rd = ((inst >> 2) & 0x7) + 8;
                        var rs1 = ((inst >> 7) & 0x7) + 8;
                        // offset[5:3|2|6] = isnt[12:10|6|5]
                        var offset = ((inst << 1) & 0x40) // imm[6]
                                     | ((inst >> 7) & 0x38) // imm[5:3]
                                     | ((inst >> 4) & 0x4); // imm[2]
                        var addr = XRegisters[rs1] + offset;
                        var val = Read(addr, Bits.Word);
                        XRegisters[rd] = (ulong)(long)(int)val;
                        return;
                    }
                    case 0x3:
                    {
                        var rd = ((inst >> 2) & 0x7) + 8;
                        var rs1 = ((inst >> 7) & 0x7) + 8;
                        // offset[5:3|7:6] = isnt[12:10|6:5]
                        var offset = ((inst << 1) & 0xc0) // imm[7:6]
                                     | ((inst >> 7) & 0x38); // imm[5:3]
                        var addr = XRegisters[rs1] + offset;
                        var val = Read(addr, Bits.DoubleWord);
                        XRegisters[rd] = val;
                        return;
                    }
                    case 0x4:
                    {
                        throw new IllegalInstruction(inst); // reserved
                    }
                    case 0x5: // c.fsd
                    {
                        var rs2 = ((inst >> 2) & 0x7) + 8;
                        var rs1 = ((inst >> 7) & 0x7) + 8;
                        // offset[5:3|7:6] = isnt[12:10|6:5]
                        var offset = ((inst << 1) & 0xc0) // imm[7:6]
                                     | ((inst >> 7) & 0x38); // imm[5:3]
                        var addr = XRegisters[rs1] + offset;
                        var fl = FRegisters[rs2];
                        var flasu = Unsafe.As<double, ulong>(ref fl);
                        Write(addr, flasu, Bits.DoubleWord);
                        return;
                    }
                    case 0x6: // c.sw
                    {
                        var rs2 = ((inst >> 2) & 0x7) + 8;
                        var rs1 = ((inst >> 7) & 0x7) + 8;
                        // offset[5:3|2|6] = isnt[12:10|6|5]
                        var offset = ((inst << 1) & 0x40) // imm[6]
                                     | ((inst >> 7) & 0x38) // imm[5:3]
                                     | ((inst >> 4) & 0x4); // imm[2]
                        var addr = XRegisters[rs1] + offset;
                        Write(addr, XRegisters[rs2], Bits.Word);
                        return;
                    }
                    case 0x7: // c.sd
                    {
                        var rs2 = ((inst >> 2) & 0x7) + 8;
                        var rs1 = ((inst >> 7) & 0x7) + 8;
                        // offset[5:3|7:6] = isnt[12:10|6:5]
                        var offset = ((inst << 1) & 0xc0) // imm[7:6]
                                     | ((inst >> 7) & 0x38); // imm[5:3]
                        var addr = XRegisters[rs1] + offset;
                        Write(addr, XRegisters[rs2], Bits.DoubleWord);
                        return;
                    }
                }
                return;
            }
            case 1:
            {
                // Quadrant 1.
                switch (funct3)
                {
                    case 0x0: // c.addi
                    {
                        var rd = (inst >> 7) & 0x1f;
                        // nzimm[5|4:0] = inst[12|6:2]
                        var nzimm = ((inst >> 7) & 0x20) | ((inst >> 2) & 0x1f);
                        // Sign-extended.
                        nzimm = ((nzimm & 0x20) == 0) switch
                        {
                            true => nzimm,
                            false => (ulong)(long)(sbyte)(0xc0 | nzimm),
                        };
                        if (rd != 0)
                        {
                            XRegisters[rd] += nzimm;
                        }
                        return;
                    }
                    case 0x1: // c.addiw
                    {
                        var rd = (inst >> 7) & 0x1f;
                        // imm[5|4:0] = inst[12|6:2]
                        var imm = ((inst >> 7) & 0x20) | ((inst >> 2) & 0x1f);
                        // Sign-extended.
                        imm = ((imm & 0x20) == 0) switch
                        {
                            true => imm,
                            false => (ulong)(long)(sbyte)(0xc0 | imm),
                        };
                        if (rd != 0)
                        {
                            XRegisters[rd] += (ulong)(long)(int)imm;
                        }
                        return;
                    }
                    case 0x2: // c.li
                    {
                        var rd = (inst >> 7) & 0x1f;
                        // imm[5|4:0] = inst[12|6:2]
                        var imm = ((inst >> 7) & 0x20) | ((inst >> 2) & 0x1f);
                        // Sign-extended.
                        imm = ((imm & 0x20) == 0) switch
                        {
                            true => imm,
                            false => (ulong)(long)(sbyte)(0xc0 | imm),
                        };
                        if (rd != 0)
                        {
                            XRegisters[rd] = imm;
                        }
                        return;
                    }
                    case 0x3:
                    {
                        var rd = (inst >> 7) & 0x1f;
                        switch (rd)
                        {
                            case 0x0:
                                return;
                            case 0x2:  // c.addi16sp
                            {
                                var nzimm = ((inst >> 3) & 0x200) // nzimm[9]
                                            | ((inst >> 2) & 0x10) // nzimm[4]
                                            | ((inst << 1) & 0x40) // nzimm[6]
                                            | ((inst << 4) & 0x180) // nzimm[8:7]
                                            | ((inst << 3) & 0x20); // nzimm[5]
                                nzimm = ((nzimm & 0x200) == 0) switch
                                {
                                    true => nzimm,
                                    // Sign-extended.
                                    false => (ulong)(long)(int)(short)(0xfc00 | nzimm),
                                };
                                if (nzimm != 0)
                                {
                                    XRegisters[2] = XRegisters[2] + nzimm;
                                }
                                return;
                            }
                            default: // c.lui
                            {
                                var nzimm = ((inst << 5) & 0x20000) | ((inst << 10) & 0x1f000);
                                // Sign-extended.
                                nzimm = ((nzimm & 0x20000) == 0) switch
                                {
                                    true => nzimm,
                                    false => (ulong)(long)(int)(0xfffc0000 | nzimm),
                                };
                                if (nzimm != 0)
                                {
                                    XRegisters[rd] = nzimm;
                                }
                                return;
                            }
                        }
                    }
                    case 0x4:
                    {
                        var funct2 = (inst >> 10) & 0x3;
                        switch (funct2)
                        {
                            case 0x0: // c.srli
                            {
                                var rd = ((inst >> 7) & 0b111) + 8;
                                // shamt[5|4:0] = inst[12|6:2]
                                var shamt = ((inst >> 7) & 0x20) | ((inst >> 2) & 0x1f);
                                XRegisters[rd] = XRegisters[rd] >> (int)shamt;
                                return;
                            }
                            case 0x1: // c.srai
                            {
                                var rd = ((inst >> 7) & 0b111) + 8;
                                // shamt[5|4:0] = inst[12|6:2]
                                var shamt = ((inst >> 7) & 0x20) | ((inst >> 2) & 0x1f);
                                XRegisters[rd] = (ulong)(((long)XRegisters[rd]) >> (int)shamt);
                                return;
                            }
                            case 0x2:  // c.andi
                            {
                                var rd = ((inst >> 7) & 0b111) + 8;
                                // imm[5|4:0] = inst[12|6:2]
                                var imm = ((inst >> 7) & 0x20) | ((inst >> 2) & 0x1f);
                                // Sign-extended.
                                imm = ((imm & 0x20) == 0) switch {
                                    true => imm,
                                    false => (ulong)(long)(sbyte)(0xc0 | imm),
                                };
                                XRegisters[rd] = XRegisters[rd] & imm;
                                return;
                            }
                            case 0x3:
                            {
                                var rd = ((inst >> 7) & 0b111) + 8;
                                var rs2 = ((inst >> 2) & 0b111) + 8;
                                switch ((inst >> 12) & 0b1, (inst >> 5) & 0b11)
                                {
                                    case (0x0, 0x0): // c.sub
                                    {
                                        XRegisters[rd] = XRegisters[rd] - XRegisters[rs2];
                                        return;
                                    }
                                    case (0x0, 0x1): // c.xor
                                    {
                                        XRegisters[rd] = XRegisters[rd] ^ XRegisters[rs2];
                                        return;
                                    }
                                    case (0x0, 0x2): // c.or
                                    {
                                        XRegisters[rd] = XRegisters[rd] | XRegisters[rs2];
                                        return;
                                    }
                                    case (0x0, 0x3): // c.and
                                    {
                                        XRegisters[rd] = XRegisters[rd] & XRegisters[rs2];
                                        return;
                                    }
                                    case (0x1, 0x0): // c.subw
                                    {
                                        XRegisters[rd] = (ulong)(long)(int)(XRegisters[rd] - XRegisters[rs2]);
                                        return;
                                    }
                                    case (0x1, 0x1): // c.addw
                                    {
                                        XRegisters[rd] = (ulong)(long)(int)(XRegisters[rd] + XRegisters[rs2]);
                                        return;
                                    }
                                }
                                return;
                            }
                            default:
                                throw new IllegalInstruction(inst);
                        }
                    }
                    case 0x5: // c.j
                    {
                        var offset = ((inst >> 1) & 0x800) // offset[11]
                                         | ((inst << 2) & 0x400) // offset[10]
                                         | ((inst >> 1) & 0x300) // offset[9:8]
                                         | ((inst << 1) & 0x80) // offset[7]
                                         | ((inst >> 1) & 0x40) // offset[6]
                                         | ((inst << 3) & 0x20) // offset[5]
                                         | ((inst >> 7) & 0x10) // offset[4]
                                         | ((inst >> 2) & 0xe); // offset[3:1]

                        // Sign-extended.
                        offset = ((offset & 0x800) == 0) switch
                        {
                            true => offset,
                            false => (ulong)(long)(short)(0xf000 | offset),
                        };
                        PC += offset - 2;
                        return;
                    }
                    case 0x6: // c.beqz
                    {
                        var rs1 = ((inst >> 7) & 0b111) + 8;
                        // offset[8|4:3|7:6|2:1|5] = inst[12|11:10|6:5|4:3|2]
                        var offset = ((inst >> 4) & 0x100) // offset[8]
                                         | ((inst << 1) & 0xc0) // offset[7:6]
                                         | ((inst << 3) & 0x20) // offset[5]
                                         | ((inst >> 7) & 0x18) // offset[4:3]
                                         | ((inst >> 2) & 0x6); // offset[2:1]
                        // Sign-extended.
                        offset = ((offset & 0x100) == 0) switch
                        {
                            true => offset,
                            false => (ulong)(long)(short)(0xfe00 | offset),
                        };
                        if (XRegisters[rs1] == 0)
                        {
                            PC += offset - 2;
                        }
                        return;
                    }
                    case 0x7: // c.bnez
                    {
                        var rs1 = ((inst >> 7) & 0b111) + 8;
                        // offset[8|4:3|7:6|2:1|5] = inst[12|11:10|6:5|4:3|2]
                        var offset = ((inst >> 4) & 0x100) // offset[8]
                                         | ((inst << 1) & 0xc0) // offset[7:6]
                                         | ((inst << 3) & 0x20) // offset[5]
                                         | ((inst >> 7) & 0x18) // offset[4:3]
                                         | ((inst >> 2) & 0x6); // offset[2:1]
                        // Sign-extended.
                        offset = ((offset & 0x100) == 0) switch
                        {
                            true => offset,
                            false => (ulong)(long)(short)(0xfe00 | offset),
                        };
                        if (XRegisters[rs1] == 0)
                        {
                            PC += offset - 2;
                        }
                        return;
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
            case 2:
            {
                // Quadrant 2.
                switch (funct3)
                {
                    case 0x0: // c.slli
                    {
                        var rd = (inst >> 7) & 0x1f;
                        // shamt[5|4:0] = inst[12|6:2]
                        var shamt = ((inst >> 7) & 0x20) | ((inst >> 2) & 0x1f);
                        if (rd != 0)
                        {
                            XRegisters[rd] = XRegisters[rd] << (int)shamt;
                        }
                        return;
                    }
                    case 0x1: // c.fldsp
                    {
                        var rd = (inst >> 7) & 0x1f;
                        // offset[5|4:3|8:6] = inst[12|6:5|4:2]
                        var offset = ((inst << 4) & 0x1c0) // offset[8:6]
                                     | ((inst >> 7) & 0x20) // offset[5]
                                     | ((inst >> 2) & 0x18); // offset[4:3]
                        var val = Read(XRegisters[2] + offset, Bits.DoubleWord);
                        FRegisters[rd] = Unsafe.As<ulong, double>(ref val);
                        return;
                    }
                    case 0x2: // c.lwsp
                    {
                        var rd = (inst >> 7) & 0x1f;
                        // offset[5|4:2|7:6] = inst[12|6:4|3:2]
                        var offset = ((inst << 4) & 0xc0) // offset[7:6]
                                     | ((inst >> 7) & 0x20) // offset[5]
                                     | ((inst >> 2) & 0x1c); // offset[4:2]
                        var val = Read(XRegisters[2] + offset, Bits.Word);
                        XRegisters[rd] = (ulong)(long)(int)val;
                        return;
                    }
                    case 0x3: // c.ldsp
                    {
                        var rd = (inst >> 7) & 0x1f;
                        // offset[5|4:3|8:6] = inst[12|6:5|4:2]
                        var offset = ((inst << 4) & 0x1c0) // offset[8:6]
                                     | ((inst >> 7) & 0x20) // offset[5]
                                     | ((inst >> 2) & 0x18); // offset[4:3]
                        var val = Read(XRegisters[2] + offset, Bits.DoubleWord);
                        XRegisters[rd] = val;
                        return;
                    }
                    case 0x4:
                    {
                        switch ((inst >> 12) & 0x1, (inst >> 2) & 0x1f)
                        {
                            case (0, 0): // c.jr
                            {
                                var rs1 = (inst >> 7) & 0x1f;
                                if (rs1 != 0)
                                {
                                    PC = XRegisters[rs1] - 2;
                                }
                                return;
                            }
                            case (0, _):  // c.mv
                            {
                                var rd = (inst >> 7) & 0x1f;
                                var rs2 = (inst >> 2) & 0x1f;
                                if (rs2 != 0)
                                {
                                    XRegisters[rd] = XRegisters[rs2];
                                }
                                return;
                            }
                            case (1, 0):
                            {
                                var rd = (inst >> 7) & 0x1f;
                                if (rd == 0) // c.ebreak
                                {
                                    throw new Breakpoint();
                                }
                                else // c.jalr
                                {
                                    var rs1 = (inst >> 7) & 0x1f;
                                    var t = PC + 2;
                                    PC = XRegisters[rs1] - 2;
                                    XRegisters[1] = t;
                                }
                                return;
                            }
                            case (1, _): // // c.add
                            {
                                var rd = (inst >> 7) & 0x1f;
                                var rs2 = (inst >> 2) & 0x1f;
                                if (rs2 != 0)
                                {
                                    XRegisters[rd] += XRegisters[rs2];
                                }
                                return;
                            }
                            default:
                                throw new IllegalInstruction(inst);
                        }
                    }
                    case 0x5: // c.fsdsp
                    {
                        var rs2 = (inst >> 2) & 0x1f;
                        // offset[5:3|8:6] = isnt[12:10|9:7]
                        var offset = ((inst >> 1) & 0x1c0) // offset[8:6]
                                     | ((inst >> 7) & 0x38); // offset[5:3]
                        var addr = XRegisters[2] + offset;
                        Write(addr, Unsafe.As<double, ulong>(ref FRegisters[rs2]), Bits.DoubleWord);
                        return;
                    }
                    case 0x6: // c.swsp
                    {
                        var rs2 = (inst >> 2) & 0x1f;
                        // offset[5:2|7:6] = inst[12:9|8:7]
                        var offset = ((inst >> 1) & 0xc0) // offset[7:6]
                                     | ((inst >> 7) & 0x3c); // offset[5:2]
                        var addr = XRegisters[2] + offset;
                        Write(addr, XRegisters[rs2], Bits.Word);
                        return;
                    }
                    case 0x7:
                    {
                        var rs2 = (inst >> 2) & 0x1f;
                        // offset[5:3|8:6] = isnt[12:10|9:7]
                        var offset = ((inst >> 1) & 0x1c0) // offset[8:6]
                                     | ((inst >> 7) & 0x38); // offset[5:3]
                        var addr = XRegisters[2] + offset;
                        Write(addr, XRegisters[rs2], Bits.DoubleWord);
                        return;
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
        }
    }

    private void ExecuteGeneral(ulong inst)
    {
        var opcode = inst & 0x0000007f;
        var rd = (inst & 0x00000f80) >> 7;
        var rs1 = (inst & 0x000f8000) >> 15;
        var rs2 = (inst & 0x01f00000) >> 20;
        var funct3 = (inst & 0x00007000) >> 12;
        var funct7 = (inst & 0xfe000000) >> 25;

        switch (opcode)
        {
            case 0x03: // RV32I and RV64I
            {
                var offset = (ulong)(((long)(int)inst) >> 20);
                var addr = XRegisters[rs1] + offset;
                switch (funct3)
                {
                    case 0x0: // lb
                    {
                        var val = Read(addr, Bits.Byte);
                        XRegisters[rd] = (ulong)(long)(sbyte)val;
                        return;
                    }
                    case 0x1: // lh
                    {
                        var val = Read(addr, Bits.HalfWord);
                        XRegisters[rd] = (ulong)(long)(short)val;
                        return;
                    }
                    case 0x2: // lw
                    {
                        var val = Read(addr, Bits.Word);
                        XRegisters[rd] = (ulong)(long)val;
                        return;
                    }
                    case 0x3: // ld
                    {
                        var val = Read(addr, Bits.DoubleWord);
                        XRegisters[rd] = val;
                        return;
                    }
                    case 0x4: // lbu
                    {
                        var val = Read(addr, Bits.Byte);
                        XRegisters[rd] = val;
                        return;
                    }
                    case 0x5: // lhu
                    {
                        var val = Read(addr, Bits.HalfWord);
                        XRegisters[rd] = val;
                        return;
                    }
                    case 0x6: // lwu
                    {
                        var val = Read(addr, Bits.Word);
                        XRegisters[rd] = val;
                        return;
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
            case 0x07: // RV32D and RV64D
            {
                var offset = (ulong)(((long)(int)inst) >> 20);
                var addr = XRegisters[rs1] + offset;
                switch (funct3)
                {
                    case 0x2: // flw
                    {
                        var readed = (uint)Read(addr, Bits.Word);
                        var val = Unsafe.As<uint, float>(ref readed);
                        FRegisters[rd] = val;
                        return;
                    }
                    case 0x3: // fld
                    {
                        var readed = Read(addr, Bits.DoubleWord);
                        var val = Unsafe.As<ulong, double>(ref readed);
                        FRegisters[rd] = val;
                        return;
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
            case 0x0f: // RV32I and RV64I
            {
                switch (funct3)
                {
                    case 0x0: // fence
                    case 0x1: // fence.i
                    {
                        return;
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
            case 0x13:
            {
                var imm = (ulong)(((long)(int)inst) >> 20);
                var funct6 = funct7 >> 1;
                switch (funct3)
                {
                    case 0x0: // addi
                    {
                        XRegisters[rd] = XRegisters[rs1] + imm;
                        return;
                    }
                    case 0x1: // slli
                    {
                        var shamt = (inst >> 20) & 0x3f;
                        XRegisters[rd] = XRegisters[rs1] << (int)shamt;
                        return;
                    }
                    case 0x2: // slti
                    {
                        XRegisters[rd] = (long)XRegisters[rs1] < (long)imm ? 1u : 0u;
                        return;
                    }
                    case 0x3: // sltiu
                    {
                        XRegisters[rd] = XRegisters[rs1] < imm ? 1u : 0u;
                        return;
                    }
                    case 0x4: // xori
                    {
                        XRegisters[rd] = XRegisters[rs1] ^ imm;
                        return;
                    }
                    case 0x5:
                    {
                        switch (funct6)
                        {
                            case 0x00: // srli
                            {
                                var shamt = (inst >> 20) & 0x3f;
                                XRegisters[rd] = XRegisters[rs1] >> (int)shamt;
                                return;
                            }
                            case 0x10: // srai
                            {
                                var shamt = (inst >> 20) & 0x3f;
                                XRegisters[rd] = (ulong)(((long)XRegisters[rs1]) >> (int)shamt);
                                return;
                            }
                            default:
                                throw new IllegalInstruction(inst);
                        }
                    }
                    case 0x6: // ori
                    {
                        XRegisters[rd] = XRegisters[rs1] | imm;
                        return;
                    }
                    case 0x7: // andi
                    {
                        XRegisters[rd] = XRegisters[rs1] & imm;
                        return;
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
            case 0x17: // RV32I // auipc
            {
                var imm = (ulong)(long)(int)(inst & 0xfffff000);
                XRegisters[rd] = PC + imm;
                return;
            }
            case 0x1b: // RV64I
            {
                var imm = (ulong)(((long)(int)inst) >> 20);
                switch (funct3)
                {
                    case 0x0: // addiw
                    {
                        XRegisters[rd] = (ulong)(long)(int)(XRegisters[rs1] + imm);
                        return;
                    }
                    case 0x1: // slliw
                    {
                        var shamt = (int)(imm & 0x1f);
                        XRegisters[rd] = (ulong)(long)(int)(XRegisters[rs1] << shamt);
                        return;
                    }
                    case 0x5:
                    {
                        switch (funct7)
                        {
                            case 0x0: // srliw
                            {
                                var shamt = (int)(imm & 0x1f);
                                XRegisters[rd] = (ulong)(long)(int)(XRegisters[rs1] >>> shamt);
                                return;
                            }
                            case 0x20: // sraiw
                            {
                                var shamt = (int)(imm & 0x1f);
                                XRegisters[rd] = (ulong)(long)(XRegisters[rs1] >>> shamt);
                                return;
                            }
                            default:
                                throw new IllegalInstruction(inst);
                        }
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
            case 0x23: // RV32I
            {
                var offset = ((ulong)((long)(int)(inst & 0xfe000000)) >> 20) | ((inst >> 7) & 0x1f);
                var addr = XRegisters[rs1] + offset;

                switch (funct3)
                {
                    case 0x0: // sb
                    {
                        Write(addr, XRegisters[rs2], Bits.Byte);
                        return;
                    }
                    case 0x1: // sh
                    {
                        Write(addr, XRegisters[rs2], Bits.HalfWord);
                        return;
                    }
                    case 0x2: // sw
                    {
                        Write(addr, XRegisters[rs2], Bits.Word);
                        return;
                    }
                    case 0x3: // sd
                    {
                        Write(addr, XRegisters[rs2], Bits.DoubleWord);
                        return;
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
            case 0x27: // RV32F and RV64F
            {
                var offset = (((ulong)((long)(int)inst) >> 20) & 0xfe0) | ((inst >> 7) & 0x1f);
                var addr = XRegisters[rs1] + offset;
                switch (funct3)
                {
                    case 0x2: // fsw
                    {
                        var v = (float)FRegisters[rs2];
                        Write(addr, Unsafe.As<float, uint>(ref v), Bits.Word);
                        return;
                    }
                    case 0x3: // fsd
                    {
                        Write(addr, Unsafe.As<double, ulong>(ref FRegisters[rs2]), Bits.DoubleWord);
                        return;
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
            case 0x2f:
            {
                // RV32A and RV64A
                var funct5 = (funct7 & 0b1111100) >> 2;
                // TODO: Handle `aq` and `rl`.
                //var _aq = (funct7 & 0b0000010) >> 1; // acquire access
                //var _rl = funct7 & 0b0000001; // release access

                switch (funct3, funct5)
                {
                    case (0x2, 0x00): // amoadd.w
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 4 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var t = Read(addr, Bits.Word);
                        Write(addr, t + XRegisters[rs2], Bits.Word);
                        XRegisters[rd] = (ulong)(long)(int)t;
                        return;
                    }
                    case (0x3, 0x00): // amoadd.d
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 8 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var t = Read(addr, Bits.DoubleWord);
                        Write(addr, t + XRegisters[rs2], Bits.DoubleWord);
                        XRegisters[rd] = t;
                        return;
                    }
                    case (0x2, 0x01): // amoswap.w
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 4 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var t = Read(addr, Bits.Word);
                        Write(addr, XRegisters[rs2], Bits.Word);
                        XRegisters[rd] = (ulong)(long)(int)t;
                        return;
                    }
                    case (0x3, 0x01): // amoswap.d
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 8 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var t = Read(addr, Bits.DoubleWord);
                        Write(addr, XRegisters[rs2], Bits.DoubleWord);
                        XRegisters[rd] = t;
                        return;
                    }
                    case (0x2, 0x02): // lr.w
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 4 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var value = Read(addr, Bits.Word);
                        XRegisters[rd] = (ulong)(long)(int)value;
                        ReservationSet.Add(addr);
                        return;
                    }
                    case (0x3, 0x02): // lr.d
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 8 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var value = Read(addr, Bits.DoubleWord);
                        XRegisters[rd] = value;
                        ReservationSet.Add(addr);
                        return;
                    }
                    case (0x2, 0x03): // sc.w
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 4 != 0)
                        {
                            throw new StoreAMOAddressMisaligned();
                        }
                        if (ReservationSet.Contains(addr))
                        {
                            ReservationSet.RemoveAll(x => x == addr);
                            Write(addr, XRegisters[rs2], Bits.Word);
                            XRegisters[rd] = 0;
                        }
                        else
                        {
                            //  self.reservation_set.retain(|&x| x != addr); чел зачем-то еще раз добавил сюда, хотя это ничего не сделает. Раскомментить, если будет пиздец
                            XRegisters[rd] = 1;
                        }
                        return;
                    }
                    case (0x3, 0x03):
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 8 != 0)
                        {
                            throw new StoreAMOAddressMisaligned();
                        }
                        if (ReservationSet.Contains(addr))
                        {
                            ReservationSet.RemoveAll(x => x == addr);
                            Write(addr, XRegisters[rs2], Bits.DoubleWord);
                            XRegisters[rd] = 0;
                        }
                        else
                        {
                            //  self.reservation_set.retain(|&x| x != addr); чел зачем-то еще раз добавил сюда, хотя это ничего не сделает. Раскомментить, если будет пиздец
                            XRegisters[rd] = 1;
                        }
                        return;
                    }
                    case (0x2, 0x04): // amoxor.w
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 4 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var t = Read(addr, Bits.Word);
                        Write(addr, (ulong)(long)((int)t ^ ((int)XRegisters[rs2])), Bits.Word);
                        XRegisters[rd] = (ulong)(long)(int)t;
                        return;
                    }
                    case (0x3, 0x04): // amoxor.d
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 8 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var t = Read(addr, Bits.DoubleWord);
                        Write(addr, t ^ XRegisters[rs2], Bits.DoubleWord);
                        XRegisters[rd] = t;
                        return;
                    }
                    case (0x2, 0x08):  // amoor.w
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 4 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var t = Read(addr, Bits.Word);
                        Write(addr, (ulong)(long)((int)t | ((int)XRegisters[rs2])), Bits.Word);
                        XRegisters[rd] = (ulong)(long)(int)t;
                        return;
                    }
                    case (0x3, 0x08): // amoor.d
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 8 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var t = Read(addr, Bits.DoubleWord);
                        Write(addr, t | XRegisters[rs2], Bits.DoubleWord);
                        XRegisters[rd] = t;
                        return;
                    }
                    case (0x2, 0x0c):  // amoand.w
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 4 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var t = Read(addr, Bits.Word);
                        Write(addr, (ulong)(long)((int)t & ((int)XRegisters[rs2])), Bits.Word);
                        XRegisters[rd] = (ulong)(long)(int)t;
                        return;
                    }
                    case (0x3, 0x0c): // amoand.d
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 8 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var t = Read(addr, Bits.DoubleWord);
                        Write(addr, t & XRegisters[rs2], Bits.DoubleWord);
                        XRegisters[rd] = t;
                        return;
                    }
                    case (0x2, 0x10):  // amomin.w
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 4 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var t = Read(addr, Bits.Word);
                        Write(addr, (ulong)(long)(int.Min((int)t, ((int)XRegisters[rs2]))), Bits.Word);
                        XRegisters[rd] = (ulong)(long)(int)t;
                        return;
                    }
                    case (0x3, 0x10): // amomin.d
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 8 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var t = Read(addr, Bits.DoubleWord);
                        Write(addr, ulong.Min(t, XRegisters[rs2]), Bits.DoubleWord);
                        XRegisters[rd] = t;
                        return;
                    }
                    case (0x2, 0x14):  // amomax.w
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 4 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var t = Read(addr, Bits.Word);
                        Write(addr, (ulong)(long)(int.Max((int)t, ((int)XRegisters[rs2]))), Bits.Word);
                        XRegisters[rd] = (ulong)(long)(int)t;
                        return;
                    }
                    case (0x3, 0x14): // amomax.d
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 8 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var t = Read(addr, Bits.DoubleWord);
                        Write(addr, (ulong)long.Max((long)t, (long)XRegisters[rs2]), Bits.DoubleWord);
                        XRegisters[rd] = t;
                        return;
                    }
                    case (0x2, 0x18):  // amominu.w
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 4 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var t = Read(addr, Bits.Word);
                        Write(addr, (ulong)(uint.Min((uint)t, ((uint)XRegisters[rs2]))), Bits.Word);
                        XRegisters[rd] = t;
                        return;
                    }
                    case (0x3, 0x18): // amominu.d
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 8 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var t = Read(addr, Bits.DoubleWord);
                        Write(addr, ulong.Min(t, XRegisters[rs2]), Bits.DoubleWord);
                        XRegisters[rd] = t;
                        return;
                    }
                    case (0x2, 0x1c):  // amomaxu.w
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 4 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var t = Read(addr, Bits.Word);
                        Write(addr, (ulong)(uint.Max((uint)t, ((uint)XRegisters[rs2]))), Bits.Word);
                        XRegisters[rd] = t;
                        return;
                    }
                    case (0x3, 0x1c): // amomaxu.d
                    {
                        var addr = XRegisters[rs1];
                        if (addr % 8 != 0)
                        {
                            throw new LoadAddressMisaligned();
                        }
                        var t = Read(addr, Bits.DoubleWord);
                        Write(addr, ulong.Max(t, XRegisters[rs2]), Bits.DoubleWord);
                        XRegisters[rd] = t;
                        return;
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
            case 0x33: // RV64I and RV64M
            {
                switch (funct3, funct7)
                {
                    case (0x0, 0x00): // add
                    {
                        XRegisters[rd] = XRegisters[rs1] + XRegisters[rs2];
                        return;
                    }
                    case (0x0, 0x01): // mul
                    {
                        XRegisters[rd] = (ulong)((long)XRegisters[rs1] * (long)XRegisters[rs2]);
                        return;
                    }
                    case (0x0, 0x20): // sub
                    {
                        XRegisters[rd] = XRegisters[rs1] - XRegisters[rs2];
                        return;
                    }
                    case (0x1, 0x00): // sll
                    {
                        var shamt = XRegisters[rs2] & 0x3f;
                        XRegisters[rd] = XRegisters[rs1] << (int)shamt;
                        return;
                    }
                    case (0x1, 0x01): // mulh
                    {
                        XRegisters[rd] = (ulong)(((Int128)(long)XRegisters[rs1] * (Int128)(long)XRegisters[rs2]) >> 64);
                        return;
                    }
                    case (0x2, 0x00): // slt
                    {
                        XRegisters[rd] = (long)XRegisters[rs1] < (long)XRegisters[rs2] ? 1u : 0u;
                        return;
                    }
                    case (0x2, 0x01): // mulhsu
                    {
                        XRegisters[rd] = (ulong)(((UInt128)(Int128)(long)XRegisters[rs1] * (UInt128)XRegisters[rs2]) >> 64);
                        return;
                    }
                    case (0x3, 0x00): // sltu
                    {
                        XRegisters[rd] = XRegisters[rs1] < XRegisters[rs2] ? 1u : 0u;
                        return;
                    }
                    case (0x3, 0x01): // mulhu
                    {
                        XRegisters[rd] = (ulong)(((UInt128)XRegisters[rs1] * (UInt128)XRegisters[rs2]) >> 64);
                        return;
                    }
                    case (0x4, 0x00): // xor
                    {
                        XRegisters[rd] = XRegisters[rs1] ^ XRegisters[rs2];
                        return;
                    }
                    case (0x4, 0x01): // div
                    {
                        var dividend = (long)XRegisters[rs1];
                        var divisor = (long)XRegisters[rs2];
                        if (divisor == 0)
                        {
                            State.WriteBit(State.FCSR, 3, 1);
                            XRegisters[rd] = ulong.MaxValue;
                        }
                        else if (dividend == long.MinValue && divisor == -1)
                        {
                            XRegisters[rd] = unchecked((ulong)dividend);
                        }
                        else
                        {
                            XRegisters[rd] = unchecked((ulong)(dividend / divisor));
                        }

                        return;
                    }
                    case (0x5, 0x00): // srl
                    {
                        var shamt = XRegisters[rs2] & 0x3f;
                        XRegisters[rd] = XRegisters[rs1] >> (int)shamt;
                        return;
                    }
                    case (0x5, 0x01): // divu
                    {
                        var dividend = XRegisters[rs1];
                        var divisor = XRegisters[rs2];
                        if (divisor == 0)
                        {
                            State.WriteBit(State.FCSR, 3, 1);
                            XRegisters[rd] = ulong.MaxValue;
                        }
                        else
                        {
                            XRegisters[rd] = dividend / divisor;
                        }
                        return;
                    }
                    case (0x5, 0x20): // sra
                    {
                        var shamt = XRegisters[rs2] & 0x3f;
                        XRegisters[rd] = (ulong)(((long)XRegisters[rs1]) >> (int)shamt);
                        return;
                    }
                    case (0x6, 0x00): // or
                    {
                        XRegisters[rd] = XRegisters[rs1] | XRegisters[rs2];
                        return;
                    }
                    case (0x6, 0x01): // rem
                    {
                        var dividend = (long)XRegisters[rs1];
                        var divisor = (long)XRegisters[rs2];
                        if (divisor == 0)
                        {
                            XRegisters[rd] = (ulong)dividend;
                        }
                        else if (dividend == long.MinValue && divisor == -1)
                        {
                            XRegisters[rd] = 0;
                        }
                        else
                        {
                            XRegisters[rd] = unchecked((ulong)(dividend % divisor));
                        }

                        return;
                    }
                    case (0x7, 0x00): // and
                    {
                        XRegisters[rd] = XRegisters[rs1] & XRegisters[rs2];
                        return;
                    }
                    case (0x7, 0x01): // remu
                    {
                        var dividend = XRegisters[rs1];
                        var divisor = XRegisters[rs2];
                        if (divisor == 0)
                        {
                            XRegisters[rd] = dividend;
                        }
                        else
                        {
                            XRegisters[rd] = dividend % divisor;
                        }

                        return;
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
            case 0x37: // RV32I // lui
            {
                XRegisters[rd] = (ulong)((long)(int)(inst & 0xfffff000));
                return;
            }
            case 0x3b: // RV64I and RV64M
            {
                switch (funct3, funct7)
                {
                    case (0x0, 0x00): // addw
                    {
                        XRegisters[rd] = (ulong)(long)((int)XRegisters[rs1] + (int)XRegisters[rs2]);
                        return;
                    }
                    case (0x0, 0x01): // mulw
                    {
                        XRegisters[rd] = (ulong)(long)((int)XRegisters[rs1] * (int)XRegisters[rs2]);
                        return;
                    }
                    case (0x0, 0x20): // subw
                    {
                        XRegisters[rd] = (ulong)(long)((int)XRegisters[rs1] - (int)XRegisters[rs2]);
                        return;
                    }
                    case (0x1, 0x00): // sllw
                    {
                        var shamt = (int)XRegisters[rs2] & 0x1f;
                        XRegisters[rd] = (ulong)(long)((int)XRegisters[rs1] << shamt);
                        return;
                    }
                    case (0x4, 0x01): // divw
                    {
                        var dividend = (int)XRegisters[rs1];
                        var divisor = (int)XRegisters[rs2];
                        if (divisor == 0)
                        {
                            State.WriteBit(State.FCSR, 3, 1);
                            XRegisters[rd] = ulong.MaxValue;
                        }
                        else if (dividend == int.MinValue && divisor == -1)
                        {
                            XRegisters[rd] = (ulong)(long)dividend;
                        }
                        else
                        {
                            XRegisters[rd] = (ulong)(long)(dividend / divisor);
                        }
                        return;
                    }
                    case (0x5, 0x00): // srlw
                    {
                        var shamt = (int)XRegisters[rs2] & 0x1f;
                        XRegisters[rd] = ((uint)XRegisters[rs1]) >> shamt;
                        return;
                    }
                    case (0x5, 0x01): // divuw ^^
                    {
                        var dividend = (uint)XRegisters[rs1];
                        var divisor = (uint)XRegisters[rs2];
                        if (divisor == 0)
                        {
                            State.WriteBit(State.FCSR, 3, 1);
                            XRegisters[rd] = ulong.MaxValue;
                        }
                        else
                        {
                            XRegisters[rd] = (ulong)(dividend / divisor);
                        }
                        return;
                    }
                    case (0x5, 0x20): // sraw
                    {
                        var shamt = (int)XRegisters[rs2] & 0x1f;
                        XRegisters[rd] = (ulong)(long)((int)XRegisters[rs1]) >> shamt;
                        return;
                    }
                    case (0x6, 0x01): // remw
                    {
                        var dividend = (int)XRegisters[rs1];
                        var divisor = (int)XRegisters[rs2];
                        if (divisor == 0)
                        {
                            XRegisters[rd] = (ulong)(long)dividend;
                        }
                        else if (dividend == int.MinValue && divisor == -1)
                        {
                            XRegisters[rd] = 0;
                        }
                        else
                        {
                            XRegisters[rd] = (ulong)(long)(dividend % divisor);
                        }
                        return;
                    }
                    case (0x7, 0x01): // remuw
                    {
                        var dividend = (uint)XRegisters[rs1];
                        var divisor = (uint)XRegisters[rs2];
                        if (divisor == 0)
                        {
                            XRegisters[rd] = (ulong)dividend;
                        }
                        else
                        {
                            XRegisters[rd] = (ulong)(dividend % divisor);
                        }
                        return;
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
            case 0x43: // RV32F and RV64F
            {
                // не реализован rounding mode encoding (rm), так как в rvemu нет. :(
                var rs3 = (inst & 0xf8000000) >> 27;
                var funct2 = (inst & 0x03000000) >> 25;
                switch (funct2)
                {
                    case 0x0: // fmadd.s
                    {
                        FRegisters[rd] = ((float)FRegisters[rs1] * (float)FRegisters[rs2]) + (float)FRegisters[rs3];
                        return;
                    }
                    case 0x1: // fmadd.d
                    {
                        FRegisters[rd] = (FRegisters[rs1] * FRegisters[rs2]) + FRegisters[rs3];
                        return;
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
            case 0x47: // RV32F and RV64F
            {
                // чек 0x43
                var rs3 = (inst & 0xf8000000) >> 27;
                var funct2 = (inst & 0x03000000) >> 25;
                switch (funct2)
                {
                    case 0x0: // fmsub.s
                    {
                        FRegisters[rd] = ((float)FRegisters[rs1] * (float)FRegisters[rs2]) - (float)FRegisters[rs3];
                        return;
                    }
                    case 0x1: // fmsub.d
                    {
                        FRegisters[rd] = (FRegisters[rs1] * FRegisters[rs2]) - FRegisters[rs3];
                        return;
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
            case 0x4b: // RV32F and RV64F
            {
                // пупупу, чек 0x47
                var rs3 = (inst & 0xf8000000) >> 27;
                var funct2 = (inst & 0x03000000) >> 25;
                switch (funct2)
                {
                    case 0x0: // fnmadd.s
                    {
                        FRegisters[rd] = -(((float)FRegisters[rs1] * (float)FRegisters[rs2]) + (float)FRegisters[rs3]);
                        return;
                    }
                    case 0x1: // fnmadd.d
                    {
                        FRegisters[rd] = -((FRegisters[rs1] * FRegisters[rs2]) + FRegisters[rs3]);
                        return;
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
            case 0x4f: // RV32F and RV64F
            {
                // кхм, чек 0x4b
                var rs3 = (inst & 0xf8000000) >> 27;
                var funct2 = (inst & 0x03000000) >> 25;
                switch (funct2)
                {
                    case 0x0: // fnmsub.s
                    {
                        FRegisters[rd] = -(((float)FRegisters[rs1] * (float)FRegisters[rs2]) - (float)FRegisters[rs3]);
                        return;
                    }
                    case 0x1: // fnmsub.d
                    {
                        FRegisters[rd] = -((FRegisters[rs1] * FRegisters[rs2]) - FRegisters[rs3]);
                        return;
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
            case 0x53: // RV32F and RV64F
            {
                // чек 0x4f + в rvemu не сделали set exception flags и NaN Boxing of Narrower Values
                // пупупупу, ныряем в плавающее говно *badum-tss*
                switch (State.ReadBits(State.FCSR, 5..8))
                {
                    case 0b000:
                    case 0b001:
                    case 0b011:
                    case 0b100:
                    case 0b111:
                        break;
                    default:
                        throw new IllegalInstruction(inst);
                }

                switch (funct7)
                {
                    case 0x00: // fadd.s
                    {
                        FRegisters[rd] = (float)FRegisters[rs1] + (float)FRegisters[rs2];
                        return;
                    }
                    case 0x01: // fadd.d
                    {
                        FRegisters[rd] = FRegisters[rs1] + FRegisters[rs2];
                        return;
                    }
                    case 0x04: // fsub.s
                    {
                        FRegisters[rd] = (float)FRegisters[rs1] - (float)FRegisters[rs2];
                        return;
                    }
                    case 0x05: // fsub.d
                    {
                        FRegisters[rd] = FRegisters[rs1] - FRegisters[rs2];
                        return;
                    }
                    case 0x08: // fmul.s
                    {
                        FRegisters[rd] = (float)FRegisters[rs1] * (float)FRegisters[rs2];
                        return;
                    }
                    case 0x09: // fmul.d
                    {
                        FRegisters[rd] = FRegisters[rs1] * FRegisters[rs2];
                        return;
                    }
                    // пацаны, только не делите на 0, пж, я не хочу гуглить какой флаг ставить,
                    // чтобы исправить недоработку в rvemu
                    case 0x0c: // fdiv.s
                    {
                        FRegisters[rd] = (float)FRegisters[rs1] / (float)FRegisters[rs2];
                        return;
                    }
                    case 0x0d: // fdiv.d
                    {
                        FRegisters[rd] = FRegisters[rs1] / FRegisters[rs2];
                        return;
                    }
                    case 0x10:
                    {
                        switch (funct3)
                        {
                            case 0x0: // fsgnj.s
                            {
                                // фига че есть copysign прям как в расте
                                FRegisters[rd] = double.CopySign(FRegisters[rs1], FRegisters[rs2]);
                                return;
                            }
                            case 0x1: // fsgnjn.s
                            {
                                FRegisters[rd] = double.CopySign(FRegisters[rs1], -FRegisters[rs2]);
                                return;
                            }
                            case 0x2: // fsgnjx.s
                            {
                                // АААААААААААААААААААААА
                                var _sign1 = (float)FRegisters[rs1];
                                var _sign2 = (float)FRegisters[rs1];
                                var _other = (float)FRegisters[rs1];
                                var sign1 = Unsafe.As<float, uint>(ref _sign1) & 0x80000000;
                                var sign2 = Unsafe.As<float, uint>(ref _sign2) & 0x80000000;
                                var other = Unsafe.As<float, uint>(ref _other) & 0x7fffffff;
                                var pupupupupu = sign1 ^ sign2 | other;
                                FRegisters[rd] = Unsafe.As<uint, float>(ref pupupupupu);
                                return;
                            }
                            default:
                                throw new IllegalInstruction(inst);
                        }
                    }
                    case 0x11:
                    {
                        switch (funct3)
                        {
                            case 0x0: // fsgnj.d
                            {
                                FRegisters[rd] = double.CopySign(FRegisters[rs1], FRegisters[rs2]);
                                return;
                            }
                            case 0x1: // fsgnjn.d
                            {
                                FRegisters[rd] = double.CopySign(FRegisters[rs1], -FRegisters[rs2]);
                                return;
                            }
                            case 0x2: // fsgnjx.d
                            {
                                // АААААААААААААААААААААА x2
                                var _sign1 = FRegisters[rs1];
                                var _sign2 = FRegisters[rs1];
                                var _other = FRegisters[rs1];
                                var sign1 = Unsafe.As<double, ulong>(ref _sign1) & 0x80000000_00000000;
                                var sign2 = Unsafe.As<double, ulong>(ref _sign2) & 0x80000000_00000000;
                                var other = Unsafe.As<double, ulong>(ref _other) & 0x7fffffff_ffffffff;
                                var pupupupupu = sign1 ^ sign2 | other;
                                FRegisters[rd] = Unsafe.As<ulong, double>(ref pupupupupu);
                                return;
                            }
                            default:
                                throw new IllegalInstruction(inst);
                        }
                    }
                    case 0x14:
                    case 0x15:
                    {
                        switch (funct3)
                        {
                            case 0x0:  // fmin.s fmin.d
                            {
                                FRegisters[rd] = double.Min(FRegisters[rs1], FRegisters[rs2]);
                                return;
                            }
                            case 0x1:  // fmax.s fmax.d
                            {
                                FRegisters[rd] = double.Max(FRegisters[rs1], FRegisters[rs2]);
                                return;
                            }
                            default:
                                throw new IllegalInstruction(inst);
                        }
                    }
                    case 0x20: // fcvt.s.d
                    {
                        FRegisters[rd] = FRegisters[rs1];
                        return;
                    }
                    case 0x21: // fcvt.s.d
                    {
                        // не ищите смысл, я просто перевожу с rust на c#
                        FRegisters[rd] = (float)FRegisters[rs1];
                        return;
                        // а может я проглядел что-то и тут должна вызываться какая-то функция
                    }
                    case 0x2c: // fsqrt.s
                    {
                        FRegisters[rd] = float.Sqrt((float)FRegisters[rs1]);
                        return;
                    }
                    case 0x2d: // fsqrt.d
                    {
                        FRegisters[rd] = double.Sqrt(FRegisters[rs1]);
                        return;
                    }
                    case 0x50: //
                    case 0x51: // как 50, только отрезали .s и пришили .d
                    {
                        switch (funct3)
                        {
                            case 0x0: // fle.s
                            {
                                XRegisters[rd] = FRegisters[rs1] <= FRegisters[rs2] ? 1u : 0u;
                                return;
                            }
                            case 0x1: // flt.s
                            {
                                XRegisters[rd] = FRegisters[rs1] < FRegisters[rs2] ? 1u : 0u;
                                return;
                            }
                            case 0x2: // feq.s
                            {
                                // а ну цыц
                                // ReSharper disable once CompareOfFloatsByEqualityOperator
                                XRegisters[rd] = FRegisters[rs1] == FRegisters[rs2] ? 1u : 0u;
                                return;
                            }
                            default:
                                throw new IllegalInstruction(inst);
                        }
                    }
                    case 0x60:
                    case 0x61: // .d вместо .s
                    {
                        switch (rs2)
                        {
                            case 0x0: // fcvt.w.s
                            {
                                XRegisters[rd] = (ulong)(long)float.Round((float)FRegisters[rs1]);
                                return;
                            }
                            case 0x1: // fcvt.wu.s
                            {
                                XRegisters[rd] = (ulong)(int)(uint)float.Round((float)FRegisters[rs1]);
                                return;
                            }
                            case 0x2: // fcvt.l.s
                            {
                                // в emu не делали -> i64 -> u64, но инфа сотка чел просто заснул как я
                                XRegisters[rd] = (ulong)(long)float.Round((float)FRegisters[rs1]);
                                return;
                            }
                            case 0x3: // fcvt.lu.s
                            {
                                XRegisters[rd] = (ulong)float.Round((float)FRegisters[rs1]);
                                return;
                            }
                            default:
                                throw new IllegalInstruction(inst);
                        }
                    }
                    case 0x68:
                    case 0x69: // .s --> .d
                    {
                        switch (rs2)
                        {
                            case 0x0: // fcvt.s.w
                            {
                                FRegisters[rd] = (int)XRegisters[rs1];
                                return;
                            }
                            case 0x1: // fcvt.s.wu
                            {
                                FRegisters[rd] = (uint)XRegisters[rs1];
                                return;
                            }
                            case 0x2: // fcvt.s.l
                            {
                                FRegisters[rd] = (long)XRegisters[rs1];
                                return;
                            }
                            case 0x3: // fcvt.s.lu
                            {
                                FRegisters[rd] = (ulong)XRegisters[rs1];
                                return;
                            }
                            default:
                                throw new IllegalInstruction(inst);
                        }
                    }
                    case 0x70:
                    {
                        switch (funct3)
                        {
                            case 0x0: // fmv.x.w
                            {
                                var _f = (float)FRegisters[rs1];
                                XRegisters[rd] = (ulong)(long)(int)(Unsafe.As<float, uint>(ref _f) & 0xffffffff);
                                return;
                            }
                            case 0x1: // fclass.s
                            {
                                var f = FRegisters[rs1];
                                if (double.IsInfinity(f))
                                    XRegisters[rd] = double.IsPositiveInfinity(f) ? 0u : 7u;
                                else if (double.IsSubnormal(f))
                                    XRegisters[rd] = double.IsNegative(rd) ? 2u : 5u;
                                else if (f == 0)
                                    XRegisters[rd] = double.IsNegative(rd) ? 3u : 4u;
                                else if (double.IsNaN(f))
                                    XRegisters[rd] = 9;
                                else // normal
                                    XRegisters[rd] = double.IsNegative(rd) ? 1u : 6u;
                                return;
                            }
                            default:
                                throw new IllegalInstruction(inst);
                        }
                    }
                    case 0x71:
                    {
                        switch (funct3)
                        {
                            case 0x0: // fmv.x.d
                            {
                                var _f = FRegisters[rs1];
                                XRegisters[rd] = Unsafe.As<double, ulong>(ref _f);
                                return;
                            }
                            case 0x1: // fclass.d
                            {
                                var f = FRegisters[rs1];
                                if (double.IsInfinity(f))
                                    XRegisters[rd] = !double.IsPositiveInfinity(f) ? 0u : 7u;
                                else if (double.IsSubnormal(f))
                                    XRegisters[rd] = double.IsNegative(rd) ? 2u : 5u;
                                else if (f == 0)
                                    XRegisters[rd] = double.IsNegative(rd) ? 3u : 4u;
                                else if (double.IsNaN(f))
                                    XRegisters[rd] = 9;
                                else // normal
                                    XRegisters[rd] = double.IsNegative(rd) ? 1u : 6u;
                                return;
                            }
                            default:
                                throw new IllegalInstruction(inst);
                        }
                    }
                    case 0x78: // fmv.w.x
                    {
                        // cpu.rs 3196
                        var val = XRegisters[rs1] & 0xffffffff;
                        FRegisters[rd] = Unsafe.As<ulong, double>(ref val);
                        return;
                    }
                    case 0x79: // fmv.d.x
                    {
                        var val = XRegisters[rs1];
                        FRegisters[rd] = Unsafe.As<ulong, double>(ref val);
                        return;
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
            case 0x63: // RV32I
            {
                var imm = (ulong)(((long)(int)(inst & 0x80000000)) >> 19)
                          | ((inst & 0x80) << 4) // imm[11]
                          | ((inst >> 20) & 0x7e0) // imm[10:5]
                          | ((inst >> 7) & 0x1e); // imm[4:1]
                switch (funct3)
                {
                    case 0x0: // beq
                    {
                        if (XRegisters[rs1] == XRegisters[rs2])
                            PC += imm - 4;
                        return;
                    }
                    case 0x1: // bne
                    {
                        if (XRegisters[rs1] != XRegisters[rs2])
                            PC += imm - 4;
                        return;
                    }
                    case 0x4: // blt
                    {
                        if ((long)XRegisters[rs1] < (long)XRegisters[rs2])
                            PC += imm - 4;
                        return;
                    }
                    case 0x5: // bge
                    {
                        if ((long)XRegisters[rs1] >= (long)XRegisters[rs2])
                            PC += imm - 4;
                        return;
                    }
                    case 0x6: // bltu
                    {
                        if (XRegisters[rs1] < XRegisters[rs2])
                            PC += imm - 4;
                        return;
                    }
                    case 0x7: // bgeu
                    {
                        if (XRegisters[rs1] >= XRegisters[rs2])
                            PC += imm - 4;
                        return;
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
            case 0x67: // jalr
            {
                var t = PC + 4;
                var offset = ((long)(int)inst) >> 20;
                var target = ((long)XRegisters[rs1]) + (offset & ~1);
                PC = (ulong)target - 4;
                XRegisters[rd] = t;
                return;
            }
            case 0x6F: // jal
            {
                XRegisters[rd] = PC + 4;
                var offset = ((ulong)(((long)(int)(inst & 0x80000000)) >> 11)) // imm[20]
                             | (inst & 0xff000) // imm[19:12]
                             | ((inst >> 9) & 0x800) // imm[11]
                             | ((inst >> 20) & 0x7fe); // imm[10:1]
                PC += offset - 4;
                return;
            }
            case 0x73: // RV32I, RVZicsr, and supervisor ISA
            {
                var csrAddr = (ushort)((inst >> 20) & 0xfff);
                switch (funct3)
                {
                    case 0x0:
                    {
                        switch (rs2, funct7)
                        {
                            case (0x0, 0x0): // ecall
                            {
                                switch (Mode)
                                {
                                    case Mode.User:
                                        throw new EnvironmentCallFromUMode();
                                    case Mode.Supervisor:
                                        throw new EnvironmentCallFromSMode();
                                    case Mode.Machine:
                                        throw new EnvironmentCallFromMMode();
                                    default:
                                        throw new IllegalInstruction(inst);
                                }
                            }
                            case (0x1, 0x0):
                            {
                                throw new Breakpoint();
                            }
                            case (0x2, 0x0):
                            {
                                // cpu.rs 3352 там нет, значит и тут нет
                                throw new NotImplementedException("uret");
                            }
                            case (0x2, 0x8):
                            {
                                PC = State.Read(State.SPEC) + 4;
                                Mode = State.ReadSStatus(State.XSTATUS_SPP) switch
                                {
                                    0b0 => Mode.User,
                                    0b1 => Mode.Supervisor,
                                    _ => Mode.Debug
                                };
                                if (Mode == Mode.Supervisor)
                                    State.WriteMStatus(State.MSTATUS_MPRV, 0);

                                State.WriteSStatus(State.XSTATUS_SIE, State.ReadSStatus(State.XSTATUS_SPIE));
                                State.WriteSStatus(State.XSTATUS_SPIE, 1);
                                State.WriteSStatus(State.XSTATUS_SPP, 0);
                                return;
                            }
                            case (0x2, 0x18): // mret
                            {
                                PC = State.Read(State.MEPC) + 4;
                                Mode = State.ReadMStatus(State.MSTATUS_MPP) switch
                                {
                                    0b0 => Mode.User,
                                    0b1 => Mode.Supervisor,
                                    0b11 => Mode.Machine,
                                    _ => Mode.Debug
                                };
                                if (Mode == Mode.User || Mode == Mode.Supervisor)
                                    State.WriteMStatus(State.MSTATUS_MPRV, 0);

                                State.WriteMStatus(State.MSTATUS_MIE, State.ReadSStatus(State.MSTATUS_MPIE));
                                State.WriteMStatus(State.MSTATUS_MPIE, 1);
                                State.WriteMStatus(State.MSTATUS_MPP, (ulong)Mode.User);
                                return;
                            }
                            case (0x5, 0x8): // wfi
                            {
                                Idle = true; // спать, пока не стукнет interrupt или ручками не снимет PCI
                                return;
                            }
                            case (_, 0x9): // sfence.vma
                            case (_, 0x11): // sfence.bvma
                            case (_, 0x51): // sfence.hvma
                            {
                                // гайс у нас однопоток
                                // cpu.rs 3459 - 3473
                                return;
                            }
                            default:
                                throw new IllegalInstruction(inst);
                        }
                    }
                    case 0x1: // csrrw
                    {
                        var t = State.Read(csrAddr);
                        State.Write(csrAddr, XRegisters[rs1]);
                        XRegisters[rd] = t;
                        if (csrAddr == State.SATP)
                        {
                            // self.update_paging at cpu.rs 3490
                            // мне влом реализовывать систему страниц
                        }

                        return;
                    }
                    case 0x2: // csrrs
                    {
                        var t = State.Read(csrAddr);
                        State.Write(csrAddr, t | XRegisters[rs1]);
                        XRegisters[rd] = t;
                        if (csrAddr == State.SATP) {}
                        return;
                    }
                    case 0x3: // csrrc
                    {
                        var t = State.Read(csrAddr);
                        State.Write(csrAddr, t & (~XRegisters[rs1]));
                        XRegisters[rd] = t;
                        if (csrAddr == State.SATP) {}
                        return;
                    }
                    case 0x5: // csrrwi
                    {
                        var zimm = rs1;
                        XRegisters[rd] = State.Read(csrAddr);
                        State.Write(csrAddr, zimm);
                        if (csrAddr == State.SATP) {}
                        return;
                    }
                    case 0x6: // csrrsi
                    {
                        var zimm = rs1;
                        var t = State.Read(csrAddr);
                        State.Write(csrAddr, t | zimm);
                        XRegisters[rd] = t;
                        if (csrAddr == State.SATP) {}
                        return;
                    }
                    case 0x7: // csrrci
                    {
                        var zimm = rs1;
                        var t = State.Read(csrAddr);
                        State.Write(csrAddr, t & (~zimm));
                        XRegisters[rd] = t;
                        if (csrAddr == State.SATP) {}
                        return;
                    }
                    default:
                        throw new IllegalInstruction(inst);
                }
            }
            // вот и сказочки конец
            default:
                throw new IllegalInstruction(inst);
        }
    }

    public ulong Fetch(Bits size)
    {
        if (size != Bits.HalfWord && size != Bits.Word)
            throw new InstructionAccessFault();

        return Bus.Read(PC, size);
    }

    public ulong Read(ulong addr, Bits size) => Bus.Read(addr, size);

    public void Write(ulong addr, ulong value, Bits size) => Bus.Write(addr, value, size);

    public void Reset()
    {
        PC = 0;
        Mode = Mode.Machine;
        State.Reset();
        XRegisters.Reset();
        FRegisters.Reset();
    }
}
