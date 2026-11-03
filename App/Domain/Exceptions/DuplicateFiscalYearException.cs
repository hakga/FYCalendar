namespace App.Domain.Exceptions;

/// <summary>同じ年度が既に登録されていることを表します。</summary>
public sealed class DuplicateFiscalYearException : InvalidOperationException {
    public int Year { get; }

    public DuplicateFiscalYearException(int year, Exception? innerException = null)
        : base($"{year}年度は既に登録されています。", innerException) {
        Year = year;
    }
}
