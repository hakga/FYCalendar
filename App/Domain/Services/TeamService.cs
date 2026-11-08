using App.Data;
using App.Data.Entities;
using App.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace App.Domain.Services;

/// <summary>チームと所属するメンバーの登録・取得を行います。</summary>
public sealed class TeamService(AppDbContext context) {
    public const string DefaultTimeZoneId = UserService.DefaultTimeZoneId;

    public async Task<Team> CreateAsync(
        string name, string timeZoneId = DefaultTimeZoneId, CancellationToken cancellationToken = default) {
        var normalizedName = name?.Trim();
        if (string.IsNullOrEmpty(normalizedName) || normalizedName.Length > 200) {
            throw new ArgumentException("チーム名を入力し、200文字以内で指定してください。", nameof(name));
        }
        var team = new Team {
            Name = normalizedName,
            TimeZoneId = CalendarValidation.NormalizeTimeZoneId(timeZoneId)
        };
        context.Teams.Add(team);
        await context.SaveChangesAsync(cancellationToken);
        return team;
    }

    public async Task<IReadOnlyList<Team>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Teams.AsNoTracking().OrderByDescending(team => team.Id).ToListAsync(cancellationToken);

    public Task<Team?> FindAsync(long teamId, CancellationToken cancellationToken = default) =>
        context.Teams.AsNoTracking().SingleOrDefaultAsync(team => team.Id == teamId, cancellationToken);

    public async Task<IReadOnlyList<TeamMember>> GetMembersAsync(
        long teamId, CancellationToken cancellationToken = default) =>
        await context.TeamMembers.AsNoTracking().Include(member => member.User)
            .Where(member => member.TeamId == teamId)
            .OrderBy(member => member.UserId).ToListAsync(cancellationToken);

    public async Task<TeamMember> AddMemberAsync(
        long teamId, long userId, CancellationToken cancellationToken = default) {
        await RequireTeamAsync(teamId, cancellationToken);
        if (userId <= 0 || !await context.Users.AnyAsync(user => user.Id == userId, cancellationToken)) {
            throw new ArgumentException("選択した利用者が見つかりません。利用者を選択し直してください。", nameof(userId));
        }
        if (await context.TeamMembers.AnyAsync(member => member.TeamId == teamId && member.UserId == userId, cancellationToken)) {
            throw new DuplicateTeamMemberException(teamId, userId);
        }
        var member = new TeamMember { TeamId = teamId, UserId = userId, Role = "member" };
        context.TeamMembers.Add(member);
        try {
            await context.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateException) {
            context.Entry(member).State = EntityState.Detached;
            // 別のリクエストによる同時追加も、複合主キーで防いで画面に通知する。
            if (await context.TeamMembers.AnyAsync(item => item.TeamId == teamId && item.UserId == userId, cancellationToken)) {
                throw new DuplicateTeamMemberException(teamId, userId);
            }
            throw;
        }
        return member;
    }

    public async Task<bool> RemoveMemberAsync(long teamId, long userId, CancellationToken cancellationToken = default) {
        await RequireTeamAsync(teamId, cancellationToken);
        var member = await context.TeamMembers.SingleOrDefaultAsync(
            item => item.TeamId == teamId && item.UserId == userId, cancellationToken);
        if (member is null) return false;
        context.TeamMembers.Remove(member);
        try {
            await context.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateConcurrencyException) {
            context.Entry(member).State = EntityState.Detached;
            if (await context.TeamMembers.AnyAsync(item => item.TeamId == teamId && item.UserId == userId, cancellationToken)) {
                throw;
            }
            return false;
        }
        return true;
    }

    private async Task RequireTeamAsync(long teamId, CancellationToken cancellationToken) {
        if (teamId <= 0 || !await context.Teams.AnyAsync(team => team.Id == teamId, cancellationToken)) {
            throw new ArgumentException("選択したチームが見つかりません。チームを選択し直してください。", nameof(teamId));
        }
    }
}
