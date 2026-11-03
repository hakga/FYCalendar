using App.Data;
using App.Data.Entities;
using App.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace App.Domain.Services;

/// <summary>事業年度の登録と取得を行います。</summary>
public sealed class FiscalYearService {
    private const int MaxNameLength = 50;
    private readonly AppDbContext _context;

    public FiscalYearService(AppDbContext context) {
        _context = context;
    }

    /// <summary>年度から期間を計算し、状態openで登録します。名称の前後の空白は除去します。</summary>
    /// <exception cref="ArgumentOutOfRangeException">年度が1～9998の範囲外です。</exception>
    /// <exception cref="ArgumentException">名称が空、または前後の空白除去後に50文字を超えています。</exception>
    /// <exception cref="DuplicateFiscalYearException">同じ年度が既に登録されています。</exception>
    public async Task<FiscalYear> CreateAsync(
        int year, string name, CancellationToken cancellationToken = default) {
        var period = FiscalYearCalculator.GetPeriod(year);
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("名称を入力してください。", nameof(name));
        }

        var normalizedName = name.Trim();
        if (normalizedName.Length > MaxNameLength) {
            throw new ArgumentException($"名称は{MaxNameLength}文字以内で入力してください。", nameof(name));
        }

        if (await _context.FiscalYears.AnyAsync(item => item.Year == year, cancellationToken)) {
            throw new DuplicateFiscalYearException(year);
        }

        var fiscalYear = new FiscalYear {
            Year = year,
            Name = normalizedName,
            StartsOn = period.StartsOn,
            EndsOn = period.EndsOn,
            Status = "open"
        };
        _context.FiscalYears.Add(fiscalYear);

        try {
            await _context.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateException exception) {
            // 失敗した追加を残さず、同じスコープで次の登録を行えるようにする。
            _context.Entry(fiscalYear).State = EntityState.Detached;

            // 事前チェック後に別の処理が登録した場合を確認する。
            // 年度が存在しなければ、重複以外のDBエラーとして元の例外を伝える。
            if (await _context.FiscalYears.AnyAsync(item => item.Year == year, cancellationToken)) {
                throw new DuplicateFiscalYearException(year, exception);
            }

            throw;
        }

        return fiscalYear;
    }

    /// <summary>登録済み年度を新しい年度から順に取得します。</summary>
    public async Task<IReadOnlyList<FiscalYear>> GetAllAsync(CancellationToken cancellationToken = default) {
        return await _context.FiscalYears
            .AsNoTracking()
            .OrderByDescending(item => item.Year)
            .ToListAsync(cancellationToken);
    }

    /// <summary>指定年度を取得します。未登録の場合はnullを返します。</summary>
    /// <exception cref="ArgumentOutOfRangeException">年度が1～9998の範囲外です。</exception>
    public async Task<FiscalYear?> FindByYearAsync(int year, CancellationToken cancellationToken = default) {
        _ = FiscalYearCalculator.GetPeriod(year);
        return await _context.FiscalYears
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Year == year, cancellationToken);
    }
}
