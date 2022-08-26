// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Tinyhand.Tree;

namespace Tinyhand;

/// <summary>
/// Represents a collection of <see langword="ulong"/> hash and string pairs for each culture.
/// </summary>
public static class HashedString
{
    public const int MaxKeyLength = 256; // The maximum length of a key.
    public const int MaxStringLength = 16 * 1024; // The maximum length of a string.
    public const int MaxTinyhandLength = 4 * 1024 * 1024; // The maximum length of tinyhand file.

    public static ulong IdentifierToHash(string identifier) => FarmHash.Hash64(identifier);

    public static string ShortNameToCultureName(string name) => name switch
    {
        "ja" => "ja-JP",
        "en" => "en-US",
        _ => name,
    };

    static HashedString()
    {
        var table = new UInt64Hashtable<string>();
        currentCultureTable = table;
        defaultCultureTable = table;
        defaultCultureName = "en-US";
        currentCultureInfo = new CultureInfo(defaultCultureName);

        cultureTable = new();
        cultureTable.TryAdd(currentCultureInfo.Name, table);
    }

    public static string ErrorMessage { get; set; } = "No KeyString"; // Error message.

    public static CultureInfo CurrentCulture => currentCultureInfo;

    /// <summary>
    /// Gets the name of the current culture.
    /// </summary>
    /// <returns>The name of the current culture.</returns>
    public static string CurrentCultureName => CurrentCulture.Name;

    /// <summary>
    /// Get a string that matches the hash.<br/>
    /// Current culture -> Default culture -> Error message, if not found.
    /// </summary>
    /// <param name="hash"><see cref="ulong"/> hash.</param>
    /// <returns>Returns a string. If no string is found, the return value is the error message.</returns>
    public static string Get(ulong hash) => GetInternal(hash, ErrorMessage);

    /// <summary>
    /// Get(hash) + <see cref="string.Format(string, object)"/>.
    /// </summary>
    /// <param name="hash"><see cref="ulong"/> hash.</param>
    /// <param name="obj1">The object to format.</param>
    /// <returns>Returns a string. If no string is found, the return value is the error message.</returns>
    public static string Get(ulong hash, object obj1) => string.Format(GetInternal(hash, ErrorMessage), obj1);

    /// <summary>
    /// Get(hash) + <see cref="string.Format(string, object)"/>.
    /// </summary>
    /// <param name="hash"><see cref="ulong"/> hash.</param>
    /// <param name="obj1">The object to format.</param>
    /// <param name="obj2">The object to format2.</param>
    /// <returns>Returns a string. If no string is found, the return value is the error message.</returns>
    public static string Get(ulong hash, object obj1, object obj2) => string.Format(GetInternal(hash, ErrorMessage), obj1, obj2);

    /// <summary>
    /// Get(hash) + <see cref="string.Format(string, object)"/>.
    /// </summary>
    /// <param name="hash"><see cref="ulong"/> hash.</param>
    /// <param name="obj1">The object to format.</param>
    /// <param name="obj2">The object to format2.</param>
    /// <param name="obj3">The object to format3.</param>
    /// <returns>Returns a string. If no string is found, the return value is the error message.</returns>
    public static string Get(ulong hash, object obj1, object obj2, object obj3) => string.Format(GetInternal(hash, ErrorMessage), obj1, obj2, obj3);

    /// <summary>
    /// Get a string that matches the identifier.<br/>
    /// Current culture -> Default culture -> <see cref="string.Empty"/>, if not found.
    /// </summary>
    /// <param name="hash"><see cref="ulong"/> hash.</param>
    /// <returns>Returns a string. If no string is found, the return value is <see cref="string.Empty"/>.</returns>
    public static string GetOrEmpty(ulong hash) => GetInternal(hash, string.Empty);

    /// <summary>
    /// Get a string that matches the identifier.<br/>
    /// Current culture -> Default culture -> The alternative string, if not found.
    /// </summary>
    /// <param name="hash"><see cref="ulong"/> hash.</param>
    /// <param name="alternative">The alternative string.</param>
    /// <returns>Returns a string. If no string is found, the return value is the alternative string.</returns>
    public static string GetOrAlternative(ulong hash, string alternative) => GetInternal(hash, alternative);

    /// <summary>
    /// Get a string that matches the identifier.<br/>
    /// Current culture -> Default culture -> Error message, if not found.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>Returns a string. If no string is found, the return value is the error message.</returns>
    public static string Get(string identifier) => GetInternal(IdentifierToHash(identifier), ErrorMessage);

    /// <summary>
    /// Get a string that matches the identifier.<br/>
    /// Current culture -> Default culture -> the identifier itself, if not found.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>Returns a string. If no string is found, the return value is the identifier itself.</returns>
    public static string GetOrIdentifier(string identifier) => GetInternal(IdentifierToHash(identifier), identifier ?? string.Empty);

    /// <summary>
    /// Get a string that matches the identifier.<br/>
    /// Current culture -> Default culture -> <see cref="string.Empty"/>, if not found.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>Returns a string. If no string is found, the return value is <see cref="string.Empty"/>.</returns>
    public static string GetOrEmpty(string identifier) => GetInternal(IdentifierToHash(identifier), string.Empty);

    /// <summary>
    /// Get a string that matches the identifier.<br/>
    /// Current culture -> Default culture -> The alternative string, if not found.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <param name="alternative">The alternative string.</param>
    /// <returns>Returns a string. If no string is found, the return value is the alternative string.</returns>
    public static string GetOrAlternative(string identifier, string alternative) => GetInternal(IdentifierToHash(identifier), alternative);

    /// <summary>
    /// Set the default culture.
    /// </summary>
    /// <param name="cultureName">The name of the default culture.</param>
    public static void SetDefaultCulture(string cultureName)
    {
        cultureName = ShortNameToCultureName(cultureName);

        lock (syncObject)
        {
            defaultCultureName = cultureName;

            UInt64Hashtable<string>? table = null;
            if (!cultureTable.TryGetValue(cultureName, out table))
            {
                table = new();
            }

            Volatile.Write(ref defaultCultureTable, table);
        }
    }

    /// <summary>
    /// Change the current culture.
    /// </summary>
    /// <param name="cultureName">The culture name.</param>
    /// <returns><see langword="true" /> if the culture change was successfully done.</returns>
    public static bool ChangeCulture(string cultureName)
    {
        cultureName = ShortNameToCultureName(cultureName);
        if (cultureName == CurrentCulture.Name)
        {
            return true;
        }

        var cultureInfo = new CultureInfo(cultureName);

        lock (syncObject)
        {
            if (!cultureTable.TryGetValue(cultureName, out var table))
            {
                return false;
            }

            Volatile.Write(ref currentCultureTable, table);
            Volatile.Write(ref currentCultureInfo, cultureInfo);
        }

        return true;
    }

    public static void Clear()
    {
        lock (syncObject)
        {
            foreach (var x in cultureTable.ToArray())
            {
                x.Clear();
            }
        }
    }

    /// <summary>
    /// Load key/string data from a tinyhand file.
    /// </summary>
    /// <param name="culture">The target culture (null for default culture).</param>
    /// <param name="tinyhandPath">The path of the tinyhand file.</param>
    /// <param name="reset"><see langword="true"/> to reset key/string data before loading.</param>
    public static void Load(string? culture, string tinyhandPath, bool reset = false)
    {
        using (var fs = File.OpenRead(tinyhandPath))
        {
            lock (syncObject)
            {
                Load(culture, fs, reset);
            }
        }
    }

    /// <summary>
    /// Load key/string data from a stream.
    /// </summary>
    /// <param name="culture">The target culture.</param>
    /// <param name="stream">Stream.</param>
    /// <param name="reset"><see langword="true"/> to reset key/string data before loading.</param>
    public static void LoadStream(string culture, Stream stream, bool reset = false)
    {
        lock (syncObject)
        {
            Load(culture, stream, reset);
        }
    }

#if !NETFX_CORE
    /// <summary>
    /// Load from assembly.
    /// </summary>
    /// <param name="culture">The target culture (null for default culture).</param>
    /// <param name="assembly">The assembly.</param>
    /// <param name="name">The resource name.</param>
    /// <param name="reset"><see langword="true"/> to reset key/string data before loading.</param>
    public static void LoadAssembly(string? culture, System.Reflection.Assembly assembly, string name, bool reset = false)
    {
        using (var stream = assembly.GetManifestResourceStream(assembly.GetName().Name + "." + name))
        {
            if (stream == null)
            {
                throw new FileNotFoundException();
            }

            lock (syncObject)
            {
                Load(culture, stream, reset);
            }
        }
    }
#endif

    private static void Load(string? culture, Stream stream, bool reset)
    {
        if (stream.Length > MaxTinyhandLength)
        {
            throw new OverflowException();
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var element = (Group)TinyhandParser.Parse(ms.ToArray());

        UInt64Hashtable<string>? table = null;
        culture ??= defaultCultureName;
        culture = ShortNameToCultureName(culture);
        if (!cultureTable.TryGetValue(culture, out table))
        {
            table = new();
            cultureTable.TryAdd(culture, table);
        }
        else if (reset)
        {// Clear
            table.Clear();
        }

        LoadElement(table, string.Empty, element);

        if (culture == defaultCultureName)
        {
            Volatile.Write(ref defaultCultureTable, table);
        }

        return;
    }

    private static void LoadElement(UInt64Hashtable<string> table, string groupName, Element element)
    {
        if (element is not Group group)
        {
            return;
        }

        foreach (var x in group)
        {
            if (x is Assignment assignment)
            {
                if (assignment.LeftElement is Value_Identifier valueIdentifier)
                {// Identifier = ?
                    string identifier;
                    if (string.IsNullOrEmpty(groupName))
                    {
                        identifier = valueIdentifier.IdentifierUtf16;
                    }
                    else
                    {
                        identifier = groupName + "." + valueIdentifier.IdentifierUtf16;
                    }

                    if (assignment.RightElement is Value_String valueString)
                    {// Identifier = "String"
                        if (valueString.ValueStringUtf16.Length <= MaxStringLength)
                        {
                            table.TryAdd(IdentifierToHash(identifier), valueString.ValueStringUtf16);
                        }
                    }
                    else if (assignment.RightElement is Group subgroup)
                    {
                        LoadElement(table, identifier, subgroup);
                    }
                }
            }
        }
    }

    private static string GetInternal(ulong hash, string alternative)
    {
        string? result;
        if (currentCultureTable.TryGetValue(hash, out result))
        {// Found in the current culture table.
            return result;
        }

        if (currentCultureTable != defaultCultureTable && defaultCultureTable.TryGetValue(hash, out result))
        {// Found in the default culture table.
            return result;
        }

        return alternative;
    }

    private static object syncObject = new();
    private static UInt64Hashtable<string> currentCultureTable; // Current culture data (name to string).
    private static UInt64Hashtable<string> defaultCultureTable; // Default culture data (name to string).
    private static Utf16Hashtable<UInt64Hashtable<string>> cultureTable; // Culture and data (culture to Utf16Hashtable<string>).
    private static string defaultCultureName; // Default culture
    private static CultureInfo currentCultureInfo;
}
