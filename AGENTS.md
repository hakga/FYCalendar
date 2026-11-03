# テストの命名

- `App.Tests` と `App.E2ETests` のテストクラス名は、対象との対応が分かる英語名にする（例：`FiscalYearCalculatorTests`）。
- `[Fact]`・`[Theory]` のテストメソッド名は、条件と期待結果が伝わる簡潔な日本語にする（例：`同じ年度は重複登録できない`）。
- `Category` などのTrait名・値は英語で統一し、`Unit`・`Integration`・`E2E` を使用する。
- 補助メソッドは通常の英語名とし、`InitializeAsync`・`DisposeAsync` などインターフェースで定められた名前を維持する。
- C#の開き波括弧 `{` は、クラス・メソッド・制御文などの宣言と同じ行に置く。
