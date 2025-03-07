using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Ardalis.GuardClauses;
using Extensions.Gaurd;

namespace EpiSystem.Extensions.Collection.Generic;

using System.Linq;

public static class EnumerableExtensions {
    public static double Median(this IEnumerable<int> source) {
        Guard.Against.Null(source, nameof(source));
        Guard.Against.EmptyEnumerable(source, nameof(source),message:"No elements in the source collection.");
        var sortedArray=source.OrderBy(number => number).ToArray();
        int itemIndex=sortedArray.Length/2;
        if (sortedArray.Length % 2 == 0) {
            //Even
            return (sortedArray[itemIndex] + sortedArray[itemIndex - 1]) / 2.0;
        } else {
            // Odd 
            return sortedArray[itemIndex];
        }
    }

    public static double Median(this IEnumerable<long> source) => Median<long, long, double>(source);

    public static float Median(this IEnumerable<float> source) => (float)Median<float, double, double>(source);

    public static double Median(this IEnumerable<double> source) => Median<double, double, double>(source);

    public static decimal Median(this IEnumerable<decimal> source) => Median<decimal, decimal, decimal>(source);
    
    private static TResult Median<TSource, TAccumulator, TResult>(this IEnumerable<TSource> source)
        where TSource :  struct,INumber<TSource>
        where TAccumulator :  struct,INumber<TAccumulator>
        where TResult :  struct,INumber<TResult> {
        Guard.Against.Null(source, nameof(source));
        Guard.Against.EmptyEnumerable(source, nameof(source));
        if (source.OrderBy(number => number).TryGetSpan(out var sortedList)) {
            int itemIndex = sortedList.Length / 2;
    
            if (sortedList.Length % 2 == 0) {
                //Even
                return TResult.CreateChecked((TAccumulator.CreateChecked(sortedList[itemIndex]+sortedList[itemIndex-1])) / TAccumulator.CreateChecked(2));
            } else {
                // Odd 
                return TResult.CreateChecked(sortedList[itemIndex]);
            }
        } else {
            var sortedArray=source.OrderBy(number => number).ToArray();
            int itemIndex = sortedArray.Length / 2;
            Console.WriteLine($"ItemIndex: {itemIndex}");
            if (sortedList.Length % 2 == 0) {
                //Even
                return TResult.CreateChecked((TAccumulator.CreateChecked(sortedArray[itemIndex]+sortedArray[itemIndex-1])) / TAccumulator.CreateChecked(2));
            } else {
                // Odd 
                return TResult.CreateChecked(sortedArray[itemIndex]);
            }
        }
    }

    public static double? Median(this IEnumerable<int?> source) => Median<int, long, double>(source);

    public static double? Median(this IEnumerable<long?> source) => Median<long, long, double>(source);

    public static float? Median(this IEnumerable<float?> source) => Median<float, double, double>(source) is double result ? (float)result : null;

    public static double? Median(this IEnumerable<double?> source) => Median<double, double, double>(source);

    public static decimal? Median(this IEnumerable<decimal?> source) => Median<decimal, decimal, decimal>(source);
    
    private static TResult? Median<TSource, TAccumulator, TResult>(this IEnumerable<TSource?> source)
        where TSource :  struct,INumber<TSource>
        where TAccumulator :  struct,INumber<TAccumulator>
        where TResult :  struct,INumber<TResult> {
        Guard.Against.Null(source, nameof(source));
        Guard.Against.EmptyEnumerable(source, nameof(source));
        if (source.OrderBy(number => number).TryGetSpan(out var sortedList)) {
            int itemIndex = sortedList.Length / 2;
    
            if (sortedList.Length % 2 == 0) {
                //Even
                if (sortedList[itemIndex].HasValue && sortedList[itemIndex - 1].HasValue) {
                    var in1=sortedList[itemIndex].GetValueOrDefault();
                    var in2=sortedList[itemIndex-1].GetValueOrDefault();
                    return TResult.CreateChecked((TAccumulator.CreateChecked(in1)+TAccumulator.CreateChecked(in2)) / TAccumulator.CreateChecked(2));
                }
                return null;
            } else {
                // Odd 
                if (sortedList[itemIndex].HasValue) {
                    return TResult.CreateChecked(sortedList[itemIndex].GetValueOrDefault());
                }

                return null;
            }
        } else {
            var sortedArray=source.OrderBy(number => number).ToArray();
            int itemIndex = sortedArray.Length / 2;
            if (sortedArray.Length % 2 == 0) {
                //Even
                if (sortedArray[itemIndex].HasValue && sortedArray[itemIndex - 1].HasValue) {
                    var in1=sortedArray[itemIndex].GetValueOrDefault();
                    var in2=sortedArray[itemIndex-1].GetValueOrDefault();
                    return TResult.CreateChecked((TAccumulator.CreateChecked(in1)+TAccumulator.CreateChecked(in2)) / TAccumulator.CreateChecked(2));
                }
                return null;
            } else {
                // Odd 
                return TResult.CreateChecked(sortedArray[itemIndex].GetValueOrDefault());
            }
        }
    }
    
    public static double Median<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector) =>
        Median<TSource, int, long, double>(source, selector);

    public static double Median<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector) =>
        Median<TSource, long, long, double>(source, selector);

    public static float Median<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector) =>
        (float)Median<TSource, float, double, double>(source, selector);

    public static double Median<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector) =>
        Median<TSource, double, double, double>(source, selector);

    public static decimal Median<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector) =>
        Median<TSource, decimal, decimal, decimal>(source, selector);

    private static TResult Median<TSource, TSelector, TAccumulator, TResult>(this IEnumerable<TSource> source,
        Func<TSource, TSelector> selector)
        where TSelector : struct, INumber<TSelector>
        where TAccumulator : struct, INumber<TAccumulator>
        where TResult : struct, INumber<TResult> {
        Guard.Against.Null(source, nameof(source));
        Guard.Against.Null(selector, nameof(selector));
        
        using IEnumerator<TSource> e = source.GetEnumerator();
        if (source.Select(selector).OrderBy(number => number).TryGetSpan(out var sortedList)) {
            int itemIndex = sortedList.Length / 2;
    
            if (sortedList.Length % 2 == 0) {
                //Even
                return TResult.CreateChecked((TAccumulator.CreateChecked(sortedList[itemIndex]+sortedList[itemIndex-1])) / TAccumulator.CreateChecked(2));
            } else {
                // Odd 
                return TResult.CreateChecked(sortedList[itemIndex]);
            }
        } else {
            var sortedArray=source.Select(selector).OrderBy(number => number).ToArray();
            int itemIndex = sortedArray.Length / 2;
    
            if (sortedArray.Length % 2 == 0) {
                //Even
                return TResult.CreateChecked((TAccumulator.CreateChecked(sortedArray[itemIndex]+sortedArray[itemIndex-1])) / TAccumulator.CreateChecked(2));
            } else {
                // Odd 
                return TResult.CreateChecked(sortedArray[itemIndex]);
            }
        }
    }

    
    public static double? Median<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector) =>
        Median<TSource, int, long, double>(source, selector);

    public static double? Median<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector) =>
        Median<TSource, long, long, double>(source, selector);

    public static float? Median<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector) =>
        Median<TSource, float, double, double>(source, selector) is double result ? (float)result : null;

    public static double? Median<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector) =>
        Median<TSource, double, double, double>(source, selector);

    public static decimal? Median<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector) =>
        Median<TSource, decimal, decimal, decimal>(source, selector);

    private static TResult? Median<TSource, TSelector, TAccumulator, TResult>(this IEnumerable<TSource> source,
        Func<TSource, TSelector?> selector)
        where TSelector : struct, INumber<TSelector>
        where TAccumulator : struct, INumber<TAccumulator>
        where TResult : struct, INumber<TResult> {
        Guard.Against.Null(source, nameof(source));
        Guard.Against.Null(selector, nameof(selector));

        if (source.Select(selector).OrderBy(number => number).TryGetSpan(out var sortedList)) {
            int itemIndex = sortedList.Length / 2;
    
            if (sortedList.Length % 2 == 0) {
                //Even
                if (sortedList[itemIndex].HasValue && sortedList[itemIndex - 1].HasValue) {
                    var in1=sortedList[itemIndex].GetValueOrDefault();
                    var in2=sortedList[itemIndex-1].GetValueOrDefault();
                    return TResult.CreateChecked((TAccumulator.CreateChecked(in1)+TAccumulator.CreateChecked(in2)) / TAccumulator.CreateChecked(2));
                }
                return null;
            } else {
                // Odd 
                if (sortedList[itemIndex].HasValue) {
                    return TResult.CreateChecked(sortedList[itemIndex].GetValueOrDefault());
                }

                return null;
            }
        } else {
            var sortedArray=source.Select(selector).OrderBy(number => number).ToArray();
            int itemIndex = sortedArray.Length / 2;
            if (sortedArray.Length % 2 == 0) {
                //Even
                if (sortedArray[itemIndex].HasValue && sortedArray[itemIndex - 1].HasValue) {
                    var in1=sortedArray[itemIndex].GetValueOrDefault();
                    var in2=sortedArray[itemIndex-1].GetValueOrDefault();
                    return TResult.CreateChecked((TAccumulator.CreateChecked(in1)+TAccumulator.CreateChecked(in2)) / TAccumulator.CreateChecked(2));
                }
                return null;
            } else {
                // Odd 
                return TResult.CreateChecked(sortedArray[itemIndex].GetValueOrDefault());
            }
        }

        return null;
    }

    internal static bool TryGetSpan<TSource>(this IEnumerable<TSource> source, out ReadOnlySpan<TSource> span) {
        // Use `GetType() == typeof(...)` rather than `is` to avoid cast helpers.  This is measurably cheaper
        // but does mean we could end up missing some rare cases where we could get a span but don't (e.g. a uint[]
        // masquerading as an int[]).  That's an acceptable tradeoff.  The Unsafe usage is only after we've
        // validated the exact type; this could be changed to a cast in the future if the JIT starts to recognize it.
        // We only pay the comparison/branching costs here for super common types we expect to be used frequently
        // with LINQ methods.

        bool result = true;
        if (source.GetType() == typeof(TSource[])) {
            span = Unsafe.As<TSource[]>(source);
        } else if (source.GetType() == typeof(List<TSource>)) {
            span = CollectionsMarshal.AsSpan(Unsafe.As<List<TSource>>(source));
        } else {
            span = default;
            result = false;
        }

        return result;
    }

    private static TResult Sum<T, TResult>(ReadOnlySpan<T> span)
        where T : struct, INumber<T>
        where TResult : struct, INumber<TResult> {
        if (typeof(T) == typeof(TResult)
            && Vector<T>.IsSupported
            && Vector.IsHardwareAccelerated
            && Vector<T>.Count > 2
            && span.Length >= Vector<T>.Count * 4) {
            // For cases where the vector may only contain two elements vectorization doesn't add any benefit
            // due to the expense of overflow checking. This means that architectures where Vector<T> is 128 bit,
            // such as ARM or Intel without AVX, will only vectorize spans of ints and not longs.

            if (typeof(T) == typeof(long)) {
                return (TResult)(object)SumSignedIntegersVectorized(
                    Unsafe.BitCast<ReadOnlySpan<T>, ReadOnlySpan<long>>(span));
            }

            if (typeof(T) == typeof(int)) {
                return (TResult)(object)SumSignedIntegersVectorized(
                    Unsafe.BitCast<ReadOnlySpan<T>, ReadOnlySpan<int>>(span));
            }
        }

        TResult sum = TResult.Zero;
        foreach (T value in span) {
            checked {
                sum += TResult.CreateChecked(value);
            }
        }

        return sum;
    }

    private static T SumSignedIntegersVectorized<T>(ReadOnlySpan<T> span)
        where T : struct, IBinaryInteger<T>, ISignedNumber<T>, IMinMaxValue<T> {
        Debug.Assert(span.Length >= Vector<T>.Count * 4);
        Debug.Assert(Vector<T>.Count > 2);
        Debug.Assert(Vector.IsHardwareAccelerated);

        ref T ptr = ref MemoryMarshal.GetReference(span);
        nuint length = (nuint)span.Length;

        // Overflow testing for vectors is based on setting the sign bit of the overflowTracking
        // vector for an element if the following are all true:
        //   - The two elements being summed have the same sign bit. If one element is positive
        //     and the other is negative then an overflow is not possible.
        //   - The sign bit of the sum is not the same as the sign bit of the previous accumulator.
        //     This indicates that the new sum wrapped around to the opposite sign.
        //
        // This is done by:
        //   overflowTracking |= (result ^ input1) & (result ^ input2);
        //
        // The general premise here is that we're doing signof(result) ^ signof(input1). This will produce
        // a sign-bit of 1 if they differ and 0 if they are the same. We do the same with
        // signof(result) ^ signof(input2), then combine both results together with a logical &.
        //
        // Thus, if we had a sign swap compared to both inputs, then signof(input1) == signof(input2) and
        // we must have overflowed.
        //
        // By bitwise or-ing the overflowTracking vector for each step we can save cycles by testing
        // the sign bits less often. If any iteration has the sign bit set in any element it indicates
        // there was an overflow.
        //
        // Note: The overflow checking in this algorithm is only correct for signed integers.
        // If support is ever added for unsigned integers then the overflow check should be:
        //   overflowTracking |= (input1 & input2) | Vector.AndNot(input1 | input2, result);

        Vector<T> accumulator = Vector<T>.Zero;

        // Build a test vector with only the sign bit set in each element.
        Vector<T> overflowTestVector = new(T.MinValue);

        // Unroll the loop to sum 4 vectors per iteration. This reduces range check
        // and overflow check frequency, allows us to eliminate move operations swapping
        // accumulators, and may have pipelining benefits.
        nuint index = 0;
        nuint limit = length - (nuint)Vector<T>.Count * 4;
        do {
            // Switch accumulators with each step to avoid an additional move operation
            Vector<T> data = Vector.LoadUnsafe(ref ptr, index);
            Vector<T> accumulator2 = accumulator + data;
            Vector<T> overflowTracking = (accumulator2 ^ accumulator) & (accumulator2 ^ data);

            data = Vector.LoadUnsafe(ref ptr, index + (nuint)Vector<T>.Count);
            accumulator = accumulator2 + data;
            overflowTracking |= (accumulator ^ accumulator2) & (accumulator ^ data);

            data = Vector.LoadUnsafe(ref ptr, index + (nuint)Vector<T>.Count * 2);
            accumulator2 = accumulator + data;
            overflowTracking |= (accumulator2 ^ accumulator) & (accumulator2 ^ data);

            data = Vector.LoadUnsafe(ref ptr, index + (nuint)Vector<T>.Count * 3);
            accumulator = accumulator2 + data;
            overflowTracking |= (accumulator ^ accumulator2) & (accumulator ^ data);

            if ((overflowTracking & overflowTestVector) != Vector<T>.Zero) {
                //ThrowHelper.ThrowOverflowException();
            }

            index += (nuint)Vector<T>.Count * 4;
        } while (index < limit);

        // Process remaining vectors, if any, without unrolling
        limit = length - (nuint)Vector<T>.Count;
        if (index < limit) {
            Vector<T> overflowTracking = Vector<T>.Zero;

            do {
                Vector<T> data = Vector.LoadUnsafe(ref ptr, index);
                Vector<T> accumulator2 = accumulator + data;
                overflowTracking |= (accumulator2 ^ accumulator) & (accumulator2 ^ data);
                accumulator = accumulator2;

                index += (nuint)Vector<T>.Count;
            } while (index < limit);

            if ((overflowTracking & overflowTestVector) != Vector<T>.Zero) {
                //ThrowHelper.ThrowOverflowException();
            }
        }

        // Add the elements in the vector horizontally.
        // Vector.Sum doesn't perform overflow checking, instead add elements individually.
        T result = T.Zero;
        for (int i = 0; i < Vector<T>.Count; i++) {
            checked {
                result += accumulator[i];
            }
        }

        // Add any remaining elements
        while (index < length) {
            checked {
                result += Unsafe.Add(ref ptr, index);
            }

            index++;
        }

        return result;
    }
    
    
}