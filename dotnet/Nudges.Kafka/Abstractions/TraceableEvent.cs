namespace Nudges.Kafka.Abstractions;

public abstract record class TraceableEvent(string TraceParent);
