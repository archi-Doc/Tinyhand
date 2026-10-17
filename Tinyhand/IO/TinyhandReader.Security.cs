// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Tinyhand.IO;

public ref partial struct TinyhandReader
{
    /// <summary>
    /// Attaches size limits before reading. A restricted reader and all its copies share one cumulative budget;
    /// subsequent calls never reset that budget. Custom formatters should call this before reading or allocating.
    /// </summary>
    /// <param name="security">The security policy for this deserialization.</param>
    /// <remarks>
    /// Reapplying the same limits or an unrestricted policy preserves the existing limits and consumed budget.
    /// Different finite limits cannot be applied to an active restricted reader; create a new reader for a new operation.
    /// </remarks>
    /// <exception cref="TinyhandException">The input exceeds its limit, or different finite limits are already attached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplySecurity(TinyhandSecurity security)
    {
        this.validateStringValues |= security.ValidateStringValues;
        if (security.Limits.IsUnrestricted)
        {
            return;
        }

        if (this.budget is null)
        {
            this.ApplySecuritySlow(security);
        }
        else if (!ReferenceEquals(this.budget.Limits, security.Limits))
        {
            this.budget.CheckCompatibleLimits(security.Limits);
        }
    }

    /// <summary>Charges the logical storage for a collection before allocating or modifying it.</summary>
    /// <typeparam name="T">The stored element type. For a map, use its key/value pair type.</typeparam>
    /// <param name="count">The declared element or pair count.</param>
    /// <remarks>Copies and sub-readers share this cumulative budget. The charge includes reused storage, but not collection overhead or referenced objects.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReserveCollectionStorage<T>(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        this.budget?.ReserveCollectionStorage((long)count * Unsafe.SizeOf<T>());
    }

    // Only internal, non-allocating lookahead may avoid cumulative charges. Public Fork and reader copies cannot.
    internal TinyhandReader ForkForValidation()
    {
        var reader = this;
        reader.budget = this.budget?.ForkForValidation();
        return reader;
    }

    // Commit successful lookahead without re-reading headers and without replacing the budget shared by public reader copies.
    internal TinyhandReader CompleteValidation()
    {
        if (this.budget?.Parent is { } parent)
        {
            parent.Commit(this.budget);
            this.budget = parent;
        }

        return this;
    }

    internal void ReserveDecompressedBytes(long length) => this.budget?.ReserveDecompressedBytes(length);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ApplySecuritySlow(TinyhandSecurity security)
    {
        if (!security.Limits.IsUnrestricted)
        {
            security.Limits.CheckInputLength(this.remaining);
            this.budget = new DeserializationBudget(security.Limits);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReserveCollection(int count, bool map) => this.budget?.ReserveCollection(count, map);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReservePayload(long length, bool text) => this.budget?.ReservePayload(length, text);

    private sealed class DeserializationBudget
    {
        private readonly TinyhandDeserializationLimits limits;
        private long remainingElements;
        private long remainingPayloadBytes;
        private long remainingDecompressedBytes;
        private long remainingCollectionBytes;

        internal DeserializationBudget(TinyhandDeserializationLimits limits)
        {
            this.limits = limits;
            this.remainingElements = limits.MaximumTotalElements;
            this.remainingPayloadBytes = limits.MaximumTotalPayloadBytes;
            this.remainingDecompressedBytes = limits.MaximumDecompressedBytes;
            this.remainingCollectionBytes = limits.MaximumTotalCollectionBytes;
        }

        internal DeserializationBudget? Parent { get; private init; }

        internal TinyhandDeserializationLimits Limits => this.limits;

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void CheckCompatibleLimits(TinyhandDeserializationLimits limits)
        {
            if (limits != TinyhandDeserializationLimits.Unrestricted && limits != this.limits)
            {
                throw new TinyhandException("Different deserialization limits cannot be applied to an active reader.");
            }
        }

        internal DeserializationBudget ForkForValidation() => new(this.limits)
        {
            Parent = this,
            remainingElements = this.remainingElements,
            remainingPayloadBytes = this.remainingPayloadBytes,
            remainingDecompressedBytes = this.remainingDecompressedBytes,
            remainingCollectionBytes = this.remainingCollectionBytes,
        };

        internal void Commit(DeserializationBudget validated)
        {
            this.remainingElements = Math.Min(this.remainingElements, validated.remainingElements);
            this.remainingPayloadBytes = Math.Min(this.remainingPayloadBytes, validated.remainingPayloadBytes);
            this.remainingDecompressedBytes = Math.Min(this.remainingDecompressedBytes, validated.remainingDecompressedBytes);
            this.remainingCollectionBytes = Math.Min(this.remainingCollectionBytes, validated.remainingCollectionBytes);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void ReserveCollectionStorage(long length)
        {
            if (length > this.remainingCollectionBytes)
            {
                throw new TinyhandException("The collection exceeds the deserialization storage limit.");
            }

            this.remainingCollectionBytes -= length;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void ReserveCollection(int count, bool map)
        {
            var elements = map ? 2L * count : count;
            if (count < 0 || count > this.limits.MaximumCollectionLength || elements > this.remainingElements)
            {
                throw new TinyhandException("The collection exceeds the deserialization element limit.");
            }

            this.remainingElements -= elements;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void ReservePayload(long length, bool text)
        {
            if (length < 0 || length > (text ? this.limits.MaximumStringBytes : this.limits.MaximumBinaryBytes) || length > this.remainingPayloadBytes)
            {
                throw new TinyhandException("The value exceeds the deserialization payload limit.");
            }

            this.remainingPayloadBytes -= length;
        }

        internal void ReserveDecompressedBytes(long length)
        {
            if (length < 0 || length > this.remainingDecompressedBytes)
            {
                throw new TinyhandException("The compressed data exceeds the deserialization output limit.");
            }

            this.remainingDecompressedBytes -= length;
        }
    }
}
