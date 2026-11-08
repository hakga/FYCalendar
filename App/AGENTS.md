# アプリケーションのコード配置

- 今後追加する `.cs` ファイルは、`Controllers`、`Infrastructures`、`Models` に収めるものを除き、`Domain` 以下の役割に応じたフォルダーに配置する。
- 名前空間はフォルダー構成に合わせる。例：`Domain/Services/FiscalYearCalculator.cs` は `App.Domain.Services`。
- 年度・利用者の管理機能は `MaintainController` にまとめる。
- カレンダーと予定の画面は `CalendarsController` にまとめる。
- C#の開き波括弧 `{` は、クラス・メソッド・制御文などの宣言と同じ行に置く。
