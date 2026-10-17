// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace Tinyhand;

/// <summary>
/// Bounds the input and declared sizes processed by one reader and its copies.
/// Element counts are not heap-byte limits: element sizes and collection overhead depend on the target schema.
/// These limits do not bound allocations or side effects performed by application constructors or custom formatters.
/// </summary>
public sealed record TinyhandDeserializationLimits
{
    /// <summary>Gets unrestricted size limits. Use only when the caller enforces appropriate resource bounds.</summary>
    public static TinyhandDeserializationLimits Unrestricted { get; } = new();

    /// <summary>Gets finite defaults for trusted sources, including larger local datasets. Applications can adjust them with a record copy.</summary>
    public static TinyhandDeserializationLimits TrustedData { get; } = new()
    {
        MaximumInputBytes = 64 * 1024 * 1024,
        MaximumDecompressedBytes = 128 * 1024 * 1024,
        MaximumCollectionLength = 2_097_152,
        MaximumTotalElements = 4_194_304,
        MaximumStringBytes = 16 * 1024 * 1024,
        MaximumBinaryBytes = 64 * 1024 * 1024,
        MaximumTotalPayloadBytes = 128 * 1024 * 1024,
    };

    /// <summary>Gets finite defaults for network input. Applications can use a record copy to adjust them.</summary>
    public static TinyhandDeserializationLimits UntrustedData { get; } = new()
    {
        MaximumInputBytes = 16 * 1024 * 1024,
        MaximumDecompressedBytes = 64 * 1024 * 1024,
        MaximumCollectionLength = 65_536,
        MaximumTotalElements = 1_048_576,
        MaximumStringBytes = 16 * 1024 * 1024,
        MaximumBinaryBytes = 16 * 1024 * 1024,
        MaximumTotalPayloadBytes = 64 * 1024 * 1024,
        MaximumTotalCollectionBytes = 64 * 1024 * 1024,
    };

    /// <summary>Gets the maximum size of the original input or buffered stream, in bytes.</summary>
    public int MaximumInputBytes { get; init => field = Nonnegative(value); } = int.MaxValue;

    /// <summary>Gets the cumulative maximum LZ4 output size, in bytes, before buffers are requested.</summary>
    public int MaximumDecompressedBytes { get; init => field = Nonnegative(value); } = int.MaxValue;

    /// <summary>Gets the maximum array length or map pair count, including generated object containers.</summary>
    public int MaximumCollectionLength { get; init => field = Nonnegative(value); } = int.MaxValue;

    /// <summary>Gets the cumulative declared element count. Each map pair counts as two elements.</summary>
    public long MaximumTotalElements { get; init => field = Nonnegative(value); } = long.MaxValue;

    /// <summary>Gets the cumulative logical storage budget for collection elements, measured as count times the in-memory element size.</summary>
    /// <remarks>
    /// Standard and generated collection formatters charge this before allocation or reuse mutation.
    /// Reused storage is charged too. This excludes collection overhead, referenced objects, and application constructors;
    /// it is not a bound on total managed-heap usage. Binary payloads are covered by the separate payload limits.
    /// </remarks>
    public long MaximumTotalCollectionBytes { get; init => field = Nonnegative(value); } = long.MaxValue;

    /// <summary>Gets the maximum encoded byte length of a string or identifier, before decoding.</summary>
    public int MaximumStringBytes { get; init => field = Nonnegative(value); } = int.MaxValue;

    /// <summary>Gets the maximum byte length of a binary or extension payload.</summary>
    public int MaximumBinaryBytes { get; init => field = Nonnegative(value); } = int.MaxValue;

    /// <summary>Gets the cumulative encoded byte lengths of strings, binary values, and extensions.</summary>
    public long MaximumTotalPayloadBytes { get; init => field = Nonnegative(value); } = long.MaxValue;

    internal bool IsUnrestricted => ReferenceEquals(this, Unrestricted);

    internal void CheckInputLength(long length)
    {
        if (length < 0 || length > this.MaximumInputBytes)
        {
            throw new TinyhandException("The input exceeds the maximum deserialization input length.");
        }
    }

    private static int Nonnegative(int value) => value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));

    private static long Nonnegative(long value) => value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
}
