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
public class HashedString
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

    public HashedString()
    {
        var table = new UInt64Hashtable<string>();
        this.currentCultureTable = table;
        this.defaultCultureTable = table;
        this.defaultCultureName = "en-US";
        this.currentCultureInfo = new CultureInfo(this.defaultCultureName);

        this.cultureTable = new();
        this.cultureTable.TryAdd(this.currentCultureInfo.Name, table);
    }

    public static HashedString Instance { get; } = new HashedString();

    public string ErrorMessage { get; set; } = "No KeyString"; // Error message.

    public CultureInfo CurrentCulture => this.currentCultureInfo;

    /// <summary>
    /// Gets the name of the current culture.
    /// </summary>
    /// <returns>The name of the current culture.</returns>
    public string CurrentCultureName => this.CurrentCulture.Name;

    /// <summary>
    /// Get a string that matches the hash.<br/>
    /// Current culture -> Default culture -> Error message, if not found.
    /// </summary>
    /// <param name="hash"><see cref="ulong"/> hash.</param>
    /// <returns>Returns a string. If no string is found, the return value is the error message.</returns>
    public string Get(ulong hash) => this.GetInternal(hash, this.ErrorMessage);

    /// <summary>
    /// Get a string that matches the identifier.<br/>
    /// Current culture -> Default culture -> <see cref="string.Empty"/>, if not found.
    /// </summary>
    /// <param name="hash"><see cref="ulong"/> hash.</param>
    /// <returns>Returns a string. If no string is found, the return value is <see cref="string.Empty"/>.</returns>
    public string GetOrEmpty(ulong hash) => this.GetInternal(hash, string.Empty);

    /// <summary>
    /// Get a string that matches the identifier.<br/>
    /// Current culture -> Default culture -> The alternative string, if not found.
    /// </summary>
    /// <param name="hash"><see cref="ulong"/> hash.</param>
    /// <param name="alternative">The alternative string.</param>
    /// <returns>Returns a string. If no string is found, the return value is the alternative string.</returns>
    public string GetOrAlternative(ulong hash, string alternative) => this.GetInternal(hash, alternative);

    /// <summary>
    /// Get a string that matches the identifier.<br/>
    /// Current culture -> Default culture -> Error message, if not found.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>Returns a string. If no string is found, the return value is the error message.</returns>
    public string Get(string identifier) => this.GetInternal(IdentifierToHash(identifier), this.ErrorMessage);

    /// <summary>
    /// Get a string that matches the identifier.<br/>
    /// Current culture -> Default culture -> the identifier itself, if not found.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>Returns a string. If no string is found, the return value is the identifier itself.</returns>
    public string GetOrIdentifier(string identifier) => this.GetInternal(IdentifierToHash(identifier), identifier ?? string.Empty);

    /// <summary>
    /// Get a string that matches the identifier.<br/>
    /// Current culture -> Default culture -> <see cref="string.Empty"/>, if not found.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>Returns a string. If no string is found, the return value is <see cref="string.Empty"/>.</returns>
    public string GetOrEmpty(string identifier) => this.GetInternal(IdentifierToHash(identifier), string.Empty);

    /// <summary>
    /// Get a string that matches the identifier.<br/>
    /// Current culture -> Default culture -> The alternative string, if not found.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <param name="alternative">The alternative string.</param>
    /// <returns>Returns a string. If no string is found, the return value is the alternative string.</returns>
    public string GetOrAlternative(string identifier, string alternative) => this.GetInternal(IdentifierToHash(identifier), alternative);

    /// <summary>
    /// Set the default culture.
    /// </summary>
    /// <param name="cultureName">The name of the default culture.</param>
    public void SetDefaultCulture(string cultureName)
    {
        cultureName = ShortNameToCultureName(cultureName);

        lock (this.syncObject)
        {
            this.defaultCultureName = cultureName;

            UInt64Hashtable<string>? table = null;
            if (!this.cultureTable.TryGetValue(cultureName, out table))
            {
                table = new();
            }

            Volatile.Write(ref this.defaultCultureTable, table);
        }
    }

    /// <summary>
    /// Change the current culture.
    /// </summary>
    /// <param name="cultureName">The culture name.</param>
    public void ChangeCulture(string cultureName)
    {
        cultureName = ShortNameToCultureName(cultureName);
        if (cultureName == this.CurrentCulture.Name)
        {
            return;
        }

        var cultureInfo = new CultureInfo(cultureName);

        lock (this.syncObject)
        {
            if (!this.cultureTable.TryGetValue(cultureName, out var table))
            {
                throw new CultureNotFoundException();
            }

            Volatile.Write(ref this.currentCultureTable, table);
            Volatile.Write(ref this.currentCultureInfo, cultureInfo);
        }
    }

    /// <summary>
    /// Load key/string data from a tinyhand file.
    /// </summary>
    /// <param name="culture">The target culture (null for default culture).</param>
    /// <param name="tinyhandPath">The path of the tinyhand file.</param>
    /// <param name="reset"><see langword="true"/> to reset key/string data before loading.</param>
    public void Load(string? culture, string tinyhandPath, bool reset = false)
    {
        using (var fs = File.OpenRead(tinyhandPath))
        {
            lock (this.syncObject)
            {
                this.Load(culture, fs, reset);
            }
        }
    }

    /// <summary>
    /// Load key/string data from a stream.
    /// </summary>
    /// <param name="culture">The target culture.</param>
    /// <param name="stream">Stream.</param>
    /// <param name="reset"><see langword="true"/> to reset key/string data before loading.</param>
    public void LoadStream(string culture, Stream stream, bool reset = false)
    {
        lock (this.syncObject)
        {
            this.Load(culture, stream, reset);
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
    public void LoadAssembly(string? culture, System.Reflection.Assembly assembly, string name, bool reset = false)
    {
        using (var stream = assembly.GetManifestResourceStream(assembly.GetName().Name + "." + name))
        {
            if (stream == null)
            {
                throw new FileNotFoundException();
            }

            lock (this.syncObject)
            {
                this.Load(culture, stream, reset);
            }
        }
    }
#endif

    private void Load(string? culture, Stream stream, bool reset)
    {
        if (stream.Length > MaxTinyhandLength)
        {
            throw new OverflowException();
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var element = (Group)TinyhandParser.Parse(ms.ToArray());

        UInt64Hashtable<string>? table = null;
        culture ??= this.defaultCultureName;
        culture = ShortNameToCultureName(culture);
        if (!this.cultureTable.TryGetValue(culture, out table))
        {
            table = new();
            this.cultureTable.TryAdd(culture, table);
        }
        else if (reset)
        {// Clear
            table.Clear();
        }

        this.LoadElement(table, string.Empty, element);

        if (culture == this.defaultCultureName)
        {
            Volatile.Write(ref this.defaultCultureTable, table);
        }

        return;
    }

    private void LoadElement(UInt64Hashtable<string> table, string groupName, Element element)
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
                        this.LoadElement(table, identifier, subgroup);
                    }
                }
            }
        }
    }

    private string GetInternal(ulong hash, string alternative)
    {
        string? result;
        if (this.currentCultureTable.TryGetValue(hash, out result))
        {// Found in the current culture table.
            return result;
        }

        if (this.currentCultureTable != this.defaultCultureTable && this.defaultCultureTable.TryGetValue(hash, out result))
        {// Found in the default culture table.
            return result;
        }

        return alternative;
    }

    private object syncObject = new();
    private UInt64Hashtable<string> currentCultureTable; // Current culture data (name to string).
    private UInt64Hashtable<string> defaultCultureTable; // Default culture data (name to string).
    private Utf16Hashtable<UInt64Hashtable<string>> cultureTable; // Culture and data (culture to Utf16Hashtable<string>).
    private string defaultCultureName; // Default culture
    private CultureInfo currentCultureInfo;
}
