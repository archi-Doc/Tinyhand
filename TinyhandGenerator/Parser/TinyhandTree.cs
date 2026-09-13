// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1602 // Enumeration items should be documented

namespace Tinyhand.Tree;

internal enum ElementType
{
    /// <summary>
    /// None.
    /// </summary>
    None,

    /// <summary>
    /// Value type. Identifier, String, Long...
    /// </summary>
    Value,

    /// <summary>
    /// Assignment A = B.  A should be a value type.
    /// </summary>
    Assignment,

    /// <summary>
    /// A collection of multiple elements.
    /// </summary>
    Group,

    /// <summary>
    /// Modifier.
    /// </summary>
    Modifier,

    /// <summary>
    /// Line Feed. Used for contextual information.
    /// </summary>
    LineFeed,

    /// <summary>
    /// Comment. Used for contextual information.
    /// </summary>
    Comment,
}

internal enum ValueElementType
{
    Identifier, // objectA
    SpecialIdentifier, // @mode
    Binary, // b"Base64"
    String, // "text"
    Long, // -123(long)
    ULong, // 123(ulong)
    Double, // 1.23(double)
    Null, // null
    Bool, // true/false
}

/// <summary>
/// Tinyhand Modifier.
/// </summary>
internal class Modifier : Element
{
    public Modifier()
        : this(Array.Empty<byte>())
    {
    }

    public Modifier(byte[] modifierUtf8)
        : base(ElementType.Modifier)
    {
        this.utf8 = modifierUtf8;
    }

    public Modifier(string valueStringUtf16)
        : base(ElementType.Modifier)
    {
        this.utf16 = valueStringUtf16;
    }

    public override object DeepCopy()
    {
        var instance = (Modifier)base.DeepCopy();
        instance.utf8 = (byte[]?)this.utf8?.Clone();
        return instance;
    }

    public override string ToString() => "Modifier: " + this.Utf16;

    private byte[]? utf8;

    public byte[] Utf8
    {
        get => this.utf8 ??= Encoding.UTF8.GetBytes(this.utf16!);
        set
        {
            this.utf8 = value;
            this.utf16 = null;
        }
    }

    private string? utf16;

    public string Utf16
    {
        get => this.utf16 ??= Encoding.UTF8.GetString(this.utf8!);
        set
        {
            this.utf8 = null;
            this.utf16 = value;
        }
    }
}

/// <summary>
/// A base class for TinyhandValue/TinyhandAssignment and other classes.
/// </summary>
internal class Element
{
    internal Element? parent;
    internal Element? contextualChain;

    public Element(ElementType type)
    {
        this.Type = type;
    }

    public virtual void RemoveChild(Element child)
    {
    }

    public virtual object DeepCopy()
    {
        var instance = (Element)this.MemberwiseClone();
        instance.parent = null;
        instance.contextualChain = (Element?)this.contextualChain?.DeepCopy();

        return instance;
    }

    public void MoveContextualChainTo(Element destination, bool prepend = false)
    {
        if (this.contextualChain == null)
        {
            return;
        }

        if (prepend)
        { // Add to the first chain.
            var last = this.contextualChain;
            while (last.contextualChain != null)
            {
                last = last.contextualChain;
            }

            last.contextualChain = destination.contextualChain;
            destination.contextualChain = this.contextualChain;
            this.contextualChain = null;
        }
        else
        { // Add to the last chain.
            var last = destination.contextualChain;
            if (last == null)
            {
                destination.contextualChain = this.contextualChain;
                this.contextualChain = null;
                return;
            }

            while (last.contextualChain != null)
            {
                last = last.contextualChain;
            }

            last.contextualChain = this.contextualChain;
            this.contextualChain = null;
        }
    }

    public string GetLinePositionString() => $"Line:{this.LineNumber} BytePosition:{this.BytePositionInLine}";

    public Element? Parent => this.parent;

    public ElementType Type { get; }

    public int LineNumber { get; set; }

    public int BytePositionInLine { get; set; }
}

/// <summary>
/// TinyhandValue.
/// </summary>
internal class Value : Element
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Value"/> class.
    /// </summary>
    /// <param name="valueType">Value.</param>
    public Value(ValueElementType valueType)
        : base(ElementType.Value)
    {
        this.ValueType = valueType;
    }

    public ValueElementType ValueType { get; set; }
}

/// <summary>
/// TinyhandLineFeed.
/// </summary>
internal class LineFeed : Element
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LineFeed"/> class.
    /// </summary>
    public LineFeed()
        : base(ElementType.LineFeed)
    {
    }

    public override string ToString() => "LF";
}

/// <summary>
/// TinyhandComment.
/// </summary>
internal class Comment : Element
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Comment"/> class.
    /// </summary>
    /// <param name="commentUtf8">Comment string.</param>
    public Comment(byte[] commentUtf8)
        : base(ElementType.Comment)
    {
        this.commentUtf8 = commentUtf8;
    }

    public override object DeepCopy()
    {
        var instance = (Comment)base.DeepCopy();
        instance.commentUtf8 = (byte[]?)this.commentUtf8?.Clone();
        return instance;
    }

    private byte[]? commentUtf8;

    public byte[] Utf8
    {
        get => this.commentUtf8 ??= Encoding.UTF8.GetBytes(this.commentUtf16!);
        set
        {
            this.commentUtf8 = value;
            this.commentUtf16 = null;
        }
    }

    public override string ToString() => "Comment: " + this.Utf16;

    private string? commentUtf16;

    public string Utf16
    {
        get => this.commentUtf16 ??= Encoding.UTF8.GetString(this.commentUtf8!);
        set
        {
            this.commentUtf8 = null;
            this.commentUtf16 = value;
        }
    }
}

internal class IdentifierValue : Value
{
    public IdentifierValue(bool isSpecial, byte[] identifierUtf8)
        : base(ValueElementType.Identifier)
    {
        this.IsSpecial = isSpecial;
        this.utf8 = identifierUtf8;
    }

    public override object DeepCopy()
    {
        var instance = (IdentifierValue)base.DeepCopy();
        instance.utf8 = (byte[]?)this.utf8?.Clone();
        return instance;
    }

    public bool IsSpecial { get; }

    private byte[]? utf8;

    public byte[] Utf8
    {
        get => this.utf8 ??= Encoding.UTF8.GetBytes(this.utf16!);
        set
        {
            this.utf8 = value;
            this.utf16 = null;
        }
    }

    private string? utf16;

    public string Utf16
    {
        get => this.utf16 ??= Encoding.UTF8.GetString(this.utf8!);
        set
        {
            this.utf8 = null;
            this.utf16 = value;
        }
    }

    public override string ToString() => "Identifier: " + this.Utf16;
}

internal class BoolValue : Value
{
    public BoolValue()
        : base(ValueElementType.Bool)
    {
    }

    public BoolValue(bool valueBool)
        : this()
    {
        this.ValueBool = valueBool;
    }

    public bool ValueBool { get; set; }

    public override string ToString() => "Bool: " + this.ValueBool.ToString();
}

internal class NullValue : Value
{
    public NullValue()
        : base(ValueElementType.Null)
    {
    }

    public NullValue(Element? original)
        : base(ValueElementType.Null)
    {
        this.contextualChain = (Element?)original?.contextualChain?.DeepCopy();
    }

    public override string ToString() => "Null";
}

internal class LongValue : Value
{
    public LongValue()
        : base(ValueElementType.Long)
    {
    }

    public LongValue(long valueLong)
        : this()
    {
        this.ValueLong = valueLong;
    }

    public override string ToString() => "Long: " + this.ValueLong.ToString();

    public long ValueLong { get; set; }
}

internal class ULongValue : Value
{
    public ULongValue()
        : base(ValueElementType.ULong)
    {
    }

    public ULongValue(ulong valueULong)
        : this()
    {
        this.ValueULong = valueULong;
    }

    public override string ToString() => "ULong: " + this.ValueULong.ToString();

    public ulong ValueULong { get; set; }
}

internal class DoubleValue : Value
{
    public DoubleValue()
        : base(ValueElementType.Double)
    {
    }

    public DoubleValue(double valueDouble)
        : this()
    {
        this.ValueDouble = valueDouble;
    }

    public override string ToString() => "Double: " + this.ValueDouble.ToString(CultureInfo.InvariantCulture) + "d";

    public double ValueDouble { get; set; }
}

internal class StringValue : Value
{
    public StringValue()
        : this(Array.Empty<byte>())
    {
    }

    public StringValue(byte[] valueStringUtf8)
        : base(ValueElementType.String)
    {
        this.utf8 = valueStringUtf8;
    }

    public StringValue(Element original, byte[] valueStringUtf8)
        : base(ValueElementType.String)
    {
        this.contextualChain = (Element?)original?.contextualChain?.DeepCopy();
        this.utf8 = valueStringUtf8;
    }

    public StringValue(string valueStringUtf16)
        : base(ValueElementType.String)
    {
        this.utf16 = valueStringUtf16;
    }

    public override object DeepCopy()
    {
        var instance = (StringValue)base.DeepCopy();
        instance.utf8 = (byte[]?)this.utf8?.Clone();
        return instance;
    }

    public override string ToString() => "String: " + this.Utf16;

    public bool IsTripleQuoted { get; set; }

    private byte[]? utf8;

    public byte[] Utf8
    {
        get => this.utf8 ??= Encoding.UTF8.GetBytes(this.utf16!);
        set
        {
            this.utf8 = value;
            this.utf16 = null;
        }
    }

    private string? utf16;

    public string Utf16
    {
        get => this.utf16 ??= Encoding.UTF8.GetString(this.utf8!);
        set
        {
            this.utf8 = null;
            this.utf16 = value;
        }
    }

    public bool HasTripleQuote()
    {
        ReadOnlySpan<byte> s = this.Utf8;

        if (s.Length < 3)
        {
            return false;
        }
        else if (s[0] == (byte)'\"' && s[1] == (byte)'\"' && s[2] == (byte)'\"')
        {
            return true;
        }

        for (var i = 2; i < s.Length; i += 2)
        {
            if (s[i] == (byte)'\"')
            {
                if (s[i - 1] == (byte)'\"' && (i + 1) < s.Length && s[i + 1] == (byte)'\"')
                {
                    return true;
                }
                else if ((i + 2) < s.Length && s[i + 1] == (byte)'\"' && s[i + 2] == (byte)'\"')
                {
                    return true;
                }
            }
        }

        return false;
    }
}

internal class BinaryValue : Value
{
    public BinaryValue()
        : this(null)
    {
    }

    public BinaryValue(byte[]? valueBinary)
        : base(ValueElementType.Binary)
    {
        this.ValueBinary = valueBinary ?? Array.Empty<byte>();
    }

    public override object DeepCopy()
    {
        var instance = (BinaryValue)base.DeepCopy();
        instance.ValueBinary = (byte[])this.ValueBinary.Clone();
        return instance;
    }

    public override string ToString() => "Binary";

    public byte[] ValueBinary { get; set; }

    // public byte[] ValueBinaryToBase64 => Arc.Crypto.Base64.Url.FromByteArrayToUtf8(this.ValueBinary); // Base64.EncodeToBase64Utf8(this.ValueBinary);
}

/// <summary>
/// A base class for TinyhandValue/TinyhandAssignment and other classes.
/// </summary>
internal class Assignment : Element
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Assignment"/> class.
    /// </summary>
    public Assignment()
        : this(null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Assignment"/> class.
    /// </summary>
    /// <param name="leftElement">The left part of an assignment.</param>
    /// /// <param name="rightElement">The right part of an assignment.</param>
    public Assignment(Element? leftElement, Element? rightElement)
        : base(ElementType.Assignment)
    {
        if (leftElement != null)
        {
            this.LeftElement = leftElement;
        }

        if (rightElement != null)
        {
            this.RightElement = rightElement;
        }
    }

    public override void RemoveChild(Element child)
    {
        if (child.parent == this)
        {
            child.parent = null;
        }

        if (this.leftElement == child)
        {
            this.leftElement = null;
        }

        if (this.rightElement == child)
        {
            this.rightElement = null;
        }
    }

    public override object DeepCopy()
    {
        var instance = (Assignment)base.DeepCopy();

        if (this.leftElement != null)
        {
            instance.leftElement = (Element)this.leftElement.DeepCopy();
            instance.leftElement.parent = instance;
        }

        if (this.rightElement != null)
        {
            instance.rightElement = (Element)this.rightElement.DeepCopy();
            instance.rightElement.parent = instance;
        }

        return instance;
    }

    public override string ToString() => (this.LeftElement == null ? "Null" : this.LeftElement.ToString()) +
        " = " + (this.RightElement == null ? "Null" : this.RightElement.ToString());

    private Element? leftElement;

    public Element? LeftElement
    {
        get => this.leftElement;
        set
        {
            if (this.leftElement != null)
            {
                this.leftElement.parent = null;
            }

            this.leftElement = value;
            if (this.leftElement != null)
            {
                this.leftElement.parent?.RemoveChild(this.leftElement);
                this.leftElement.parent = this;
            }
        }
    }

    private Element? rightElement;

    public Element? RightElement
    {
        get => this.rightElement;
        set
        {
            if (this.rightElement != null)
            {
                this.rightElement.parent = null;
            }

            this.rightElement = value;
            if (this.rightElement != null)
            {
                this.rightElement.parent?.RemoveChild(this.rightElement);
                this.rightElement.parent = this;
            }
        }
    }
}

/// <summary>
/// TinyhandGroup holds multiple Elements.
/// </summary>
internal class Group : Element, IEnumerable<Element>
{
    public static Group Empty = new Group();

    // { <-forwardContextual  } <-contextualChain
    internal Element forwardContextual = new Element(ElementType.None);

    /// <summary>
    /// Initializes a new instance of the <see cref="Group"/> class.
    /// </summary>
    public Group()
        : base(ElementType.Group)
    {
        this.ElementList = new();
    }

    public override string ToString() => "Group(" + this.ElementList.Count + ")";

    /// <summary>
    /// Initializes a new instance of the <see cref="Group"/> class.
    /// </summary>
    /// <param name="capacity">The number of elements that the new group can initially store.</param>
    public Group(int capacity)
        : base(ElementType.Group)
    {
        this.ElementList = new(capacity);
    }

    public override void RemoveChild(Element child)
    {
        if (child.parent == this)
        {
            child.parent = null;
            this.ElementList.Remove(child);
        }
    }

    public override object DeepCopy()
    {
        var instance = (Group)base.DeepCopy();
        instance.forwardContextual = (Element)this.forwardContextual.DeepCopy();
        instance.ElementList = new List<Element>();
        foreach (var x in this.ElementList)
        {
            var item = (Element)x.DeepCopy();
            item.parent = instance;
            instance.ElementList.Add(item);
        }

        return instance;
    }

    public List<Element> ElementList { get; private set; }

    public virtual void Add(Element element)
    {
        element.parent?.RemoveChild(element);
        this.ElementList.Add(element);
        element.parent = this;
    }

    public IEnumerator<Element> GetEnumerator() => ((IEnumerable<Element>)this.ElementList).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.ElementList).GetEnumerator();
}
