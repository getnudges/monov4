namespace KafkaConsumer.Services;

internal class DisposableWrapper<T>(Func<T> innerObject) : IDisposable where T : class {
    public T Instance { get; private set; } = innerObject();

    public void Dispose() => Instance = null!;
}
