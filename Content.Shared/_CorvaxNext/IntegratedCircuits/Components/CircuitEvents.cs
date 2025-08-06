using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.IntegratedCircuits.Components;

[NetSerializable, Serializable, DataDefinition]
public sealed partial class CircuitEvent : IEquatable<CircuitEvent>
{
    [DataField("name", readOnly: true, required: true)]
    public string Name = string.Empty;

    [DataField("eventtype", readOnly: true, required: true)]
    public EventType EventType;

    [DataField("eventlinks")]
    public List<EventLinks> EventLinks = [];

    public bool Equals(CircuitEvent? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Name == other.Name && EventType == other.EventType && EventLinks.Equals(other.EventLinks);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is CircuitEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, (int)EventType, EventLinks);
    }
}

[NetSerializable, Serializable]
public sealed class EventLinks : IEquatable<EventLinks>
{
    public NetEntity Circuit;

    public string EventName = string.Empty;

    public EventLinks(NetEntity circuit, string eventName)
    {
        Circuit = circuit;
        EventName = eventName;
    }

    public bool Equals(EventLinks? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Circuit.Equals(other.Circuit) && EventName == other.EventName;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is EventLinks other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Circuit, EventName);
    }
}

[NetSerializable, Serializable]
public enum EventType : byte
{
    PulseOut,
    PulseIn,
}
