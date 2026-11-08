namespace App.Domain.Exceptions;

public sealed class DuplicateTeamMemberException(long teamId, long userId)
    : Exception("選択した利用者は既にこのチームのメンバーです。") {
    public long TeamId { get; } = teamId;
    public long UserId { get; } = userId;
}
