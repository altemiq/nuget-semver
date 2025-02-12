// -----------------------------------------------------------------------
// <copyright file="AsyncEnumerable.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace System.Linq;

/// <summary>
/// Extensions for <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
public static class AsyncEnumerable
{
    /// <summary>
    /// Converts an enumerable sequence to an async-enumerable sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">Enumerable sequence to convert to an async-enumerable sequence.</param>
    /// <returns>The async-enumerable sequence whose elements are pulled from the given enumerable sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static IAsyncEnumerable<TSource> ToAsyncEnumerable<TSource>(this IEnumerable<TSource> source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return new AsyncEnumerableAdapter<TSource>(source);
    }

    /// <summary>
    /// Converts an async-enumerable sequence to an enumerable sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">An async-enumerable sequence to convert to an enumerable sequence.</param>
    /// <returns>The enumerable sequence containing the elements in the async-enumerable sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static IEnumerable<TSource> ToEnumerable<TSource>(this IAsyncEnumerable<TSource> source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return Core(source);

        static IEnumerable<TSource> Core(IAsyncEnumerable<TSource> source)
        {
            var e = source.GetAsyncEnumerator(default);

            try
            {
                while (WaitAsync(e.MoveNextAsync()))
                {
                    yield return e.Current;
                }
            }
            finally
            {
                Wait(e.DisposeAsync());
            }

            static void Wait(ValueTask valueTask)
            {
                if (valueTask.IsCompletedSuccessfully)
                {
                    return;
                }

                if (valueTask.IsFaulted)
                {
                    valueTask.GetAwaiter().GetResult();
                    return;
                }

                valueTask.AsTask().GetAwaiter().GetResult();
            }

            static T WaitAsync<T>(ValueTask<T> valueTask)
            {
                if (valueTask.IsCompletedSuccessfully)
                {
                    return valueTask.Result;
                }

                if (valueTask.IsFaulted)
                {
                    _ = valueTask.GetAwaiter().GetResult();
                }

                return valueTask.AsTask().Result;
            }
        }
    }

    /// <summary>
    /// Projects each element of an async-enumerable sequence to an async-enumerable sequence and merges the resulting async-enumerable sequences into one async-enumerable sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <typeparam name="TResult">The type of the elements in the projected inner sequences and the elements in the merged result sequence.</typeparam>
    /// <param name="source">An async-enumerable sequence of elements to project.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of the input sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="selector"/> is null.</exception>
    public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> selector)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        return Core(source, selector);

        static async IAsyncEnumerable<TResult> Core(IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> selector)
        {
            await foreach (var item in source.ConfigureAwait(false))
            {
                await foreach (var result in selector(item).ConfigureAwait(false))
                {
                    yield return result;
                }
            }
        }
    }

    /// <summary>
    /// Filters the elements of an async-enumerable sequence based on a predicate.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">An async-enumerable sequence whose elements to filter.</param>
    /// <param name="predicate">A function to test each source element for a condition.</param>
    /// <returns>An async-enumerable sequence that contains elements from the input sequence that satisfy the condition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is null.</exception>
    public static IAsyncEnumerable<TSource> Where<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        return Core(source, predicate);

        [Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3267:Loops should be simplified with \"LINQ\" expressions", Justification = "This is the simplification.")]
        static async IAsyncEnumerable<TSource> Core(IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            await foreach (var item in source.ConfigureAwait(false))
            {
                if (predicate(item))
                {
                    yield return item;
                }
            }
        }
    }

    /// <summary>
    /// Returns the first element of an async-enumerable sequence that satisfies the condition in the predicate, or a default value if no such element exists.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">Source async-enumerable sequence.</param>
    /// <param name="predicate">A predicate function to evaluate for elements in the source sequence.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>ValueTask containing the first element in the async-enumerable sequence that satisfies the condition in the predicate, or a default value if no such element exists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is null.</exception>
    public static ValueTask<TSource?> FirstOrDefaultAsync<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool>? predicate = default, CancellationToken cancellationToken = default)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        return Core(source, predicate, cancellationToken);

        static async ValueTask<TSource?> Core(IAsyncEnumerable<TSource> source, Func<TSource, bool>? predicate, CancellationToken cancellationToken)
        {
            await foreach (var item in source.ConfigureAwait(false).WithCancellation(cancellationToken))
            {
                if (predicate?.Invoke(item) ?? true)
                {
                    return item;
                }
            }

            return default;
        }
    }

    /// <summary>
    /// Returns the maximum value in a generic sequence according to a specified key selector function.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
    /// <param name="source">A sequence of values to determine the maximum value of.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="comparer">The <see cref="IComparer{TKey}" /> to compare keys.</param>
    /// <returns>The value with the maximum key in the sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>If <typeparamref name="TKey" /> is a reference type and the source sequence is empty or contains only values that are <see langword="null" />, this method returns <see langword="null" />.</para>
    /// </remarks>
    public static ValueTask<TSource?> MaxBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer = default)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (keySelector is null)
        {
            throw new ArgumentNullException(nameof(keySelector));
        }

        return Core(source, keySelector, comparer ?? Comparer<TKey>.Default);

        static async ValueTask<TSource?> Core(IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            await using var e = source.ConfigureAwait(false).GetAsyncEnumerator();

            if (!(await e.MoveNextAsync()))
            {
                if (default(TSource) is null)
                {
                    return default;
                }

                throw new InvalidOperationException("Sequence contains no elements");
            }

            var value = e.Current;
            var key = keySelector(value);

            if (default(TKey) is null)
            {
                if (key is null)
                {
                    var firstValue = value;

                    do
                    {
                        if (!(await e.MoveNextAsync()))
                        {
                            // All keys are null, surface the first element.
                            return firstValue;
                        }

                        value = e.Current;
                        key = keySelector(value);
                    }
                    while (key is null);
                }

                while (await e.MoveNextAsync())
                {
                    var nextValue = e.Current;
                    var nextKey = keySelector(nextValue);
                    if (nextKey is not null && comparer.Compare(nextKey, key) > 0)
                    {
                        key = nextKey;
                        value = nextValue;
                    }
                }
            }
            else
            {
                while (await e.MoveNextAsync())
                {
                    var nextValue = e.Current;
                    var nextKey = keySelector(nextValue);
                    if (comparer.Compare(nextKey, key) > 0)
                    {
                        key = nextKey;
                        value = nextValue;
                    }
                }
            }

            return value;
        }
    }

    /// <summary>
    /// Determines whether an async-enumerable sequence contains any elements.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">An async-enumerable sequence to check for non-emptiness.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>An async-enumerable sequence containing a single element determining whether the source sequence contains any elements.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
    public static ValueTask<bool> AnyAsync<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken = default)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return Core(source, predicate, cancellationToken);

        [Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3267:Loops should be simplified with \"LINQ\" expressions", Justification = "This is the simplification.")]
        static async ValueTask<bool> Core(IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken)
        {
            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (predicate(item))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Creates a list from an async-enumerable sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">The source async-enumerable sequence to get a list of elements for.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>An async-enumerable sequence containing a single element with a list containing all the elements of the source sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
    public static ValueTask<TSource[]> ToArrayAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return Core(source, cancellationToken);

        static async ValueTask<TSource[]> Core(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
        {
            var list = new List<TSource>();

            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                list.Add(item);
            }

            return [.. list];
        }
    }

    private sealed class AsyncEnumerableAdapter<T> : IAsyncEnumerable<T>
    {
        private readonly IEnumerable<T> source;

        public AsyncEnumerableAdapter(IEnumerable<T> source) => this.source = source;

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return new AsyncEnumerator(this.source.GetEnumerator());
        }

        private sealed class AsyncEnumerator : IAsyncEnumerator<T>
        {
            private IEnumerator<T>? enumerator;

            private T current = default!;

            public AsyncEnumerator(IEnumerator<T> enumerator) => this.enumerator = enumerator;

            public T Current => this.current;

            public ValueTask DisposeAsync()
            {
                if (this.enumerator is not null)
                {
                    this.enumerator.Dispose();
                    this.enumerator = null;
                }

                this.current = default!;
                return default;
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (this.enumerator!.MoveNext())
                {
                    this.current = this.enumerator.Current;
                    return true;
                }

                await this.DisposeAsync().ConfigureAwait(false);
                return false;
            }
        }
    }
}