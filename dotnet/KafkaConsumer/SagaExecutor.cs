using Monads;

namespace KafkaConsumer;
public class SagaStep {
    public Func<Task<Result<bool, Exception>>> Action { get; init; }
    public Func<Task> Rollback { get; init; }
}

public static class SagaExtensions {
    public static async Task<Result<bool, Exception>> ExecuteSagaAsync(this IEnumerable<SagaStep> steps) {
        var executedSteps = new Stack<SagaStep>();

        foreach (var step in steps) {
            var result = await step.Action();
            if (result.IsSuccess) {
                // Roll back all executed steps
                while (executedSteps.Count > 0) {
                    await executedSteps.Pop().Rollback();
                }
                return result; // Return failure
            }
            executedSteps.Push(step);
        }

        return Result.Success<bool, Exception>(true);
    }
}

