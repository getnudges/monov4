using System.Reflection.Metadata.Ecma335;

namespace Monads;

public static class MaybeToResultExtensions {

    public static async Task<Maybe<TResult>> Map<T, TResult>(this Task<Maybe<T>> task, Func<T, TResult> transform) {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(transform);
        var result = await task.ConfigureAwait(false);
        return result.Map(transform);
    }

    public static async Task<Maybe<TResult>> Map<T, TResult>(this Task<Maybe<T>> task, Func<T, Task<TResult>> transform) {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(transform);
        var result = await task.ConfigureAwait(false);
        if (result.HasValue) {
            return await transform(result.Value).ConfigureAwait(false);
        }
        return Maybe<TResult>.None;
    }

    public static async Task<Result<TResult, TError>> Match<T, TResult, TError>(this Task<Maybe<Result<T, TError>>> task, Func<T, Result<TResult, TError>> transform, TError nothing) {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(transform);
        var result = await task;
        if (result.HasValue) {
            if (result.Value.Error is not null) {
                return Result.Failure<TResult, TError>(result.Value.Error);
            }
            return result.Value!.Map(transform);
        }
        return Result.Failure<TResult, TError>(nothing);
    }

    public static async Task<Result<TResult, TError>> Match<T, TResult, TError>(this Task<Maybe<T>> task, Func<T, Result<TResult, TError>> transform, TError nothing) {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(transform);
        var result = await task.ConfigureAwait(false);
        return result.Match(() => Result.Failure<TResult, TError>(nothing), transform);
    }

    public static async Task<Result<TResult, TError>> Match<T, TResult, TError>(this Task<Maybe<T>> task, Func<T, Task<Result<TResult, TError>>> transform, TError nothing) {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(transform);
        var result = await task.ConfigureAwait(false);
        if (result.HasValue) {
            return await transform(result.Value).ConfigureAwait(false);
        }
        return nothing;
    }

    public static async Task<Result<TResult, TError>> Match<T, TResult, TError>(this Task<Maybe<T>> task, Func<T, Task<Result<TResult, TError>>> transform, Func<TError> lazyNothing) {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(transform);
        var result = await task.ConfigureAwait(false);
        if (result.HasValue) {
            return await transform(result.Value).ConfigureAwait(false);
        }
        return lazyNothing();
    }


    public static async Task<Result<TResult, TError>> Match<T, TResult, TError>(this Task<Result<T, TError>> task, Func<T, Task<Result<TResult, TError>>> transform, Func<TError> lazyNothing) {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(transform);
        var result = await task.ConfigureAwait(false);
        if (result.IsSuccess) {
            return await transform(result.Value!).ConfigureAwait(false);
        }
        return lazyNothing();
    }

    public static async Task<Result<TResult, TError>> Match<T, TResult, TError>(this Task<Maybe<Result<T, TError>>> task, Func<T, Result<TResult, TError>> transform, Func<TError> lazyNothing) {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(transform);
        var result = await task.ConfigureAwait(false);
        if (result.HasValue) {
            if (result.Value.Error is not null) {
                return Result.Failure<TResult, TError>(result.Value.Error);
            }
            return result.Value!.Map(transform);
        }
        return Result.Failure<TResult, TError>(lazyNothing());
    }

    public static async Task<Result<T, TError>> Map<T, TError>(this Task<Result<T, TError>> task, Func<T, Task<Maybe<TError>>> successTransform) {
        var result = await task.ConfigureAwait(false);
        if (result.IsSuccess) {
            var transformedResult = await successTransform(result.Value!).ConfigureAwait(false);
            if (transformedResult.HasValue) {
                return transformedResult.Value!;
            }
        }
        return result.Error!;
    }

    public static async Task<Result<TResult, T>> Match<T, TResult>(this Maybe<T> value, Func<T, TResult> successTransform, Func<Task<Result<TResult, T>>> nothingTransform) {
        if (value.HasValue) {
            return successTransform(value.Value!);
        }
        return await nothingTransform().ConfigureAwait(false);
    }

    public static async Task<TResult> Match<T, TResult>(this Maybe<T> value, Func<T, TResult> successTransform, Func<Task<TResult>> failureTransform) {
        if (value.HasValue) {
            return successTransform(value.Value);
        }
        return await failureTransform();
    }

    public static async Task<TResult> Match<T, TResult>(this Maybe<T> value, Func<T, Task<TResult>> successTransform, Func<Task<TResult>> failureTransform) {
        if (value.HasValue) {
            return await successTransform(value.Value);
        }
        return await failureTransform();
    }

    public static async Task<Result<TResult, TError>> Map<T, TError, TResult>(this Task<Result<T, TError>> task, Func<T, Task<TResult>> successTransform) {
        var result = await task.ConfigureAwait(false);
        if (result.IsSuccess) {
            return await successTransform(result.Value!).ConfigureAwait(false);
        }
        return result.Error!;
    }

    public static async Task<Maybe<TError>> MapError<T, TError>(this Task<Result<T, TError>> task, Func<T, Task<Maybe<TError>>> successTransform) {
        var result = await task.ConfigureAwait(false);
        if (result.IsSuccess) {
            return await successTransform(result.Value!).ConfigureAwait(false);
        }
        return result.Error!;
    }
}
