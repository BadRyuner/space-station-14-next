using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.IntegratedCircuits.Components;

[NetSerializable, Serializable, DataDefinition]
public sealed partial class CircuitWire : IEquatable<CircuitWire>
{
    [DataField("name", readOnly: true, required: true)]
    public string Name = string.Empty;

    [DataField("wireaccess", readOnly: true, required: true)]
    public WireAccess WireAccess = WireAccess.In;

    [DataField("wiretype", readOnly: true, required: true)]
    public WireType WireType = WireType.Any;

    [DataField("wiredata")]
    public object? WireData;

    [DataField("writelinks")]
    public List<WireWriteLink> WriteLinks = [];

    public bool Equals(CircuitWire? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Name == other.Name && WireAccess == other.WireAccess && WireType == other.WireType && Equals(WireData, other.WireData) && WriteLinks.Equals(other.WriteLinks);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is CircuitWire other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, (int)WireAccess, (int)WireType, WireData, WriteLinks);
    }
}

[NetSerializable, Serializable]
public sealed class WireWriteLink : IEquatable<WireWriteLink>
{
    public readonly NetEntity Circuit;

    public readonly string WireName;

    public WireWriteLink(NetEntity circuit, string wireName)
    {
        Circuit = circuit;
        WireName = wireName;
    }

    public bool Equals(WireWriteLink? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Circuit.Equals(other.Circuit) && WireName == other.WireName;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is WireWriteLink other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Circuit, WireName);
    }
}

[NetSerializable, Serializable]
public enum WireAccess : byte
{
    In,
    Out,
}

[NetSerializable, Serializable]
public enum WireType : byte
{
    Any = 0,
    Integer = 1,
    Float = 2,
    String = 3,
    Reference = 4,
}
