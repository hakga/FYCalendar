using System.ComponentModel.DataAnnotations;
using App.Data;
using App.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.Domain.Services;

/// <summary>スケジュールを利用する個人の登録と取得を行います。</summary>
public sealed class UserService(AppDbContext context) {
    public const string DefaultTimeZoneId = "Asia/Tokyo";

    public async Task<User> CreateAsync(
        string name, string? email = null, string timeZoneId = DefaultTimeZoneId,
        CancellationToken cancellationToken = default) {
        var normalizedName = name?.Trim();
        if (string.IsNullOrEmpty(normalizedName) || normalizedName.Length > 100) {
            throw new ArgumentException("氏名を入力し、100文字以内で指定してください。", nameof(name));
        }

        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        if (normalizedEmail is not null &&
            (normalizedEmail.Length > 255 || !new EmailAddressAttribute().IsValid(normalizedEmail))) {
            throw new ArgumentException("メールアドレスは正しい形式で255文字以内で入力してください。", nameof(email));
        }

        var normalizedTimeZoneId = timeZoneId?.Trim();
        if (string.IsNullOrEmpty(normalizedTimeZoneId) || normalizedTimeZoneId.Length > 64) {
            throw new ArgumentException("有効なタイムゾーンを選択してください。", nameof(timeZoneId));
        }
        try {
            _ = TimeZoneInfo.FindSystemTimeZoneById(normalizedTimeZoneId);
        } catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException) {
            throw new ArgumentException("有効なタイムゾーンを選択してください。", nameof(timeZoneId), exception);
        }

        var user = new User {
            Name = normalizedName,
            Email = normalizedEmail,
            TimeZoneId = normalizedTimeZoneId
        };
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        return user;
    }

    /// <summary>登録の新しい順に利用者を取得します。</summary>
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Users.AsNoTracking().OrderByDescending(user => user.Id).ToListAsync(cancellationToken);
}
