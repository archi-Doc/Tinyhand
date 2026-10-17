## Tinyhand
![Nuget](https://img.shields.io/nuget/v/Tinyhand) ![Build and Test](https://github.com/archi-Doc/Tinyhand/workflows/Build%20and%20Test/badge.svg)

[Tinyhand](https://github.com/archi-Doc/Tinyhand)というソースジェネレーターを使用したシリアライザを作りました。といっても、neueccさんとAArnottさんの[MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp)を99％ベースに、ソースジェネレーター対応にして少し機能を追加しただけの代物です。

本家はGitHub [archi-Doc/Tinyhand](https://github.com/archi-Doc/Tinyhand)にあります。

[MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp)からの変更点としては、

- ソースジェネレーターなので、動的コード生成のコスト少ない。
- デシリアライズ時のデフォルト値の指定が可能。
- null許容・非許容の取り扱いを改善（非許容の場合は自動でインスタンス生成）。
- インスタンスの再利用可能。

といったところです。



## Table of Contents

- [Requirements](#requirements)
- [NativeAOT](NativeAOT.md)
- [Quick Start](#quick-start)
- [Performance](#performance)
- [Serialization Target](#serialization-target)
  - [Readonly and Getter-only](#readonly-and-getter-only)
  - [Init-only property and Record type](#init-only-property-and-record-type)
  - [Include private members](#include-private-members)
  - [Explicit key only](#explicit-key-only)
- [Features](#features)
  - [Handling nullable reference types](#handling-nullable-reference-types)
  - [Default value](#default-value)
  - [Reconstruct](#reconstruct)
  - [Reuse Instance](#reuse-instance)
  - [Union](#union)
  - [Text Serializaiton](#text-serialization)
  - [Versioning](#versioning)
  - [Serialization Callback](#serialization-callback)
  - [Built-in supported types](#built-in-supported-types)
  - [LZ4 Compression](#lz4-Compression)



## Requirements

**Visual Studio 2026** 以降、**C# 14** 以降、**.NET 10** 以降を使用します。

NativeAOT では、使用する型の formatter と閉じたジェネリック型の登録コードを Source Generator が生成します。[設定・移行手順・検証内容](NativeAOT.md)を参照してください。



## Quick Start

まずはPackage Manager Consoleでインストール。

```
Install-Package Tinyhand
```

サンプルコードです。

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Tinyhand;

namespace ConsoleApp1
{
    [TinyhandObject] // シリアライズ対象のクラスにTinyhandObject属性を追加します
    public partial class MyClass // ソースジェネレーターでコード追加するので、partial classが必須
    {
        // シリアライズ対象のメンバーにKey属性（シリアライズ時の識別子）を追加します。intとstringが指定できますが、クラス毎に統一する必要があります
        // もちろんユニークな識別子が必要で、バージョニングの際には重要です
        [Key(0)]
        public int Age { get; set; }

        [Key(1)]
        public string FirstName { get; set; } = string.Empty;

        [Key(2)]
        [DefaultValue("Doe")] // デフォルト値。デシリアライズ時に対応するデータがない場合、この値が代入されます
        public string LastName { get; set; } = string.Empty;

        // IgnoreMember属性を付けると、シリアライズ対象から外れます
        [IgnoreMember]
        public string FullName { get { return FirstName + LastName; } }

        [Key(3)]
        public List<string> Friends { get; set; } = default!; // null非許容参照型。自動で新しいインスタンスが生成されます

        [Key(4)]
        public int[]? Ids { get; set; } // null許容の場合は、nullが代入

        public MyClass()
        {// デシリアライズのため、デフォルトコンストラクタ（引数のないコンストラクタ）が必須です
        }
    }

    [TinyhandObject]
    public partial class EmptyClass
    {
    }

    class Program
    {
        static void Main(string[] args)
        {
            var myClass = new MyClass() { Age = 10, FirstName = "hoge", LastName = "huga", };
            var b = TinyhandSerializer.Serialize(myClass);// 普通にシリアライズ
            var myClass2 = TinyhandSerializer.Deserialize<MyClass>(b);// 普通にデシリアライズ

            b = TinyhandSerializer.Serialize(new EmptyClass()); // 空のデータ
            var myClass3 = TinyhandSerializer.Deserialize<MyClass>(b); // 対応するデータがないので、デフォルト値が使用されます。
            
            var myClassRecon = TinyhandSerializer.Reconstruct<MyClass>(); // インスタンス生成。それぞれのメンバーには、デフォルト値諸々が入ります。
        }
    }
}
```



## Performance

[protobuf-net](https://github.com/protobuf-net/protobuf-net) と [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp) 相手のベンチマークです。

protobuf-netはもちろん、本家よりも速いです。

| Method                       |     Mean |   Error |  StdDev |   Median |  Gen 0 | Gen 1 | Gen 2 | Allocated |
| ---------------------------- | -------: | ------: | ------: | -------: | -----: | ----: | ----: | --------: |
| SerializeProtoBuf            | 449.3 ns | 4.31 ns | 6.04 ns | 452.5 ns | 0.0973 |     - |     - |     408 B |
| SerializeMessagePack         | 163.9 ns | 1.33 ns | 1.90 ns | 163.2 ns | 0.0134 |     - |     - |      56 B |
| SerializeTinyhand            | 140.4 ns | 3.32 ns | 4.97 ns | 141.0 ns | 0.0134 |     - |     - |      56 B |
| DeserializeProtoBuf          | 737.6 ns | 2.45 ns | 3.66 ns | 737.1 ns | 0.0763 |     - |     - |     320 B |
| DeserializeMessagePack       | 306.6 ns | 0.66 ns | 0.93 ns | 306.7 ns | 0.0668 |     - |     - |     280 B |
| DeserializeTinyhand          | 280.4 ns | 2.22 ns | 3.19 ns | 280.7 ns | 0.0668 |     - |     - |     280 B |
| SerializeMessagePackString   | 179.1 ns | 2.64 ns | 3.79 ns | 179.3 ns | 0.0153 |     - |     - |      64 B |
| SerializeTinyhandString      | 143.8 ns | 2.18 ns | 3.19 ns | 142.1 ns | 0.0153 |     - |     - |      64 B |
| DeserializeMessagePackString | 320.1 ns | 1.12 ns | 1.60 ns | 319.7 ns | 0.0668 |     - |     - |     280 B |
| DeserializeTinyhandString    | 311.3 ns | 1.31 ns | 1.84 ns | 310.8 ns | 0.0744 |     - |     - |     312 B |



## Serialization Target

シリアライズ対象のお話。

publicメンバーはデフォルトでシリアライズ対象で、`Key` または `KeyAsName` または `IgnoreMember`属性 のいずれかを指定する必要があります。

protected/privateメンバーはシリアライズ対象外のため、属性を付ける必要はありません。`Key`または`KeyAsName` 属性を付けると、明示的にシリアライズ対象に追加することが出来ます。



- `Key` または `KeyAsName `属性（メンバー名がそのまま`Key`になる）でシリアライズ時の識別子を指定します。
- `IgnoreMember` 属性を付けると対象から外れます。
- `ImplicitKeyAsName` 属性をクラスに付けると、シリアライズ対象のメンバーにすべて自動で `KeyAsName`属性が付きます。

```csharp
[TinyhandObject]
public partial class DefaultBehaviourClass
{
    [Key(0)]
    public int X; // Key属性が必要

    public int Y { get; private set; } // private setterでシリアライズ対象外なのでKeyは不要

    [Key(1)]
    private int Z; // プライベートメンバーでも、明示的にシリアライズ対象にすることが出来ます
}

[TinyhandObject(ImplicitKeyAsName = true)] // すべてのメンバーにKeyAsName属性を付ける
public partial class KeyAsNameClass
{
    public int X; // key "X"

    public int Y { get; private set; } // シリアライズ対象外

    [Key("Z")]
    private int Z; // key "Z"
    
    [KeyAsName]
    public int A; // key "A".
}
```



### Readonly and Getter-only

readonly と getter-only property はサポートされません（シリアライズ対象外）。 

```csharp
[TinyhandObject]
public partial class ReadonlyGetteronlyClass
{
    [Key(0)]
    public readonly int X; // Error!

    [Key(1)]
    public int Y { get; } = 0; // Error!
}
```

技術的には可能ですが、アンセーフコードと動的コード生成が必要で、信条的にも readonly / getter-only をシリアライズする必要はなかろうと考えています。ご意見お待ちしています。



### Init-only property and Record type

Init-only property と```record``` 型はサポートされます。

```csharp
[TinyhandObject]
public partial record RecordClass // もちろんpartial
{// record型の場合は、デフォルトコンストラクタ不要です
    [Key(0)]
    public int X { get; init; } // initプロパティーも無理矢理デシリアライズします

    [Key(1)]
    public string A { get; init; } = default!;
}

[TinyhandObject(ImplicitKeyAsName = true)] // こんな感じで記述できます。string keyになるので、int keyより多少パフォーマンス落ちます。
public partial record RecordClass2(int X, string A);
```



### Include private members

`IncludePrivateMembers` を `true` にすると、private/protectedもまとめてシリアライズ対象にすることが出来ます。

```csharp
[TinyhandObject(IncludePrivateMembers = true)]
public partial class IncludePrivateClass
{
    [Key(0)]
    public int X; // Key必須

    [Key(1)]
    public int Y { get; private set; } // Key必須になる

    [IgnoreMember]
    private int Z; // シリアライズ対象外にする
}
```



### Explicit key only

`ExplicitKeyOnly` を `true` にすると、`Key` 属性か `KeyAsName` 属性が付いたメンバーのみシリアライズ対象になります。

```csharp
[TinyhandObject(ExplicitKeyOnly = true)]
public partial class ExplicitKeyClass
{
    public int X; // シリアライズ対象外

    [Key(0)]
    public int Y; // シリアライズ対象
}
```



## Features

### Handling nullable reference types

Tinyhandは null許容参照型・非許容参照型を適切にデシリアライズします。つまり、空のデータや、バージョニングで対応するメンバーがないデータが来ても、null非許容参照型のインスタンスを自動で補完します。

```csharp
[TinyhandObject(ImplicitKeyAsName = true)]
public partial class NullableTestClass
{
    public int Int { get; set; } = default!; // 0

    public int? NullableInt { get; set; } = default!; // null

    public string String { get; set; } = default!;
    // データがない場合は自動でstring.Emptyが入ります。

    public string? NullableString { get; set; } = default!;
    // null許容型なので、そのままnullが入ります

    public NullableSimpleClass SimpleClass { get; set; } = default!; // new SimpleClass()

    public NullableSimpleClass? NullableSimpleClass { get; set; } = default!; // null

    public NullableSimpleClass[] Array { get; set; } = default!; // new NullableSimpleClass[0]

    public NullableSimpleClass[]? NullableArray { get; set; } = default!; // null

    public NullableSimpleClass[] Array2 { get; set; } = new NullableSimpleClass[] { new NullableSimpleClass(), null! };
    // null! は新しいインスタンスで置換されます

    public Queue<NullableSimpleClass> Queue { get; set; } = new(new NullableSimpleClass[] { null!, null!, });
    // null! は null のままになります。これはC#のジェネリック関数を介すると、参照型がnull非許容か許容かの情報が失われるためです。仕方ない。
}

[TinyhandObject]
public partial class NullableSimpleClass
{
    [Key(0)]
    public double Double { get; set; }
}

public class NullableTest
{
    public void Test()
    {
        var t = new NullableTestClass();
        var t2 = TinyhandSerializer.Deserialize<NullableTestClass>(TinyhandSerializer.Serialize(t));
    }
}
```



### Default value

`DefaultValueAttribute  ` (`System.ComponentModel`) 属性を付加することで、デフォルト値を設定できます。

クラス再構成の場合や、デシリアライズ時に該当するデータがない場合は、デフォルト値が使用されます。

対象の型は、プリミティブ（`bool`, `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `string`, `char`, `enum`）です。

```csharp
[TinyhandObject(ImplicitKeyAsName = true)]
public partial class DefaultTestClass
{
    [DefaultValue(true)]
    public bool Bool { get; set; }

    [DefaultValue(77)]
    public int Int { get; set; }

    [DefaultValue("test")]
    public string String { get; set; }
    
    [DefaultValue("Test")] // TinyhandObject属性を持つクラスに限りますが、クラスにデフォルト値を指定することが出来ます
    public DefaultTestClassName NameClass { get; set; }
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class StringEmptyClass
{
}

[TinyhandObject]
public partial class DefaultTestClassName
{
    public DefaultTestClassName()
    {
    }

    public void SetDefault(string name)
    {// デフォルト値を設定する場合は、SetDefault() が呼ばれます
        // 順番は Constructor -> SetDefault -> Deserialize or Reconstruct
        this.Name = name;
    }

    public string Name { get; private set; }
}

public class DefaultTest
{
    public void Test()
    {
        var t = new StringEmptyClass();
        var t2 = TinyhandSerializer.Deserialize<DefaultTestClass>(TinyhandSerializer.Serialize(t)); // 空データからのデシリアライズのため、それぞれのデフォルト値が入ります
    }
}
```

メンバーがデフォルト値の場合、シリアライズをスキップすることが可能です。クラス宣言の際に、`[TinyhandObject(SkipSerializingDefaultValue = true)]` と指定してください。



### Reconstruct

デシリアライズ時にメンバーを再構築（自動でインスタンス補完）します。

基本はOnですが、メンバーに `[Reconstruct(false)]` や `[Reconstruct(true)]` 属性を追加することで、挙動を変更できます。

```csharp
[TinyhandObject(ImplicitKeyAsName = true)]
public partial class ReconstructTestClass
{
    [DefaultValue(12)]
    public int Int { get; set; } // 12（デフォルト値）が入ります

    public EmptyClass EmptyClass { get; set; } = default!; // new()

    [Reconstruct(false)]
    public EmptyClass EmptyClassOff { get; set; } = default!; // null：補完されません

    public EmptyClass? EmptyClass2 { get; set; } // null

    [Reconstruct(true)]
    public EmptyClass? EmptyClassOn { get; set; } // new()：補完されます

    /* 補完対象のクラスにはデフォルトコンストラクタが必要になるため、これはエラー
    [IgnoreMember]
    [Reconstruct(true)]
    public ClassWithoutDefaultConstructor WithoutClass { get; set; }*/

    [IgnoreMember]
    [Reconstruct(true)]
    public ClassWithDefaultConstructor WithClass { get; set; } = default!;
}

public class ClassWithoutDefaultConstructor
{
    public string Name = string.Empty;

    public ClassWithoutDefaultConstructor(string name)
    {
        this.Name = name;
    }
}

public class ClassWithDefaultConstructor
{
    public string Name = string.Empty;

    public ClassWithDefaultConstructor(string name)
    {
        this.Name = name;
    }

    public ClassWithDefaultConstructor()
        : this(string.Empty)
    {
    }
}
```

メンバー再構築の挙動をまとめて変更したい場合は、`TinyhandObject` の `ReconstructMember` を変更してください（` [TinyhandObject(ReconstructMember = false)]`）。



### Reuse Instance

デシリアライズ時に、新しいインスタンスを作成せずに、既存のインスタンスを使い回すことが出来ます。

ただし、`TinyhandObject` 属性を持つクラスに限ります（プリミティブ型や配列型に適用すると、取り扱いが訳分からなくなるため）。

メンバーに `[Reuse(true)]` や `[Reuse(false)]` 属性を追加することで、それぞれの挙動を変えることが出来ます。

```csharp
[TinyhandObject(ReuseMember = true)]
public partial class ReuseTestClass
{
    [Key(0)]
    [Reuse(false)]
    public ReuseObject ObjectToCreate { get; set; } = new("create");

    [Key(1)]
    public ReuseObject ObjectToReuse { get; set; } = new("reuse");

    [IgnoreMember]
    public bool Flag { get; set; } = false;
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class ReuseObject
{
    public ReuseObject()
        : this(string.Empty)
    {
    }

    public ReuseObject(string name)
    {
        this.Name = name;
        this.Length = name.Length;
    }

    [IgnoreMember]
    public string Name { get; set; } // Not a serialization target

    public int Length { get; set; }
}

public class ReuseTest
{
    public void Test()
    {
        var t = new ReuseTestClass();
        t.Flag = true;
        // t2.Flag == true
        // t2.ObjectToCreate.Name == "create", t2.ObjectToCreate.Length == 6
        // t2.ObjectToReuse.Name == "reuse", t2.ObjectToReuse.Length == 5

        var t2 = TinyhandSerializer.Deserialize<ReuseTestClass>(TinyhandSerializer.Serialize(t)); // Reuse member
        // t2.Flag == false
        // t2.ObjectToCreate.Name == "", t2.ObjectToCreate.Length == 6 // Note that Name is not a serialization target.
        // t2.ObjectToReuse.Name == "reuse", t2.ObjectToReuse.Length == 5

        t2 = TinyhandSerializer.DeserializeWith<ReuseTestClass>(t, TinyhandSerializer.Serialize(t)); // Reuse ReuseTestClass
        // t2.Flag == true
        // t2.ObjectToCreate.Name == "", t2.ObjectToCreate.Length == 6
        // t2.ObjectToReuse.Name == "reuse", t2.ObjectToReuse.Length == 5
        
        var reader = new Tinyhand.IO.TinyhandReader(TinyhandSerializer.Serialize(t));
        t.Deserialize(ref reader, TinyhandSerializerOptions.Standard); ; // Same as above
    }
}
```

同じくReuseの挙動をまとめて変更したい場合は、`TinyhandObject` の `ReuseMember` を変更してください（` [TinyhandObject(ReuseMember = false)]`）。



### Union

インターフェースや抽象クラスから派生したクラスを、インターフェースや抽象クラス経由でシリアライズ・デシリアライズします。[MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp) や Tinyhandでは `Union` と呼んでいます。

使い方は、まずインターフェース・抽象クラスを定義します。次に、`TinyhandUnion` 属性をそれぞれの派生クラス分だけ追加します。属性は、`[TinyhandUnion(0, typeof(DerivedClassA))]` という感じで、識別子（int）と派生クラスの型を指定します。

```csharp
// インターフェースの宣言
[TinyhandUnion(0, typeof(UnionTestClassA))] // それぞれのTinyhandUnionを登録
[TinyhandUnion(1, typeof(UnionTestClassB))] // Key(int)と派生クラスを指定します
public interface IUnionTestInterface
{
    void Print();
}

[TinyhandObject]
public partial class UnionTestClassA : IUnionTestInterface
{
    [Key(0)]
    public int X { get; set; }

    public void Print() => Console.WriteLine($"A: {this.X.ToString()}");
}

[TinyhandObject]
public partial class UnionTestClassB : IUnionTestInterface
{
    [Key(0)]
    public string Name { get; set; } = default!;

    public virtual void Print() => Console.WriteLine($"B: {this.Name}");
}

public static class UnionTest
{
    public static void Test()
    {
        var classA = new UnionTestClassA() { X = 10, };
        var classB = new UnionTestClassB() { Name = "test", };

        var b = TinyhandSerializer.Serialize((IUnionTestInterface)classA);
        var i = TinyhandSerializer.Deserialize<IUnionTestInterface>(b);
        i?.Print(); // A: 10

        b = TinyhandSerializer.Serialize((IUnionTestInterface)classB);
        i = TinyhandSerializer.Deserialize<IUnionTestInterface>(b);
        i?.Print(); // B: test
    }
}
```



### Text Serialization

バイナリではなく、テキスト形式でシリアライズすることも可能です。

```csharp
// string (UTF-16 text) 形式にシリアライズ
var myClass = new MyClass() { Age = 10, FirstName = "hoge", LastName = "huga", };
var st = TinyhandSerializer.SerializeToString(myClass);
var myClass2 = TinyhandSerializer.DeserializeFromString<MyClass>(st);
```

結果はこちら。JSONに似たノリです。もちろんテキストからデシリアライズも可能です。

```
{
  10, "hoge", "huga", null, null
}
```

UTF-8版はこちら。

```csharp
var utf8 = TinyhandSerializer.SerializeToUtf8(myClass);
var myClass3 = TinyhandSerializer.DeserializeFromUtf8<MyClass>(utf8);
```

結構頑張ったんですが、ObjectをBinaryにしてから（通常はここまで）、Binaryを解釈してTextに変換する、という余計な処理が多いので、遅いです。だいたい5-8倍。

全然使えないほどではないですが、基本はバイナリを勧めます。



### Versioning

バージョニング耐性は結構考慮しています。つまり、メンバー（Key）を追加しても削除しても、可能な限りシリアライズ/デシリアライズするような設計です。

メンバーが追加されて、デシリアライズ時にデータがない場合は、初期値・デフォルト値が使用されます。逆にメンバーが削除されて、デシリアライズ時に余分なデータがある場合は、余分なデータは無視されます。例外は発生しません。

```csharp
[TinyhandObject]
public partial class VersioningClass1
{
    [Key(0)]
    public int Id { get; set; }

    public override string ToString() => $"  Version 1, ID: {this.Id}";
}

[TinyhandObject]
public partial class VersioningClass2
{
    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    [DefaultValue("John")]
    public string Name { get; set; } = default!;

    public override string ToString() => $"  Version 2, ID: {this.Id} Name: {this.Name}";
}

public static class VersioningTest
{
    public static void Test()
    {
        var v1 = new VersioningClass1() { Id = 1, };
        Console.WriteLine("Original Version 1:");
        Console.WriteLine(v1.ToString());// Version 1, ID: 1

        var v12 = TinyhandSerializer.Deserialize<VersioningClass2>(TinyhandSerializer.Serialize(v1))!;
        Console.WriteLine("Serialize v1 and deserialize as v2:");
        Console.WriteLine(v12.ToString());// Version 2, ID: 1 Name: John (Default value is set)

        Console.WriteLine();

        var v2 = new VersioningClass2() { Id = 2, Name = "Fuga", };
        Console.WriteLine("Original Version 2:");
        Console.WriteLine(v2.ToString());// Version 2, ID: 2 Name: Fuga

        var v21 = TinyhandSerializer.Deserialize<VersioningClass1>(TinyhandSerializer.Serialize(v2))!;
        Console.WriteLine("Serialize v2 and deserialize as v1:");
        Console.WriteLine(v21.ToString());// Version 1, ID: 2 (Name ignored)
    }
}
```



### Serialization Callback

シリアライズの前と、デシリアライズの後に処理を挟みたい場合は、 `ITinyhandSerializationCallback` interface を追加してください。

シリアライズ直前に `OnBeforeSerialize`、デシリアライズ直後に `OnAfterDeserialize` が呼ばれます。

```csharp
[TinyhandObject]
public partial class SampleCallback : ITinyhandSerializationCallback
{
    [Key(0)]
    public int Key { get; set; }

    public void OnBeforeSerialize()
    {
        Console.WriteLine("OnBefore");
    }

    public void OnAfterDeserialize()
    {
        Console.WriteLine("OnAfter");
    }
}
```



### Built-in supported types

`Standard` では、`object` 型の値をプリミティブ用 formatter で処理します。`null`、対応するプリミティブ値、列挙型、および対応する値を格納した `System.Collections.ICollection` / `System.Collections.IDictionary` を扱えます。任意の実行時型に応じた formatter の自動選択は行いません。カスタム型は具体的な型または定義済みの Union を使ってシリアライズしてください。

`object` 経由でシリアライズするコレクションは、内部の値にもプリミティブのエンコードを使用します。内部の整数は整数型を保持するため、従来の実行時 formatter 選択と出力バイト列が変わる場合があります。データから復元される整数型は、引き続きエンコードされたバイト列で決まります。

`Security = TinyhandSecurity.UntrustedData`（または `HashCollisionResistant` が有効）の場合、標準のセキュリティ設定ではハッシュコレクションの `object` キーを拒否します。`Dictionary<object, ...>`、`HashSet<object>`、`ILookup<object, ...>`、および `object` 経由で復元するマップが対象です。実際のキーが文字列でも拒否されるため、`Dictionary<string, ...>` など対応する具体的なキー型を指定してください。`object` に格納したスカラー値や、マップを含まない配列は引き続き扱えます。`TrustedData` は通常の比較器とカスタム型・`object` キーの互換性を維持します。

既定の `TrustedData` にも、深度・サイズの上限と decimal 内部表現などの検証が適用されます。従来の無制限設定からの変更のため、上限を超える既存データは例外になります。`UntrustedData` には以下の厳密な入力検証とコレクションの記憶量制限も適用されます。

| 上限 | TrustedData | UntrustedData |
| --- | ---: | ---: |
| オブジェクトの深度 | 256 | 500 |
| 入力サイズ（Stream のバッファリングを含む） | 64 MiB | 16 MiB |
| LZ4展開量の累計 | 128 MiB | 64 MiB |
| 配列要素数／マップの組数 | 2,097,152 | 65,536 |
| 要素数の累計（マップは1組を2要素として計上） | 4,194,304 | 1,048,576 |
| 文字列・識別子ごとのバイト数 | 16 MiB | 16 MiB |
| バイナリ・拡張値ごとのバイト数 | 64 MiB | 16 MiB |
| ペイロードの累計バイト数 | 128 MiB | 64 MiB |
| コレクション要素の累計記憶量 | 無制限 | 64 MiB |

より大きい正常データには `TinyhandDeserializationLimits.TrustedData with { MaximumCollectionLength = 3_000_000 }` のように必要な値を変更し、`TinyhandSecurity.TrustedData.WithLimits(limits)` を指定してください。深度は `WithMaximumObjectGraphDepth` で別途変更できます。`WithLimits(TinyhandDeserializationLimits.Unrestricted)` はサイズ制限のみを解除し、深度制限とネイティブ値の検証は維持します。

`MaximumTotalCollectionBytes` は、要素数とメモリ上の要素サイズの積を、標準・生成コレクションの確保や変更より前に計上します。既存領域の再利用も対象です。マップはキーと値の組、ソート用一時バッファも計上します。バイナリの内容は別のペイロード制限で扱います。

`UntrustedData` は次の検証を既定で有効にします。従来の寛容な動作に依存するデータは拒否される場合があります。

- `ValidateStringValues`：不正UTF-8、失敗した文字列変換、未消費文字の残る変換、不正な列挙名を拒否します。数値形式の列挙値や有効なFlagsは引き続き扱うため、業務上の許可値は別途検証してください。
- `RejectDuplicateKeys`：一意なキーを持つマップの重複と、生成オブジェクトの既知メンバーの重複を拒否します。明示的なmultimapや未知メンバーの読み飛ばしは互換動作を維持します。
- `RejectUnknownUnionTypes`：未知またはnilのUnion判別子を拒否します。Union値全体がnilである場合は引き続き有効です。
- `RequireCompleteInput`：消費バイト数を返さないバッファAPIは、ちょうど1個の値を要求し、LZ4内部の余剰データも拒否します。`DeserializeExact<T>` / `TryDeserializeExact<T>` で同じ動作を明示できます。Reader・消費バイト数付きAPIは連続値の読み取りを維持します。Streamは従来どおり最初の値を読み、シーク可能な場合に未消費分を巻き戻します。1個の圧縮領域には1個の値だけを認めます。
- `CheckAvailableStack`：深度上限に加えて、ネストした値へ入る前に実行時のスタック残量を検査します。
- `SortBeforeInsertion`：配列を内部に持つソート済みコレクションでは、入力を一時領域でソートしてから挿入し、逆順入力による二乗回の要素移動を防ぎます。比較器と同一キー内の順序を維持し、一時領域も予算へ計上します。

各設定には対応する `With...` メソッドがあり、互換性が必要な場合に個別に無効化できます。無効化した項目の保護は失われます。完全入力を要求するAPIはオブジェクト構築前にバイナリ構造を検証しますが、スキーマ検証、setter、コンストラクタ、コールバックはトランザクションではありません。新規の受信用DTOに読み込み、必須項目・業務ルールの検証と認可を完了してから共有状態へ反映してください。シリアライズ結果のハッシュや署名モードのバイト列だけでは、データの真正性は保証されません。

カスタム formatter は読み取り・確保前に `reader.ApplySecurity(options.Security)` を呼び、コレクションの確保・再利用前に `reader.ReserveCollectionStorage<T>(count)`（マップではキーと値の組型）を呼んでください。ネストした値の構築前に `IncrementDepth(ref reader)`、対応する `finally` 内で `reader.Depth--` を実行します。Reader のコピーと SubReader は累積予算を共有します。セキュリティ設定を省略した `new TinyhandReader(span)` には、明示的に適用するまで制限はありません。設定付きで Reader を構築した場合は、`Deserialize(ref reader, options)` にも同じ制限の options を渡してください。使用中の Reader に異なる有限制限を適用すると、予算の置き換えを防ぐため例外になります。カスタム formatter 固有の重複・値の検証は実装側の責任です。

これらはヒープ全体の使用量や実行時間の上限ではありません。コレクションの管理領域、参照先のオブジェクト、コンストラクタ、コールバック、カスタム formatter は追加のメモリを確保するため、スキーマに合わせて上限を設定してください。`TrustedData` はハッシュ衝突攻撃への防御を有効にしません。受信のタイムアウトや同時処理数も呼び出し側で制限してください。Stream のデシリアライズにはキャンセルトークン付きのオーバーロードもあります。組み込みのデシリアライズはスキーマで定義された型を選択しますが、アプリケーション提供のformatterやコールバックはアプリケーションコードを実行するため、信頼できる実装が必要です。

非ジェネリックのコレクション型（`IEnumerable`、`ICollection`、`IList`、`IDictionary`、`ArrayList`、`Hashtable`）はシリアライズ対象の型として指定できません。`IEnumerable<T>`、`ICollection<T>`、`IList<T>`、`IDictionary<TKey, TValue>` などのジェネリックインターフェイス、または対応するジェネリックコレクションの具体型を使用してください。

`System.Type` の値を扱う組み込みformatterはなく、標準のシリアライザー設定では非対応です。

サポートしている型の一覧：

* Primitives (`int`, `string`, etc...), `Enum`s, `Nullable<>`, `Lazy<>`

* `TimeSpan`,  `DateTime`, `DateTimeOffset`

* `Guid`, `Uri`, `Version`, `StringBuilder`

* `BigInteger`, `Complex`

* `Array[]`, `Array[,]`, `Array[,,]`, `Array[,,,]`, `ArraySegment<>`, `BitArray`

* `KeyValuePair<,>`, `Tuple<,...>`, `ValueTuple<,...>`

* `List<>`, `LinkedList<>`, `Queue<>`, `Stack<>`, `HashSet<>`, `ReadOnlyCollection<>`, `SortedList<,>`

* `IList<>`, `ICollection<>`, `IEnumerable<>`, `IReadOnlyCollection<>`, `IReadOnlyList<>`

* `Dictionary<,>`, `IDictionary<,>`, `SortedDictionary<,>`, `ILookup<,>`, `IGrouping<,>`, `ReadOnlyDictionary<,>`, `IReadOnlyDictionary<,>`

* `ObservableCollection<>`, `ReadOnlyObservableCollection<>`

* `ISet<>`,

* `ConcurrentBag<>`, `ConcurrentQueue<>`, `ConcurrentStack<>`, `ConcurrentDictionary<,>`

* Immutable collections (`ImmutableList<>`, etc)

* Custom implementations of `ICollection<>` or `IDictionary<,>` with a parameterless constructor



### LZ4 Compression

LZ4による圧縮も可能です（[MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp)丸パクリだから・・・）

```csharp
var b = TinyhandSerializer.Serialize(myClass, TinyhandSerializerOptions.Lz4);
var myClass2 = TinyhandSerializer.Deserialize<MyClass>(b, TinyhandSerializerOptions.Standard.WithCompression(TinyhandCompression.Lz4)); // Same as TinyhandSerializerOptions.Lz4
```
