// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Tinyhand.Tree;

namespace Tinyhand;

/// <summary>
/// Represents a collection of utf-16 key (ReadOnlySpan&lt;char&gt;) and string pairs for each culture.
/// </summary>
public class KeyString
{
    public const int MaxKeyLength = 256; // The maximum length of a key.
    public const int MaxStringLength = 16 * 1024; // The maximum length of a string.
    public const int MaxTinyhandLength = 4 * 1024 * 1024; // The maximum length of tinyhand file.

    public static string ShortNameToCultureName(string name) => name switch
    {
        "ja" => "ja-JP",
        "en" => "en-US",
        _ => name,
    };

    public KeyString(bool ignoreCase = true)
    {
        this.IgnoreCase = ignoreCase;
        var table = new Utf16Hashtable<string>();
        this.currentCultureTable = table;
        this.defaultCultureTable = table;
        this.defaultCultureName = "en-US";
        this.currentCultureInfo = new CultureInfo(this.defaultCultureName);

        this.cultureTable = new Utf16Hashtable<Utf16Hashtable<string>>();
        this.cultureTable.TryAdd(this.currentCultureInfo.Name, table);
    }

    public static KeyString Instance { get; } = new KeyString();

    public bool IgnoreCase { get; }

    public string ErrorMessage { get; set; } = "No KeyString"; // Error message.

    public CultureInfo CurrentCulture => this.currentCultureInfo;

    /// <summary>
    /// Gets the name of the current culture.
    /// </summary>
    /// <returns>The name of the current culture.</returns>
    public string CurrentCultureName => this.CurrentCulture.Name;

    /// <summary>
    /// Get a string that matches the identifier.<br/>
    /// Current culture -> Default culture -> Error message, if not found.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>Returns a string. If no string is found, the return value is the error message.</returns>
    public string Get(string? identifier) => this.GetInternal(identifier, this.ErrorMessage);

    /// <summary>
    /// Get a string that matches the identifier.<br/>
    /// Current culture -> Default culture -> the identifier itself, if not found.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>Returns a string. If no string is found, the return value is the identifier itself.</returns>
    public string GetOrIdentifier(string? identifier) => this.GetInternal(identifier, identifier ?? string.Empty);

    /// <summary>
    /// Get a string that matches the identifier.<br/>
    /// Current culture -> Default culture -> <see cref="string.Empty"/>, if not found.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>Returns a string. If no string is found, the return value is <see cref="string.Empty"/>.</returns>
    public string GetOrEmpty(string? identifier) => this.GetInternal(identifier, string.Empty);

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

            Utf16Hashtable<string>? table = null;
            if (!this.cultureTable.TryGetValue(cultureName, out table))
            {
                table = new Utf16Hashtable<string>();
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
    /// <param name="culture">The target culture.</param>
    /// <param name="tinyhandPath">The path of the tinyhand file.</param>
    /// <param name="reset"><see langword="true"/> to reset key/string data before loading.</param>
    public void Load(string culture, string tinyhandPath, bool reset = false)
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
    /// <param name="culture">The target culture.</param>
    /// <param name="assemblyname">The assembly name.</param>
    /// <param name="reset"><see langword="true"/> to reset key/string data before loading.</param>
    public void LoadAssembly(string culture, string assemblyname, bool reset = false)
    {
        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        using (var stream = asm.GetManifestResourceStream(asm.GetName().Name + "." + assemblyname))
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

    private void Load(string culture, Stream stream, bool reset)
    {
        if (stream.Length > MaxTinyhandLength)
        {
            throw new OverflowException();
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var group = (Group)TinyhandParser.Parse(ms.ToArray());

        Utf16Hashtable<string>? table = null;
        culture = ShortNameToCultureName(culture);
        if (!this.cultureTable.TryGetValue(culture, out table))
        {
            table = new Utf16Hashtable<string>();
            this.cultureTable.TryAdd(culture, table);
        }
        else if (reset)
        {// Clear
            table.Clear();
        }

        foreach (var x in group)
        {
            if (x.TryGetLeft_IdentifierUtf16(out var identifier))
            {
                if (this.IgnoreCase)
                {
                    identifier = identifier.ToLower();
                }

                if (x.TryGetRight_Value_String(out var valueString) && valueString.ValueStringUtf16.Length <= MaxStringLength)
                {
                    table.TryAdd(identifier, valueString.ValueStringUtf16);
                }
            }
        }

        if (culture == this.defaultCultureName)
        {
            Volatile.Write(ref this.defaultCultureTable, table);
        }

        return;
    }

    private string GetInternal(string? identifier, string alternative)
    {
        if (identifier == null)
        {
            return alternative;
        }
        else if (this.IgnoreCase)
        {
            identifier = identifier.ToLower();
        }

        string? result;
        if (this.currentCultureTable.TryGetValue(identifier, out result))
        {// Found in the current culture table.
            return result;
        }

        if (this.currentCultureTable != this.defaultCultureTable && this.defaultCultureTable.TryGetValue(identifier, out result))
        {// Found in the default culture table.
            return result;
        }

        return alternative;
    }

    private object syncObject = new();
    private Utf16Hashtable<string> currentCultureTable; // Current culture data (name to string).
    private Utf16Hashtable<string> defaultCultureTable; // Default culture data (name to string).
    private Utf16Hashtable<Utf16Hashtable<string>> cultureTable; // Culture and data (culture to Utf16Hashtable<string>).
    private string defaultCultureName; // Default culture
    private CultureInfo currentCultureInfo;
}
