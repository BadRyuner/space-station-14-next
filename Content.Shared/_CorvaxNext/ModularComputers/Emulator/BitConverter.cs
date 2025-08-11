namespace Content.Shared._CorvaxNext.ModularComputers.Emulator;

public ref struct BitConverter(ulong val)
{
    public ulong Ulong = val;

    public double Double => System.BitConverter.UInt64BitsToDouble(Ulong);

    //public float FloatUpper =>  System.BitConverter.UInt32BitsToSingle((uint)Ulong);

    //public float FloatLower =>  System.BitConverter.UInt32BitsToSingle((uint)(Ulong >> 32));
}
