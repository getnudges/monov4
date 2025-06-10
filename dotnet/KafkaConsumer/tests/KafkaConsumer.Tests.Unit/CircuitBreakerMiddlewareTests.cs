using Confluent.Kafka;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Nudges.Kafka.Middleware;

namespace Nudges.Kafka.Tests.Unit;

public class CircuitBreakerMiddlewareTests {
    private readonly FakeTimeProvider _fakeTimeProvider = new();
    private readonly CircuitBreakerMiddleware<string, string> _middleware;
    private readonly Mock<MessageHandler<string, string>> _nextMock;
    private readonly MessageContext<string, string> _context;

    public CircuitBreakerMiddlewareTests() {
        _middleware = new CircuitBreakerMiddleware<string, string>(_fakeTimeProvider);
        _nextMock = new Mock<MessageHandler<string, string>>();
        _context = new MessageContext<string, string>(Mock.Of<ConsumeResult<string, string>>(), CancellationToken.None);
    }

    [Fact]
    public async Task Circuit_RemainsClosed_ProcessesMessageSuccessfully() {
        _nextMock.Setup(next => next(It.IsAny<MessageContext<string, string>>()))
                 .ReturnsAsync(_context);

        var result = await _middleware.InvokeAsync(_context, _nextMock.Object);

        Assert.Equal(_context, result);
        _nextMock.Verify(next => next(It.IsAny<MessageContext<string, string>>()), Times.Once);
    }

    [Fact]
    public async Task Circuit_OpensAfterFailureThreshold() {

        _nextMock.Setup(next => next(It.IsAny<MessageContext<string, string>>()))
                 .ThrowsAsync(new Exception());

        for (var i = 0; i < 5; i++) {
            await Assert.ThrowsAsync<Exception>(() => _middleware.InvokeAsync(_context, _nextMock.Object));
        }

        var result = await _middleware.InvokeAsync(_context, _nextMock.Object);

        Assert.True(result.Failed);
    }

    [Fact(Skip = "ugh")]
    public async Task Circuit_TransitionsToHalfOpenAfterOpenInterval() {

        _nextMock.Setup(next => next(It.IsAny<MessageContext<string, string>>()))
                 .ThrowsAsync(new Exception());

        for (var i = 0; i < 5; i++) {
            await Assert.ThrowsAsync<Exception>(() => _middleware.InvokeAsync(_context, _nextMock.Object));
        }

        _fakeTimeProvider.Advance(TimeSpan.FromSeconds(31));

        var result = await _middleware.InvokeAsync(_context, _nextMock.Object);

        Assert.True(result.Failed);
    }

    [Fact(Skip = "ugh")]
    public async Task Circuit_ClosesAfterSuccessfulMessageInHalfOpenState() {
        _nextMock.Setup(next => next(It.IsAny<MessageContext<string, string>>()))
                 .ThrowsAsync(new Exception());

        for (var i = 0; i < 5; i++) {
            await Assert.ThrowsAsync<Exception>(() => _middleware.InvokeAsync(_context, _nextMock.Object));
        }

        _fakeTimeProvider.Advance(TimeSpan.FromSeconds(31));

        _nextMock.Setup(next => next(It.IsAny<MessageContext<string, string>>()))
                 .ReturnsAsync(_context);

        var result = await _middleware.InvokeAsync(_context, _nextMock.Object);

        Assert.Equal(_context, result);
        _nextMock.Verify(next => next(It.IsAny<MessageContext<string, string>>()), Times.Once);
    }

    [Fact]
    public async Task Circuit_RemainsOpenIfOpenIntervalNotPassed() {

        _nextMock.Setup(next => next(It.IsAny<MessageContext<string, string>>()))
                 .ThrowsAsync(new Exception());

        for (var i = 0; i < 5; i++) {
            await Assert.ThrowsAsync<Exception>(() => _middleware.InvokeAsync(_context, _nextMock.Object));
        }

        _fakeTimeProvider.Advance(TimeSpan.FromSeconds(29));

        var result = await _middleware.InvokeAsync(_context, _nextMock.Object);

        Assert.True(result.Failed);
    }
}
