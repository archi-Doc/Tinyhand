# NativeAOT

Tinyhand の formatter と型識別子のアダプターは、コンパイル時に型引数を確定させて登録します。実行時の `MakeGenericType`、式ツリーのコンパイル、リフレクションによるコンストラクター呼び出しは使用しません。

## アプリケーションの設定

.NET 10 / C# 14 以降を使用し、アプリケーションのプロジェクトに次を指定します。

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

NuGet パッケージには Source Generator が含まれます。リポジトリのプロジェクトを直接参照する場合は、`NativeAotTest.csproj` と同様に `TinyhandGenerator` を Analyzer として参照してください。

```sh
dotnet publish MyApplication.csproj -c Release -r win-x64
# Linux 上では -r linux-x64 を使用
```

各 OS のネイティブコンパイラーが必要です。前提条件は [.NET Native AOT の公式ドキュメント](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)を参照してください。生成された登録コードは ModuleInitializer から呼び出されるため、.NET NativeAOT で初期化メソッドを手動で呼ぶ必要はありません。

## 登録対象

Source Generator はソースコード中の閉じた型、シリアライズ API の型引数、モデルのメンバー、Union の派生型を調べ、必要なコレクション・列挙型・モデルの formatter を登録します。同一コンパイル内のジェネリックな補助メソッドも、呼び出し側で確定した型引数を追跡します。

別アセンブリのジェネリックな処理内部でのみ使う型など、呼び出し側のソースから特定できない閉じた型は明示してください。

```csharp
using Tinyhand;
using System.Collections.Generic;

[assembly: TinyhandRegister(typeof(Dictionary<string, Envelope<int>>))]

[TinyhandObject]
public partial class Envelope<T>
{
    [Key(0)] public T Value { get; set; } = default!;
}
```

`TinyhandRegister` はアセンブリに複数指定できます。`typeof(Envelope<>)` のように型引数が未指定の型は登録できません。実行時に初めて決まる任意の型を動的に生成するフォールバックはありません。独自 formatter は、型引数を確定させて `GeneratedResolver.Instance.SetFormatter<T>(formatter)` で登録します。組み込み formatter の置き換えにはカスタム resolver を使用してください。

private な型を登録するために、その型を参照できる包含型へ登録メソッドを生成する場合があります。包含型はすべて `partial` にしてください。

| 診断 | 意味 |
| --- | --- |
| THAOT001 | 登録が必要な型にアクセスできないため、包含型を `partial` にする必要がある |
| THAOT002 | 型の入れ子が 64 段、または展開後の型要素数が 4096 を超えた。型引数が再帰的に増えるモデルや補助メソッドを確認する |
| THAOT003 | `TinyhandRegister` に開いたジェネリック型を指定している |

## API の変更

- `GenericsResolver` と動的 formatter ファクトリーを削除しました。
- `TinyhandTypeIdentifier.Register(Type)` / `Register(ReadOnlySpan<Type>)` の代わりに `Register<T>()` を使用します。生成された formatter と組み込み型は自動登録されます。
- 独自の `IStringConvertible<T>` パーサーを使う手動登録型は、制約を満たす閉じた型で `TinyhandTypeIdentifier.RegisterStringConvertible<T>()` を呼びます。
- カスタム辞書には、コンパイル時に選択したコンストラクターを呼ぶ static ファクトリーを渡します。公開された `(int capacity, IEqualityComparer<TKey> comparer)` があれば優先し、なければ公開された引数なしコンストラクターを使用します。後者では、その辞書型自身が比較子を決定します。
- `UntrustedData` での `object` キーの禁止、および非ジェネリック型・ExpandoObject・System.Type のサポート廃止は継続します。

## Processor とプラグイン

プラグイン DLL を実行時に検索・ロードする処理を削除しました。プラグインのプロジェクトを参照し、処理開始前に登録してください。

```csharp
TinyhandProcess.RegisterPlugin<MyProcessCore>("my process");
// TestPlugin の例: TinyhandProcessCore_Test.Register();
```

`TinyhandProcessor` は使用する DI サービスと logger を閉じた型で登録します。現在の依存パッケージ `Arc.Unit 0.45.0` の登録ヘルパーにはトリミング注釈が不足しており、発行時に IL2091 / IL2067 の警告が計 5 件残ります。使用するコンストラクターを明示的に保持し、コンソール・ファイル・両方・出力なしの各モード、および静的プラグインを Windows x64 の NativeAOT で検証しています。警告は抑制していません。

## 検証と最適化

```sh
dotnet test --project XUnitTest/XUnitTest.csproj
dotnet publish NativeAotTest/NativeAotTest.csproj -c Release -r win-x64
dotnet publish NativeAotProcessorTest/NativeAotProcessorTest.csproj -c Release -r win-x64
```

発行先の `NativeAotTest.exe` と `NativeAotProcessorTest.exe` を実行します。これらは JIT で実行すると失敗するため、`dotnet run` は使用しません。CI に Windows / Linux の発行・実行を追加しています。

`NativeAotTest` は Tinyhand アセンブリ全体をトリミング検証のルートとし、警告をエラー扱いにします。バイナリ・テキスト・LZ4・UntrustedData・閉じたジェネリックモデル・Union・private 型・型識別子・辞書ファクトリーを検証します。

formatter の取得は型ごとの静的キャッシュを使用し、型識別子の呼び出しは型ごとに一つの静的アダプターで処理します。ジェネリックな型識別子 API のボックス化、未登録識別子のキャッシュ追加、辞書生成のリフレクション引数配列をなくしました。`Memory<T>` / `ReadOnlyMemory<T>` / `ReadOnlySequence<T>` / `ArraySegment<T>` の複製では、中間配列を作らず結果配列へ複製します。回帰テストで通常の配列複製と同じアロケーション量になること、およびカスタム要素 formatter による深い複製が維持されることを確認しています。
