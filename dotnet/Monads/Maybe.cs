namespace Monads;

public static class Maybe {
    public static Maybe<T> Some<T>(T value) => new(value);
}

/// <summary>
/// Represents a monad wrapping an optional value
/// </summary>
/// <typeparam name="T">Type of value</typeparam>
[System.Diagnostics.DebuggerStepThrough]
public sealed class Maybe<T> : IEquatable<T>, IEquatable<Maybe<T>> {

    private readonly T? _value;
    public bool HasValue => _value is not null;

    internal T Value => HasValue
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on an empty Maybe.");


    /// <summary>
    /// Initialize an empty maybe
    /// </summary>
    private Maybe() { }

    /// <summary>
    /// Initialize a maybe with the specified value
    /// </summary>
    /// <param name="item"></param>
    public Maybe(T? item) {
        if (item is not null) {
            _value = item;
        }
    }

    public static readonly Maybe<T> None = new();

    /// <summary>
    /// Return the value if it exists, otherwise return the default value
    /// </summary>
    /// <param name="justAction">Action to perform on the value</param>
    public void Map(Action<T> justAction) {
        ArgumentNullException.ThrowIfNull(justAction);
        if (HasValue) {
            justAction(Value!);
        }
    }

    /// <summary>
    /// Map the value to a new value if it exists
    /// </summary>
    /// <typeparam name="TResult">Type of value returned</typeparam>
    /// <param name="transform">Mapping function</param>
    public Maybe<TResult> Map<TResult>(Func<T, TResult> transform) {
        ArgumentNullException.ThrowIfNull(transform);
        return HasValue ? new Maybe<TResult>(transform(Value!)) : Maybe<TResult>.None;
    }
    /// <summary>
    /// Map the value to a new value if it exists
    /// </summary>
    /// <typeparam name="TResult">Type of value returned</typeparam>
    /// <param name="transform">Mapping function</param>
    public async Task<Maybe<TResult>> Map<TResult>(Func<T, Task<TResult>> transform) {
        ArgumentNullException.ThrowIfNull(transform);
        return HasValue ? new Maybe<TResult>(await transform(Value!).ConfigureAwait(false)) : Maybe<TResult>.None;
    }


    /// <summary>
    /// Return value (or generated value) if it exists
    /// </summary>
    /// <typeparam name="TResult">Type of value returned</typeparam>
    /// <param name="selector">Value selector</param>
    public Maybe<TResult> Just<TResult>(Func<T, TResult> selector) {
        ArgumentNullException.ThrowIfNull(selector);

        return HasValue ? selector(Value!) : Maybe<TResult>.None;
    }

    /// <summary>
    /// Return value (or generated value) if it exists
    /// </summary>
    /// <typeparam name="TResult">Type of value returned</typeparam>
    /// <param name="selector">Value selector</param>
    public Task<Maybe<TResult>> Just<TResult>(Func<T, Task<TResult>> selector) {
        ArgumentNullException.ThrowIfNull(selector);

        return JustImpl(selector);
    }

    private async Task<Maybe<TResult>> JustImpl<TResult>(Func<T, Task<TResult>> selector) =>
        HasValue ? await selector(Value!) : Maybe<TResult>.None;

    /// <summary>
    /// Return the specified selector result or the default provided
    /// </summary>
    /// <typeparam name="TResult">Type of value returned</typeparam>
    /// <param name="nothing">Default value</param>
    /// <param name="just">Result value selector</param>
    public TResult Match<TResult>(TResult nothing, Func<T, TResult> just) {
        ArgumentNullException.ThrowIfNull(nothing);
        ArgumentNullException.ThrowIfNull(just);

        return HasValue ? just(Value!) : nothing;
    }

    /// <summary>
    /// Return the specified selector result or the default provided
    /// </summary>
    /// <typeparam name="TResult">Type of value returned</typeparam>
    /// <param name="nothing">Default value</param>
    /// <param name="just">Result value selector</param>
    public void Match<TResult>(Action<T> just) {
        ArgumentNullException.ThrowIfNull(just);

        if (HasValue) {
            just(Value!);
        }
    }

    /// <summary>
    /// Return the specified selector result or the default provided
    /// </summary>
    /// <typeparam name="TResult">Type of value returned</typeparam>
    /// <param name="nothing">Default value</param>
    /// <param name="just">Result value selector</param>
    public async Task Match(Func<T, Task> just) {
        ArgumentNullException.ThrowIfNull(just);

        if (HasValue) {
            await just(Value!);
        }
    }

    /// <summary>
    /// Return the specified selector result or the default provided
    /// </summary>
    /// <typeparam name="TResult">Type of value returned</typeparam>
    /// <param name="lazyNothing">Default value factory</param>
    /// <param name="just">Result value selector</param>
    public TResult Match<TResult>(Func<TResult> lazyNothing, Func<T, TResult> just) {
        ArgumentNullException.ThrowIfNull(lazyNothing);
        ArgumentNullException.ThrowIfNull(just);

        return HasValue ? just(Value!) : lazyNothing();
    }

    /// <summary>
    /// Return the specified selector result or the default provided
    /// </summary>
    /// <typeparam name="TResult">Type of value returned</typeparam>
    /// <param name="nothing">Default value</param>
    /// <param name="just">Result value selector</param>
    public Task<TResult> Match<TResult>(TResult nothing, Func<T, Task<TResult>> just) {
        ArgumentNullException.ThrowIfNull(nothing);
        ArgumentNullException.ThrowIfNull(just);

        return HasValue ? just(Value!) : Task.FromResult(nothing);
    }

    /// <summary>
    /// Return the specified selector result or the default provided
    /// </summary>
    /// <typeparam name="TResult">Type of value returned</typeparam>
    /// <param name="lazyNothing">Default value</param>
    /// <param name="just">Result value selector</param>
    public Task<TResult> Match<TResult>(Func<TResult> lazyNothing, Func<T, Task<TResult>> just) {
        ArgumentNullException.ThrowIfNull(lazyNothing);
        ArgumentNullException.ThrowIfNull(just);

        return HasValue ? just(Value!) : Task.FromResult(lazyNothing());
    }

    /// <summary>
    /// Return the specified selector result or the default provided
    /// </summary>
    /// <typeparam name="TResult">Type of value returned</typeparam>
    /// <param name="lazyNothing">Default value</param>
    /// <param name="just">Result value selector</param>
    public Task<TResult> Match<TResult>(Func<Task<TResult>> lazyNothing, Func<T, Task<TResult>> just) {
        ArgumentNullException.ThrowIfNull(lazyNothing);
        ArgumentNullException.ThrowIfNull(just);

        return HasValue ? just(Value!) : lazyNothing();
    }

    /// <summary>
    /// Determines whether the specified object instances are considered equal.
    /// </summary>
    /// <param name="obj">Other object</param>
    public override bool Equals(object obj) =>
        obj is Maybe<T> other && Equals(Value!, other.Value);

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    public override int GetHashCode() =>
        HasValue ? Value!.GetHashCode() : default;

    public bool Equals(T? other) =>
        other is null
            && HasValue
            && EqualityComparer<T>.Default.Equals(Value, other);

    /// <summary>
    /// Determines whether the specified <see cref="Maybe{T}"/> instances are considered equal.
    /// </summary>
    /// <param name="obj">Other <see cref="Maybe{T}"/></param>
    public bool Equals(Maybe<T> other) =>
        EqualityComparer<T>.Default.Equals(Value!, other.Value) && HasValue.Equals(other.HasValue);

    public static implicit operator Maybe<T>(T val) => new(val);

    public static bool operator ==(Maybe<T> left, Maybe<T> right) =>
        !(left is null || right is null) && left?.Equals(right) == true;

    public static bool operator !=(Maybe<T> left, Maybe<T> right) =>
        !(left is null || right is null) && left?.Equals(right) == false;

    public override string ToString() =>
        HasValue ? Value!.ToString()! : "Maybe.Empty";
}

[System.Diagnostics.DebuggerNonUserCode]
public static partial class MaybeExtensions {
    /// <summary>
    /// Return the result of the selector if <paramref name="maybe"/> is non-empty
    /// and the result of <paramref name="evaluator"/> is non-empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TOption"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="maybe">Initial value</param>
    /// <param name="evaluator">Second value to evaluate using initial value</param>
    /// <param name="resultSelector">Result selector</param>
    public static Task<Maybe<TResult>> SelectMany<T, TOption, TResult>(
        this Maybe<T> maybe,
        Func<T, Task<Maybe<TOption>>> evaluator,
        Func<T, TOption, TResult> resultSelector) {
        ArgumentNullException.ThrowIfNull(maybe);
        ArgumentNullException.ThrowIfNull(evaluator);
        ArgumentNullException.ThrowIfNull(resultSelector);

        return SelectManyImpl(maybe, evaluator, resultSelector);
    }

    private static Task<Maybe<C>> SelectManyImpl<A, B, C>(Maybe<A> maybe, Func<A, Task<Maybe<B>>> selector, Func<A, B, C> resultSelector) =>
        maybe.Just(async v => (await selector(v)).Just(b => resultSelector(v, b)).Value!);

    public static Maybe<TResult> SelectMany<T, TResult>(this Maybe<T> source, Func<T, Maybe<TResult>> selector) {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        return source.Match(Maybe<TResult>.None, selector);
    }

    /// <summary>
    /// Return the result of the selector if <paramref name="maybe"/> is non-empty
    /// and the result of <paramref name="evaluator"/> is non-empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TOption"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="maybe">Initial value</param>
    /// <param name="evaluator">Second value to evaluate using initial value</param>
    /// <param name="resultSelector">Result selector</param>
    public static Maybe<TResult> SelectMany<T, TOption, TResult>(
        this Maybe<T> maybe,
        Func<T, Maybe<TOption>> selector,
        Func<T, TOption, TResult> resultSelector) {
        ArgumentNullException.ThrowIfNull(maybe);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        return maybe.Just(v => selector(v).Just(b => resultSelector(v, b)).Value!);
    }


    /// <summary>
    /// Returns the result of <paramref name="selector"/> if <paramref name="maybe"/> is not empty. <para />
    /// If <paramref name="other"/> is not empty, the second argument of <paramref name="selector"/> is the value
    /// of <paramref name="other"/>, otherwise both arguments are <paramref name="maybe"/>.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <typeparam name="TResult">Selected result</typeparam>
    /// <param name="maybe">Initial operating value.  First argument to the selector</param>
    /// <param name="other">Argument to pass to second <paramref name="selector"/> parameter if not empty</param>
    /// <param name="selector">Result selector</param>
    public static Maybe<TResult> SelectMatch<T, TResult>(this Maybe<T> maybe, Maybe<T> other, Func<T, T, TResult> selector) {
        ArgumentNullException.ThrowIfNull(maybe);
        ArgumentNullException.ThrowIfNull(other);
        ArgumentNullException.ThrowIfNull(selector);

        return maybe.SelectMany(m => other.Match(m, o => o), selector);
    }

    /// <summary>
    /// Return the set of <see cref="Maybe{T}"/> elements where the elements are not empty and
    /// the <paramref name="predicate"/> evaluates to true for the value contained in those elements.
    /// </summary>
    /// <typeparam name="T">Type contained in the <see cref="Maybe{T}"/> elements</typeparam>
    /// <param name="source">Source collection</param>
    /// <param name="predicate">Function to evaluate selection of elements</param>
    public static IEnumerable<Maybe<T>> Where<T>(this IEnumerable<Maybe<T>> source, Func<T, bool> predicate) {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        return source.Where(m => m.Match(false, v => predicate(v)));
    }

    /// <summary>
    /// Return the first non-empty element of a sequence
    /// </summary>
    /// <typeparam name="T">Type contained in the <see cref="Maybe{T}"/> elements</typeparam>
    /// <param name="source">Source sequence</param>
    public static Maybe<T> FirstOrDefault<T>(this IEnumerable<Maybe<T>> source) {
        ArgumentNullException.ThrowIfNull(source);

        var first = Enumerable.FirstOrDefault(source.WithValues());
        if (first is null) {
            return Maybe<T>.None;
        }
        return first.Value!;
    }

    /// <summary>
    /// Return the first non-empty element of a sequence
    /// </summary>
    /// <typeparam name="T">Type contained in the <see cref="Maybe{T}"/> elements</typeparam>
    /// <param name="source">Source sequence</param>
    public static Maybe<T> FirstOrDefault<T>(this IEnumerable<Maybe<T>> source, Func<T, bool> predicate) {
        ArgumentNullException.ThrowIfNull(source);

        var first = source.WithValues().FirstOrDefault(e => predicate(e.Value!));
        if (first is null) {
            return Maybe<T>.None;
        }
        return first.Value!;
    }

    /// <summary>
    /// Return the set of elements from the sequence which are non-empty
    /// </summary>
    /// <typeparam name="T">Type contained in the <see cref="Maybe{T}"/> elements</typeparam>
    /// <param name="source">Source sequence</param>
    public static IEnumerable<Maybe<T>> WithValues<T>(this IEnumerable<Maybe<T>> source) {
        ArgumentNullException.ThrowIfNull(source);

        var result = source.Where(m => m.HasValue);
        return result;
    }

    /// <summary>
    /// Return the values of all non-empty elements in the sequence
    /// </summary>
    /// <typeparam name="T">Type contained in the <see cref="Maybe{T}"/> elements</typeparam>
    /// <param name="source">Source sequence</param>
    public static IEnumerable<T> Values<T>(this IEnumerable<Maybe<T>> source) {
        ArgumentNullException.ThrowIfNull(source);

        var result = source.WithValues().Select(m => m.Value);
        return result!;
    }
}
