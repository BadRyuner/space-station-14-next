namespace Content.Shared._CorvaxNext.IntegratedCircuits.Events;

public sealed class OnCircuitEvent : HandledEntityEventArgs
{
    public int CurrentDepth { get; }

    public int MaxDepth { get; }

    public string EventName { get; }

    public OnCircuitEvent(string eventName, int currentDepth, int maxDepth)
    {
        EventName = eventName;
        CurrentDepth = currentDepth;
        MaxDepth = maxDepth;
    }
}
