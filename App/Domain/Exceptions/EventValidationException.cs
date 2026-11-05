namespace App.Domain.Exceptions;

/// <summary>予定の入力項目に対応する業務上の検証エラー。</summary>
public sealed class EventValidationException(string fieldName, string message) : Exception(message) {
    public string FieldName { get; } = fieldName;
}
