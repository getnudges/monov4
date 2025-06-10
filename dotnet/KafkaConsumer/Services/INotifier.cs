namespace KafkaConsumer.Services;

internal interface INotifier {
    public Task Notify(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
