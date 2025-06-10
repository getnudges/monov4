using Microsoft.Extensions.Hosting;
using Nudges.Kafka.Middleware;

namespace KafkaConsumer;

internal class MessageHandlerService<TKey, TEvent>(IMessageProcessor<TKey, TEvent> messageProcessor,
                                                   IHostApplicationLifetime appLifetime) : IHostedService {

    public async Task StartAsync(CancellationToken cancellationToken) {

        try {
            await messageProcessor.ProcessMessages(cancellationToken);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            // Ignore
        }
        appLifetime.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
