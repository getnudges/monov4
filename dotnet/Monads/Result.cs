using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Monads;

/// <summary>
/// Convenience class for creating <see cref="Result{T, TError}"/> instances.
/// </summary>
public static class Result {
    public static Result<T, TError> Success<T, TError>(T value) => new(value);
    public static Task<Result<T, TError>> SuccessTask<T, TError>(T value) => Task.FromResult<Result<T, TError>>(new(value));
    public static Result<T, TError> Failure<T, TError>(TError error) => new(error);
    public static Result<T, TError> Success<T, TError>(T value, TError _) => new(value, default!);
    public static Result<T, TError> Failure<T, TError>(T _, TError error) => new(default!, error);

    public static Result<T, Exception> Exception<T>(Exception error) => Failure<T, Exception>(error);
    public static Result<bool, Exception> Exception(Exception error) => Failure<bool, Exception>(error);
    public static Result<bool, TError> Exception<TError>(TError error) => Failure<bool, TError>(error);
    public static Result<bool, Exception> Succeed() => Success<bool, Exception>(true);
    public static Result<string, Exception> Fail(string message) => new(message);
    public static Result<bool, string> Failure(string message) => new(message);

    public static Task<Result<T, Exception>> ExceptionTask<T>(Exception error) => Task.FromResult(Exception<T>(error));
    public static Task<Result<bool, Exception>> ExceptionTask(Exception error) => Task.FromResult(Exception(error));
    public static Task<Result<bool, Exception>> SucceedTask() => Task.FromResult(Succeed());
    public static Task<Result<string, Exception>> FailTask(string message) => Task.FromResult<Result<string, Exception>>(new(message));
    public static Task<Result<bool, string>> FailureTask(string message) => Task.FromResult(Failure(message));
}

/// <summary>
/// Represents the outcome of an operation that can either succeed with a value of type <typeparamref name="T"/>
/// or fail with an error of type <typeparamref name="TError"/>.
/// </summary>
[DebuggerStepThrough]
public class Result<T, TError> {
    /// <summary>
    /// Gets a value indicating whether the result is a success.
    /// </summary>
    public bool IsSuccess => Error is null && Value is not null;

    /// <summary>
    /// Gets the value of the result if <see cref="IsSuccess"/> is true.
    /// </summary>
    [MaybeNull]
    internal T Value { get; }

    /// <summary>
    /// Gets the error of the result if <see cref="IsSuccess"/> is false.
    /// </summary>
    [MaybeNull]
    public TError Error { get; }

    private Result() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T, TError}"/> class with a success value.
    /// </summary>
    /// <param name="value">The success value.</param>
    internal Result(T value) => Value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T, TError}"/> class with an error.
    /// </summary>
    /// <param name="error">The error value.</param>
    protected internal Result(TError error) => Error = error;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T, TError}"/> class with an error.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="error">The error value.</param>
    internal Result(T value, TError error) {
        if (value is not null && error is not null) {
            throw new InvalidOperationException("Result cannot have both a value and an error.");
        }

        Error = error;
        Value = value;
    }

    /// <summary>
    /// Creates a success result.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A success result.</returns>
    internal static Result<T, TError> Success(T value) => new(value);

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    /// <param name="error">The error value.</param>
    /// <returns>A failure result.</returns>
    internal static Result<T, TError> Failure(TError error) => new(error);

    public Result<TResult, TError> Map<TResult>(Func<T, TResult> successTransform) {
        if (IsSuccess) {
            return Result.Success<TResult, TError>(successTransform(Value!));
        }

        return Result.Failure<TResult, TError>(Error!);
    }

    // TODO: this should be named Match, but that would conflict with another existing Match method
    public async Task<TResult> Map<TResult>(Func<T, Task<TResult>> successTransform, Func<TError, TResult> failureTransform) {
        if (IsSuccess) {
            return await successTransform(Value!);
        }

        return failureTransform(Error!);
    }

    public async Task<Result<T, TError>> Map(Func<T, Task<T>> successTransform) {
        if (IsSuccess) {
            return Result.Success<T, TError>(await successTransform(Value!).ConfigureAwait(false));
        }

        return Result.Failure<T, TError>(Error!);
    }

    public async Task<TResult> Match<TResult>(Func<T, Task<TResult>> successTransform, Func<TError, TResult> failureTransform) {
        if (IsSuccess) {
            return await successTransform(Value!).ConfigureAwait(false);
        }

        return failureTransform(Error!);
    }
    public async Task<TResult> Match<TResult>(Func<T, TResult> successTransform, Func<TError, Task<TResult>> failureTransform) {
        if (IsSuccess) {
            return successTransform(Value!);
        }

        return await failureTransform(Error!).ConfigureAwait(false);
    }

    public Result<TResult, TError> Map<TResult>(Func<T, Result<TResult, TError>> successTransform) {
        if (IsSuccess) {
            return successTransform(Value!);
        }
        return Result<TResult, TError>.Failure(Error!);
    }

    public async Task<Result<TResult, TError>> Map<TResult>(Func<T, Task<Result<TResult, TError>>> successTransform) {
        if (IsSuccess) {
            var transformedResult = await successTransform(Value!);
            return transformedResult;
        }
        return Result<TResult, TError>.Failure(Error!);
    }

    /// <summary>
    /// Executes one of two functions based on the result being a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="success">The function to execute if the result is a success.</param>
    /// <param name="failure">The function to execute if the result is a failure.</param>
    /// <returns>The result of executing either the success or the failure function.</returns>
    public TResult Match<TResult>(Func<T, TResult> success, Func<TError, TResult> failure) {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return IsSuccess ? success(Value!) : failure(Error!);
    }

    /// <summary>
    /// Executes one of two functions based on the result being a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="success">The function to execute if the result is a success.</param>
    /// <param name="failure">The function to execute if the result is a failure.</param>
    /// <returns>The result of executing either the success or the failure function.</returns>
    public async Task<Result<T, TError>> Match<TResult>(Func<T, Task<Result<T, TError>>> success, Func<TError, TError> failure) {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return IsSuccess ? await success(Value!).ConfigureAwait(false) : Result.Failure<T, TError>(failure(Error!));
    }

    /// <summary>
    /// Executes one of two functions based on the result being a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="success">The function to execute if the result is a success.</param>
    /// <param name="failure">The function to execute if the result is a failure.</param>
    /// <returns>The result of executing either the success or the failure function.</returns>
    public async Task<Result<TResult, TError>> Match<TResult>(Func<T, Task<Result<TResult, TError>>> success, Func<TError, Result<TResult, TError>> failure) {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return IsSuccess ? await success(Value!).ConfigureAwait(false) : failure(Error!);
    }

    /// <summary>
    /// Executes one of two functions based on the result being a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="success">The function to execute if the result is a success.</param>
    /// <param name="failure">The function to execute if the result is a failure.</param>
    /// <returns>The result of executing either the success or the failure function.</returns>
    public async Task<Result<TResult, TError>> Match<TResult>(Func<T, Task<Result<TResult, TError>>> success, Func<TError, TError> failure) {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return IsSuccess ? await success(Value!).ConfigureAwait(false) : failure(Error!);
    }

    /// <summary>
    /// Executes one of two functions based on the result being a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="success">The function to execute if the result is a success.</param>
    /// <param name="failure">The function to execute if the result is a failure.</param>
    /// <returns>The result of executing either the success or the failure function.</returns>
    public async Task<Result<T, TError>> Match(Func<T, Task<Result<T, TError>>> success, Func<TError, TError> failure) {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return IsSuccess ? await success(Value!).ConfigureAwait(false) : failure(Error!);
    }

    /// <summary>
    /// Executes one of two functions based on the result being a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="success">The function to execute if the result is a success.</param>
    /// <param name="failure">The function to execute if the result is a failure.</param>
    /// <returns>The result of executing either the success or the failure function.</returns>
    public void Match<TResult>(Action<T> success, Action<TError> failure) {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (IsSuccess) { success(Value!); } else { failure(Error!); }
    }

    /// <summary>
    /// Executes one of two functions based on the result being a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="success">The function to execute if the result is a success.</param>
    /// <param name="failure">The function to execute if the result is a failure.</param>
    /// <returns>The result of executing either the success or the failure function.</returns>
    public void Match(Action<T> success, Action<TError> failure) {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (IsSuccess) {
            success(Value!);
        } else {
            failure(Error!);
        }
    }

    /// <summary>
    /// Executes one of two functions based on the result being a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="success">The function to execute if the result is a success.</param>
    /// <param name="failure">The function to execute if the result is a failure.</param>
    /// <returns>The result of executing either the success or the failure function.</returns>
    public async Task Match(Func<T, Task> success, Func<TError, Task> failure) {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (IsSuccess) {
            await success(Value!).ConfigureAwait(false);
        } else {
            await failure(Error!).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes one of two functions based on the result being a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="success">The function to execute if the result is a success.</param>
    /// <param name="failure">The function to execute if the result is a failure.</param>
    /// <returns>The result of executing either the success or the failure function.</returns>
    public async Task<Result<TResult, TException>> Match<TResult, TException>(Func<T, Task<TResult>> success, Func<TError, Task<TException>> failure) {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (IsSuccess) {
            return await success(Value!).ConfigureAwait(false);
        } else {
            return await failure(Error!).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes one of two functions based on the result being a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="success">The function to execute if the result is a success.</param>
    /// <param name="failure">The function to execute if the result is a failure.</param>
    /// <returns>The result of executing either the success or the failure function.</returns>
    public async Task<Result<TResult, TException>> Match<TResult, TException>(Func<T, Task<TResult>> success, Func<TError, TException> failure) {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (IsSuccess) {
            return await success(Value!).ConfigureAwait(false);
        } else {
            return failure(Error!);
        }
    }

    /// <summary>
    /// Executes one of two functions based on the result being a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="success">The function to execute if the result is a success.</param>
    /// <param name="failure">The function to execute if the result is a failure.</param>
    /// <returns>The result of executing either the success or the failure function.</returns>
    public async Task<Result<TResult, TError>> Match<TResult, TException>(Func<T, Task<TResult>> success) {
        ArgumentNullException.ThrowIfNull(success);

        if (IsSuccess) {
            return await success(Value!).ConfigureAwait(false);
        } else {
            return Error!;
        }
    }

    /// <summary>
    /// Executes one of two functions based on the result being a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="success">The function to execute if the result is a success.</param>
    /// <param name="failure">The function to execute if the result is a failure.</param>
    /// <returns>The result of executing either the success or the failure function.</returns>
    public Result<TResult, TException> Match<TResult, TException>(Func<T, TResult> success, Func<TError, TException> failure) {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (IsSuccess) {
            return success(Value!);
        } else {
            return failure(Error!);
        }
    }

    /// <summary>
    /// Executes one of two functions based on the result being a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="success">The function to execute if the result is a success.</param>
    /// <param name="failure">The function to execute if the result is a failure.</param>
    /// <returns>The result of executing either the success or the failure function.</returns>
    public async Task<Result<TResult, TException>> Match<TResult, TException>(Func<T, TResult> success, Func<TError, Task<TException>> failure) {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (IsSuccess) {
            return success(Value!);
        } else {
            return await failure(Error!).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes one of two functions based on the result being a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="success">The function to execute if the result is a success.</param>
    /// <param name="failure">The function to execute if the result is a failure.</param>
    /// <returns>The result of executing either the success or the failure function.</returns>
    public async Task Match(Func<T, Task> success, Action<TError> failure) {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (IsSuccess) {
            await success(Value!).ConfigureAwait(false);
        } else {
            failure(Error!);
        }
    }

    /// <summary>
    /// Asynchronously executes one of two functions based on the result being a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="success">The function to execute if the result is a success.</param>
    /// <param name="failure">The function to execute if the result is a failure.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the result of executing either the success or the failure function.</returns>
    public async Task<TResult> Match<TResult>(Func<T, Task<TResult>> success, Func<TError, Task<TResult>> failure) {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        // TODO: move to internal MatchAsyncImpl for exception handling
        return IsSuccess ? await success(Value!).ConfigureAwait(false) : await failure(Error!).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously executes one of two functions based on the result being a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="success">The function to execute if the result is a success.</param>
    /// <param name="failure">The function to execute if the result is a failure.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the result of executing either the success or the failure function.</returns>
    public async Task Match<TResult>(Func<T, Task<TResult>> success, Action<TError> failure) {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (IsSuccess) {
            await success(Value!).ConfigureAwait(false);
        } else {
            failure(Error!);
        }
    }

    /// <summary>
    /// Asynchronously executes one of two functions based on the result being a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="success">The function to execute if the result is a success.</param>
    /// <param name="failure">The function to execute if the result is a failure.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the result of executing either the success or the failure function.</returns>
    public async Task Match<TResult>(Func<T, Task> success, Action<TError> failure) {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (IsSuccess) {
            await success(Value!).ConfigureAwait(false);
        } else {
            failure(Error!);
        }
    }

    /// <summary>
    /// Asynchronously executes one of two functions based on the result being a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="success">The function to execute if the result is a success.</param>
    /// <param name="failure">The function to execute if the result is a failure.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the result of executing either the success or the failure function.</returns>
    public async Task Match<TResult>(Func<Task<T>> success, Func<Task<TError>> failure) {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        // TODO: move to internal MatchAsyncImpl for exception handling
        if (IsSuccess) {
            await success().ConfigureAwait(false);
        } else {
            await failure().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
    public override bool Equals(object? obj) {
        if (obj is Result<T, TError> other) {
            return IsSuccess == other.IsSuccess &&
                   (IsSuccess
                    ? EqualityComparer<T>.Default.Equals(Value, other.Value)
                    : EqualityComparer<TError>.Default.Equals(Error, other.Error));
        }
        return false;
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode() => IsSuccess ? Value!.GetHashCode() : Error!.GetHashCode();

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => IsSuccess ? $"Success: {Value}" : $"Failure: {Error}";

    /// <summary>
    /// Implicitly converts a value to a success result.
    /// </summary>
    /// <param name="value">The success value.</param>
    public static implicit operator Result<T, TError>(T value) => Success(value);

    /// <summary>
    /// Implicitly converts an error to a failure result.
    /// </summary>
    /// <param name="error">The error value.</param>
    public static implicit operator Result<T, TError>(TError error) => Failure(error);
}
public static class ResultTaskExtensions {

    public static async Task<Result<TResult, TError>> Map<T, TResult, TError>(this Task<Result<T, TError>> task, Func<T, Task<Result<TResult, TError>>> successTransform) {
        var result = await task.ConfigureAwait(false);
        if (result.IsSuccess) {
            var transformedResult = await successTransform(result.Value!).ConfigureAwait(false);
            return transformedResult;
        }
        return Result<TResult, TError>.Failure(result.Error!);
    }

    public static async Task<Result<TResult, TError>> Map<T, TResult, TError>(this Task<Result<T, TError>> task, Func<T, Result<TResult, TError>> successTransform) {
        var result = await task.ConfigureAwait(false);
        if (result.IsSuccess) {
            var transformedResult = successTransform(result.Value!);
            return transformedResult;
        }
        return Result<TResult, TError>.Failure(result.Error!);
    }

    public static async Task Match<T, TError>(this Task<Result<T, TError>> task, Action<T> success, Action<TError> failure) {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        var result = await task.ConfigureAwait(false);
        if (result.IsSuccess) {
            success(result.Value!);
        } else {
            failure(result.Error!);
        }
    }

    public static async Task<Result<Maybe<TResult>, TNewError>> Match<T, TResult, TError, TNewError>(
        this Task<Result<Maybe<T>, TError>> task, Func<Maybe<T>, Task<Maybe<TResult>>> success, Func<TError, Task<TNewError>> failure) {

        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        var result = await task.ConfigureAwait(false);
        if (result.IsSuccess) {
            return await success(result.Value!).ConfigureAwait(false);
        } else {
            return await failure(result.Error!).ConfigureAwait(false);
        }
    }

    public static async Task<Result<TResult, TException>> Match<T, TResult, TError, TException>(
        this Task<Result<T, TError>> task, Func<T, Task<TResult>> success, Func<TError, TException> failure) {

        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        var result = await task.ConfigureAwait(false);
        if (result.IsSuccess) {
            return await success(result.Value!).ConfigureAwait(false);
        } else {
            return failure(result.Error!);
        }
    }

    public static async Task<Result<TResult, TException>> Match<T, TResult, TError, TException>(
        this Task<Result<T, TError>> task, Func<T, TResult> success, Func<TError, TException> failure) {

        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        var result = await task.ConfigureAwait(false);
        if (result.IsSuccess) {
            return success(result.Value!);
        } else {
            return failure(result.Error!);
        }
    }

    public static async Task<TResult> Match<T, TResult, TError>(
        this Task<Result<T, TError>> task, Func<T, Task<TResult>> success, Func<TError, TResult> failure) {

        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        var result = await task.ConfigureAwait(false);
        if (result.IsSuccess) {
            return await success(result.Value!).ConfigureAwait(false);
        } else {
            return failure(result.Error!);
        }
    }

    public static async Task<Result<Maybe<TResult>, TException>> Match<T, TResult, TError, TException>(
        this Task<Result<Maybe<T>, TError>> task, Func<Maybe<T>, Task<Maybe<TResult>>> success, Func<TError, TException> failure) {

        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        var result = await task.ConfigureAwait(false);
        if (result.IsSuccess) {
            return await success(result.Value!).ConfigureAwait(false);
        } else {
            return failure(result.Error!);
        }
    }
}
