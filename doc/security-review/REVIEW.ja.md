**追補：UntrustedData の追加対策（2026-10-17）**

以下は現在の作業ツリーに対する追加調査・修正の記録です。後半の2026-09-28の評価は当時のコミットに対する履歴であり、修正後の現在の状態を表すものではありません。

| 観点 | 今回の確認と対策 |
| --- | --- |
| メモリDoS | 件数だけでは大きな値型の配列を制限できないため、`MaximumTotalCollectionBytes` を追加。Untrusted既定64 MiB。標準・生成コレクションで確保・再利用変更前に計上し、Readerのコピー・展開後Readerで共有する |
| CPU DoS | `SortedList`、Arcの`OrderedList` / `OrderedKeyValueList`の逆順入力による二乗の要素移動を、予算付き一時バッファのソート後挿入へ変更。ハッシュ衝突対策と比較器の再利用対策も回帰テストで確認する |
| スタック | Unionの各ラッパーも深度計上。生成DTOの重複・再構築ビットマップは128バイトを超えればヒープへ移す。Untrustedでは深度上限に加えて実行時のスタック残量を検査する |
| 解釈差・整合性 | 不正UTF-8、失敗・部分成功の文字列変換、不正な列挙名、重複する一意マップキー・既知DTOキー、未知・nilのUnion判別子をUntrustedで拒否。明示的なmultimap、未知DTOメンバー、Union全体のnilは互換動作を維持する |
| フレーム境界 | Untrustedの消費バイト数を返さないバッファAPIでは1個の完全な値を要求。`DeserializeExact` / `TryDeserializeExact` を追加。LZ4内外の余剰・構造不正をオブジェクト構築前に検査し、検査用Readerで予算を二重消費しない |
| 不正な固定長値 | Struct128/256とIPEndPointの非nilバイナリ長を検査し、短い入力の既定値化や長い入力の切り捨てを拒否する。decimal、DateTime、Guid、Int128等の既存検証・予算適用も回帰対象 |
| メモリ破壊 | unsafeなReader/LZ4/ネイティブ値の境界を確認。LZ4の32/64bit実装に対し、不正コーパス、出力周辺の破壊検出、入力外パディングを変えた比較、管理コードによる参照デコードの比較を追加。新たな境界外書き込みやRCEに至る欠陥は確認していない |
| 任意コード実行 | 標準object処理、Union分岐、型識別子ディスパッチを確認。入力中の任意型名をロードする経路は確認していない。型識別子は登録済みの型へ限定されるが、その登録は認可を意味しない。独自formatter・コンストラクタ・コールバックは信頼するアプリケーションコードとして扱う |
| 認証・認可 | このライブラリには受信者の認証・業務認可機構がない。重複・不正文字列・余剰データ等によるパーサ間の解釈差を減らしたが、必須項目、許可する数値enum、`MaxLength`による切り詰め、業務ルール、署名/MACの検証は呼び出し側の責任 |

低スタック時の検査には[.NETのEnsureSufficientExecutionStack](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.runtimehelpers.ensuresufficientexecutionstack?view=net-10.0)を使用する。これは通常の関数実行と例外回復のための残量検査であり、任意に大きなstackallocや独自コードの無検査再帰まで保証するものではない。

完全入力の事前検査はバイナリ構造を対象にする。後続の型・業務検証失敗やコールバックの副作用はロールバックしない。共有オブジェクトへ直接復元せず、新規DTOを検証・認可後に反映する。Stream / 消費バイト数付き / ref ReaderのAPIは連続値の用途を維持するため、外側プロトコルのフレーミングを呼び出し側で行う。厳密な入力APIは旧形式のStringトークンもUTF-8として検証するが、生のバイナリReaderの旧形式String-as-binary互換処理は維持する。

記憶量制限は要素の論理サイズとソート一時領域の累計であり、プロセスの総ヒープやRSSではない。コレクション管理領域、参照先オブジェクト、アプリケーション側の確保は別途存在する。受信期限・同時実行数・必要なプロセス隔離は呼び出し側で設定する。LZ4テストは有限のコーパスとガード領域による検証であり、全入力に対するメモリ安全性の証明やネイティブメモリサニタイザーによる検証ではない。

各厳密検証は `TinyhandSecurity` の対応する `With...` で個別に調整できる。既定値と移行方法は [READMEのセキュリティ設定](../../README.md#deserialization-security) および [日本語README](../README.jp.md) を参照。

検証結果：Windows x64 / net10.0 のReleaseビルド成功。`dotnet XUnitTest/bin/Release/net10.0/XUnitTest.dll --progress off --output Minimal` で全841件成功、失敗・スキップ0件。`git diff --check` も成功。NuGet監査フィードへ接続できない `NU1900` 警告が残るため、この実行では依存パッケージの最新脆弱性情報を確認できていない。

---

**Tinyhand: 信頼できないバイナリのデシリアライズ評価（2026-09-28）**

対象はコミット `d32b9f376ae5bbcf43c2ab0680841fd144abc834`、リポジトリのバージョン表記 `0.148.2`、.NET 10.0.12 / Windows x64。公開NuGetパッケージ全般についての断定ではない。外部から任意のバイト列を送れる攻撃者を想定し、通常のバイナリDeserialize、Reader、主要な標準・生成フォーマッタ、LZ4、型識別子の処理を確認した。

**結論：標準設定のままネットワークに公開することは推奨しない。`UntrustedData` の指定だけでも不十分である。** 特にハッシュ衝突対策の実装に再現可能な問題があり、正規の小さな入力でCPU消費を増大させられる。メモリ・展開後サイズ・実行時間の総量制限も別途必要になる。

アプリケーション固有のDTO、カスタムフォーマッタ、受信処理は提示されていない。そのため、それらを含むエンドポイントの実際の到達性・負荷・業務影響は未判定。RCEや境界外メモリアクセスは今回確認していないが、網羅的なファジングや全依存ライブラリの監査を行ったわけではない。

**検証結果の要点**

| 項目 | 評価 | 確認結果 |
|---|---|---|
| UntrustedDataの数値ハッシュ | 高・実装上の問題 | 異なるlong 4,096個が1〜2種類のハッシュへ集中。doubleも再現 |
| BCL辞書の既存インスタンス再利用 | 高・実装上の問題 | 安全用の比較器を取得しても適用しない。新規DTOでも発生 |
| Arc UnorderedMap / UnorderedSet | 高・実装上の問題 | 比較器ポリシーを適用せず、objectキーも受理 |
| コレクション・オブジェクトのメモリ総量 | 高・制限不足 | 8,512バイトの不正入力で、拒否前に4,213,272バイトを割り当て |
| LZ4展開の総量 | 高・有効化時の制限不足 | 4,139バイトから1,048,576バイトのbyte配列を生成 |
| 既定の深度・ハッシュ設定 | 高・設定上の問題 | TrustedData、深度int.MaxValue、ハッシュ防御なし |
| 生のStream入力 | 高・利用方法依存 | EOFまで全読み込み。サイズ上限・CancellationTokenなし |
| 不正decimal内部表現 | 中・実装上の問題 | scale=255の値を受理。通常のdecimalコンストラクタは拒否 |
| 余剰データ・重複キー・不正UTF-8 | 中・利用方法依存 | 受理や解釈差を確認。厳密なプロトコルでは追加検証が必要 |
| 失敗時の既存状態・コールバック | 中〜高・利用方法依存 | TryDeserializeObjectがfalseでも、他参照から見える元オブジェクトは変更済み |

評価の「高」はインターネットから繰り返し到達できる場合の可用性リスクを示す。公式のCVSSや採番済みCVEを意味しない。

**1. UntrustedData自体のハッシュ衝突対策が成立していない**

[TinyhandSecurity.cs](../../Tinyhand/Tinyhand/TinyhandSecurity.cs) の `Int64EqualityComparer`（237行）、`UInt64EqualityComparer`（230行）、`DoubleEqualityComparer`（281行）は上位・下位32ビットを `HashCode.Combine` に渡す。これは通常入力向けのハッシュであり、敵対的入力に対する防御ではない。

Microsoft自身がseedを知らずに衝突群を作れる構成を公開している。今回、その構成をTinyhandの比較器と実際のDeserializeに適用して確認した。[.NETのHashCodeセキュリティ設計文書](https://github.com/dotnet/runtime/blob/main/docs/design/security/System.HashCode.md)

最終保存した実行では、long 4,096個のうち3,977個が同じハッシュ値となった。別プロセスでの先行実行では4,096個すべてが衝突した。seedによってグループの分かれ方は変わるが、攻撃用入力をseedに合わせて作り直す必要はない。doubleでは4,095個の異なる値のうち3,975個が同じハッシュとなった。

| Dictionary<long,int>の件数 | 入力バイト数 | Deserialize平均時間 |
|---:|---:|---:|
| 1,024 | 10,235 | 1.716 ms |
| 2,048 | 20,475 | 7.468 ms |
| 4,096 | 40,955 | 27.235 ms |

ウォームアップ後3回の平均で、精密なベンチマークではない。しかし衝突の成立はハッシュ値で直接確認でき、時間も概ね二乗増加と整合する。HashSet、ImmutableDictionary等、同じ比較器を使うコレクションにも問題が波及する。

DateTime / DateTimeOffsetも2引数のHashCode.Combine、GuidもHashCodeを使用するため設計の見直し対象。ただし今回保存した実行で具体的な衝突群を検証したのはlongとdoubleである。文字列の比較器は.NETのstring.GetHashCodeを使っており、今回の数値の衝突構成をそのまま適用できるという意味ではない。

MessagePack-CSharpには同種の問題に対する2024年のアドバイザリがある。これはTinyhandにそのCVEが公式に適用されるという主張ではなく、修正設計の参考である。[GHSA-4qm4-8hg2-g2xm](https://github.com/MessagePack-CSharp/MessagePack-CSharp/security/advisories/GHSA-4qm4-8hg2-g2xm)

対策：プロセスごとの秘密鍵を持つSipHash等、敵対的入力向けに設計されたハッシュへ置換する。値の全情報を正規化して入力し、先にGetHashCodeで32ビットへ潰さない。NaNと±0、DateTimeのKind、DateTimeOffsetのUTC同値性など、Equalsとハッシュの整合性を維持する。修正までの受信DTOでは、これらの数値をキーにするハッシュコレクションを避け、件数制限を課す。

**2. 比較器ポリシーを適用しない経路がある**

[DictionaryFormatter.cs](../../Tinyhand/Formatters/DictionaryFormatter.cs) の233〜242行では、`options.Security.GetEqualityComparer<TKey>()` を呼んだ後、既存インスタンスがあればClearしてそのまま返す。安全用の比較器は新規生成時にしか渡されない。同様の処理がIDictionary、ConcurrentDictionary、汎用Dictionaryフォーマッタにもある。

objectキーの拒否は再利用時にも動作するが、サポート済みキーの既存比較器を安全にする処理にはなっていない。標準long.GetHashCodeでは、上位32ビットと下位32ビットを等しくした異なるlongがすべて同じ値となる。

これは明示的なrefデシリアライズだけの問題ではない。`[Key(0)] public Dictionary<long,int> Values { get; set; } = new();` を持つ新規DTOを `Deserialize<Dto>(..., UntrustedData)` で生成しても、初期化済みの辞書が再利用され、デフォルト比較器が残った。1,024キーすべての衝突を確認した。

`[TinyhandObject(ReuseMembers = false)]` でも同じ結果になった。[TinyhandObject.cs](../../TinyhandGenerator/TinyhandObject.cs) の3901〜3903行ではFormatterCoderの経路で `assignment.RefValue(true)` を使用しており、この属性だけでは解決しない。

[ArcCollectionFormatters.cs](../../Tinyhand/Formatters/ArcCollectionFormatters.cs) の `UnorderedMapFormatter`（428〜450行）、`UnorderedSetFormatter`（519〜540行）ではGetEqualityComparer自体を呼ばず、new()または既存インスタンスを使う。UntrustedDataでもlongの1,024キーが衝突し、UnorderedMap<object,int>も受理された。

補足：`Utf16Hashtable<T>` のフォーマッタもTinyhandSecurityの比較器を利用しない。参照中のArc.Collections 2.1.0のソースコミット `4c4150a94f2b3505a9f40d13d91dae1fd3a8266c` では、`Arc.Collections/Hashtable/Utf16Hashtable.cs` のGetHashCodeはXxHash3Slim.Hash64を32ビットに切り詰めている。これは数値と同じ再現実験をしたという意味ではないが、固定ハッシュによる事前計算可能なバケット集中も別途評価すべきである。

対策：UntrustedDataでは、安全性を保証できない既存比較器を持つ辞書を再利用せず、安全な比較器で作り直す。比較器に独自の同値性がある場合は、黙って意味を変更せず、明示的な対応または拒否が必要。Arcのハッシュコレクションにも同じ規約を適用し、対応できない型はUntrustedDataで拒否する。TinyhandSecurityのハッシュ実装だけを直しても、この迂回経路は残る。

**3. 長さの境界検査はあるが、メモリ予算ではない**

[TinyhandReader.cs](../../Tinyhand/IO/TinyhandReader.cs) の366行以降では配列数をRemaining以下、mapの組数をRemaining/2以下に制限している。したがって、数バイトのarray32ヘッダだけで任意に巨大な配列を確保するという単純な攻撃は防がれている。

一方、`[count=8192, [count=8192, ...]]` のような64段のヘッダと8,192バイトのnilを組み合わせると、各段の最小バイト数チェックを通ってしまう。[PrimitiveObjectFormatter.cs](../../Tinyhand/Formatters/PrimitiveObjectFormatter.cs) の277行は実際の子要素の妥当性を確定する前に `new object[length]` を実行する。深度上限64、入力8,512バイトで、失敗を返すまでに4,213,272バイトを割り当てた。これは割り当て総量の計測であり、プロセスのピークRSSの計測ではない。

正常なデータでも、値型の大きな配列、空オブジェクト多数、文字列、DTOコンストラクタの初期化により、入力サイズからヒープ使用量が大きく増える。深度上限だけでは幅や総ノード数を制限できない。

`[MaxLength(3)]` も確保前の検証ではない。1万文字を入力すると、結果は3文字に切られるが、割り当ては20,080バイトあった。[TinyhandObject.cs](../../TinyhandGenerator/TinyhandObject.cs) の3001行付近と [JournalShared.cs](../../TinyhandGenerator/GeneratorShared/JournalShared.cs) はsetter側で切り詰める処理を生成する。切り詰めは認証用ID等の曖昧性にも注意が必要。

対策：受信サイズとは別に、コレクション件数、個別文字列・binary長、総ノード数、概算総確保バイト数を確保前に制限する。Reader/SubReader間で予算を共有し、再帰の各段で同じRemainingを重複した予算として使わない。スキーマに基づく事前検証、または制限対応のフォーマッタを用意する。確保後のDTO検証は業務ルールには必要だが、メモリDoSの予防としては遅い。

**4. 圧縮とStreamには別の予算が必要**

[TinyhandSerializer.cs](../../Tinyhand/Tinyhand/TinyhandSerializer.cs) の902行以降のLZ4処理は、メタデータ長、負の長さ、255倍を超える申告値、Array.MaxLength超過を検査する。実装には有益な防御があるが、アプリケーション向けの小さい展開後上限はない。

正常なLZ4データ4,139バイトから、1,048,576バイトのbyte[]を生成できた。展開バッファのほかに、生成オブジェクトの確保も必要になる。さらに、内容の不正がデコーダで判明する場合でも、申告長が前段の検査を通れば981行付近のデコード前にバッファを確保する。

831行のDeserialize(Stream)、874行のDeserializeAsync(Stream)は一般のStreamをEOFまで読み切ってからパースする。生のNetworkStream等を渡すと、無制限送信によるメモリ消費や、EOFを送らない接続による待機が成立する。これらのAPIにはCancellationToken引数がない。

対策：外側のプロトコルでフレーム長を上限付きで検査し、期限とキャンセル付きでReadExactlyAsync相当の受信を完了してから、限定されたSpanを渡す。圧縮は不要なら使わない。有効にする場合は圧縮サイズ、展開サイズ、ブロック数、展開率、生成後予算のすべてを制限する。Task.WaitAsync等で呼び出し側の待機だけを打ち切っても、同期DeserializeのCPU処理は停止しない。強い実行時間・メモリ隔離が必要なら、制限付きワーカープロセスを用いる。

**5. 深度制限は必須だが、既定で有効にならない**

[TinyhandSerializerOptions.cs](../../Tinyhand/Tinyhand/TinyhandSerializerOptions.cs) の135行はTrustedDataが既定。[TinyhandSecurity.cs](../../Tinyhand/Tinyhand/TinyhandSecurity.cs) の75行は深度int.MaxValue。UntrustedDataは500にするが、アプリケーションの実際のDTOに合わせてさらに低くすることが望ましい。

標準のobject配列、コレクション、生成DTOにはIncrementDepthがあり、今回100段の配列はTrustedDataで受理、深度64設定では拒否された。既定設定でのプロセスクラッシュ試験は行っていない。ただし無制限の実再帰が可能な型について、スタック枯渇の経路は存在する。通常のStackOverflowExceptionはtry/catchで復旧できず、既定でプロセスが終了する。[Microsoftの説明](https://learn.microsoft.com/en-us/dotnet/api/system.stackoverflowexception)

未知フィールドのSkipは現在反復処理になっており、10万段をスタック再帰なしにスキップできた。ここで深度を検査しないことを、そのままスタックオーバーフロー脆弱性とは評価しない。カスタムフォーマッタや手書きのDeserializeは別途、深度・サイズ予算を守る必要がある。

対策：全受信経路で明示的にUntrustedDataと必要な深度を渡す。32〜64は検討開始値であって、すべての型・実行環境で安全を保証する値ではない。引数なしDeserializeObject等には個別options引数がないオーバーロードもあるため、ref Reader+optionsのAPIを使うか、DefaultOptionsを設定する責任を一か所に集約する。過程でoptionsをTrustedDataに戻す呼び出しがないかも確認する。

**6. 不正decimalを生成できる**

[BuiltinResolver.cs](../../Tinyhand/Resolvers/BuiltinResolver.cs) の91行は標準decimalにNativeDecimalFormatterを登録する。[UnsafeBinaryFormatters.cs](../../Tinyhand/Formatters/UnsafeBinaryFormatters.cs) の85〜98行は16バイトという長さだけを検査し、そのままdecimalの内部表現へコピーする。

18バイトの入力でflags=`0x00FF0000`（scale=255）のdecimalが得られた。同じGetBits結果から通常のdecimalコンストラクタで再生成するとArgumentExceptionとなる。正規のscaleは0〜28で、予約ビットも制約される。[Decimalコンストラクタ仕様](https://learn.microsoft.com/en-us/dotnet/api/system.decimal.-ctor?view=net-10.0)

これは値型の不変条件を破る入力検証不足。演算・整形・比較時の追加例外や誤処理を懸念するが、今回RCEやメモリ破壊までは確認していない。

対策：コピー先へ確定する前にflagsの予約ビットとscaleを検証するか、正規のdecimalコンストラクタを通して値を作る。互換性のため生のワイヤ形式を残す場合も内部表現の検証は必要。修正まで、受信DTOでdecimalを避けるか、正規性を検査する専用フォーマッタを使う。

**7. 厳密な受信プロトコルとしては追加の検証が必要**

- 余剰バイト：`[42,99]` をintとして読むと42を返し、1バイトしか消費しない。これは連続オブジェクト用APIとしては妥当だが、「1フレーム=1オブジェクト」ではbytesReadとの一致検査が必要。
- 圧縮内の余剰バイト：LZ4の内側に42,99を入れると、外側10バイトをすべて消費したうえで42を返す。`bytesRead == frame.Length` だけでは内部の余剰を検出できない。展開後SubReaderのEndも確認するStrict APIが必要。
- 重複mapキー：Dictionary<string,int>はTryAddで最初の値を保持し、文字列キーの生成DTOは後の値で上書きする。同じ`x=1,x=2`が1と2に分かれた。別パーサによる事前認可・署名検証と組み合わせると解釈差が問題になる。厳密な受信では重複キーを拒否する。
- 不正UTF-8：`a1 ff` は拒否されずU+FFFDへ置換される。識別子、署名対象、別言語実装との相互運用では正規化の不一致を避けるため、厳密なUTF-8検証を行う。[TinyhandReader.cs](../../Tinyhand/IO/TinyhandReader.cs) の1011行を参照。
- ref再利用の失敗：PairPayloadの1項目目を9へ変更後、2項目目の不正コードで失敗させると、TryDeserializeObjectはfalseを返してrefをnullにするが、保持していた別参照ではFirst=9になっていた。処理はトランザクションではない。
- 未指定・nil・未知Union：既存値の維持、デフォルト値や再構築、読み飛ばしがあり、スキーマ互換性には役立つが「必須項目が送信された」保証ではない。必要な項目・許可するUnion・範囲・enum値を受信プロトコルとして検証する。

対策：受信用の新規DTOに読み込み、全体の検証・認可が成功してから業務オブジェクトへ反映する。共有中のエンティティに直接読み込まない。重複・欠落・余剰のルールを署名/認可側と統一する。なお、新規DTO化は失敗時の状態汚染には効くが、2の初期化済み辞書の問題は別途修正が必要。

**8. RCE・任意型生成に関する評価**

標準のobjectフォーマッタはMessagePackの基本型・配列・map等を扱い、入力の文字列をType.GetType/Assembly.Loadへ渡す経路は今回確認しなかった。Unionも生成時に定義した分岐で選択する。したがって、BinaryFormatter型の「任意の型名を入力してガジェットを起動する」方式が標準Deserialize<T>にそのまま存在するとは評価しない。

一方、生成コードはコンストラクタ、setter、TinyhandOnDeserializing / TinyhandOnDeserialized、カスタムフォーマッタを実行する。[TinyhandObject.cs](../../TinyhandGenerator/TinyhandObject.cs) の2677行付近、4231行、4241行を参照。UseServiceProviderも含め、受信DTOから到達するコードは信頼境界内で実行される。そこでファイル、プロセス、SQL、ネットワーク等を操作すれば、アプリケーション側の入力依存処理が攻撃対象になる。

[TinyhandTypeIdentifier.cs](../../Tinyhand/Tinyhand/TinyhandTypeIdentifier.cs) のTryDeserialize(uint, ...)は登録済みの型へ振り分ける。未登録型を任意ロードする処理ではなく、登録時のID衝突検査もある。しかし「登録済み」は「このエンドポイントで受信を許可した」と同義ではない。外部から型IDを受け取る場合は、認可前にエンドポイントごとの許可リストで絞る。型IDのFarmHashは認証・完全性保証にはならない。

**推奨する実施順序**

1. 受信経路の全optionsをUntrustedData+必要な深度へ統一し、フレーム長、受信期限、同時実行数、接続/送信者ごとのレートを制限する。生の無制限Streamを渡さない。圧縮は必要性を確認し、無効化できるなら無効化する。
2. 数値ハッシュを修正し、既存辞書・Arcコレクションの比較器迂回を併せて修正する。修正完了までは、外部DTOの該当コレクションを制限・置換する。単にHashCollisionResistant=trueを確認して安心しない。
3. コレクション・文字列・binary・展開後サイズ・総生成量に確保前の予算を導入する。現在のMaxLength属性をこの目的に流用しない。
4. decimalの正規性、重複キー、余剰データ、厳密UTF-8、必須項目・範囲・Unionの検査を追加する。これらの拒否は互換性に影響するためStrict受信用設定として設計してよい。
5. DTOの副作用を排除し、検証済みデータだけを業務状態へ反映する。TLSは通信路を保護するもので、相手の入力が安全になるわけではない。必要な認証・MAC等は、大きな展開やオブジェクト生成より前に検証する。認証済み利用者にもサイズ・負荷制限を適用する。
6. 今回の衝突、再利用、Arc、深い/広い入力、LZ4内部余剰、不正decimalを回帰試験へ追加する。失敗を例外にするだけでなく、拒否までの割り当て量・作業量の上限も確認する。unsafeなLZ4/Readerは別プロセスでファジングし、32/64ビット実装を継続検証する。

最小限の呼び出し形は次のようになる。ただし、これはフレームサイズ・深度・外側の余剰バイトの対策例であり、上記のハッシュ脆弱性と総確保量の問題を解消するものではない。RequestとValidateRequestはアプリケーション固有のDTOと検証処理を示す。

```csharp
static readonly TinyhandSerializerOptions NetworkOptions =
    TinyhandSerializerOptions.Standard with
    {
        Security = TinyhandSecurity.UntrustedData
            .WithMaximumObjectGraphDepth(32),
    };

static Request ReadFrame(ReadOnlySpan<byte> frame)
{
    if (frame.IsEmpty || frame.Length > 64 * 1024)
        throw new InvalidDataException("Invalid frame size.");

    var value = TinyhandSerializer.Deserialize<Request>(
        frame, out var consumed, NetworkOptions)
        ?? throw new InvalidDataException("Request is required.");

    if (consumed != frame.Length)
        throw new InvalidDataException("Trailing data.");

    ValidateRequest(value);
    return value;
}
```

32段・64KiBは例であり、プロトコルごとに決める。配列件数などはこの呼び出しより前、またはフォーマッタが確保する直前に制限する。TryDeserializeへの変更やcatchの追加では、既に費やしたCPU/メモリやStackOverflowからの復旧は保証されない。

**検証資料と限界**

- [Program.cs](Program.cs)：破壊的なOOM/StackOverflow試験を避けた小規模プローブ。`dotnet run --project doc/security-review/SecurityProbe.csproj -c Release` で再実行可能。ライブラリ本体は変更していない。
- [probe-results.txt](probe-results.txt)：最終実行の生ログ。ランダムseed・JIT・負荷によってハッシュのグループ分割や時間・割り当て値は変動する。プローブは現象の観察用であり、将来の安全な挙動を定義する回帰テストではない。
- 既存テスト79件が成功。TinyhandSecurityTest、IoBoundaryTest、Lz4BoundaryTest、MultiDimensionalArrayTestで43件、GeneratedCollectionOptimizationTest、FormatterRegressionTest、ReaderWriterTest、CollectionReuseTest、UnionTest群、MaxLengthTestで36件。
- Readerの負長・残量検査、多次元配列の次元積検査、LZ4の切り詰め入力/ゼロoffset等の既存防御を確認した。過去の問題を現実装に残っているものとして列挙してはいない。
- ネットワーク経由の負荷試験、プロセスクラッシュ試験、全フォーマッタの網羅的ファジング、ユーザーの実サービスへの到達性確認は実施していない。
