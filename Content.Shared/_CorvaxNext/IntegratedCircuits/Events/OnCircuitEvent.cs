namespace Content.Shared._CorvaxNext.IntegratedCircuits.Events;

public sealed class OnCircuitEvent : HandledEntityEventArgs
{
    public string EventName { get; }

    public OnCircuitEvent(string eventName)
    {
        EventName = eventName;
    }
}
