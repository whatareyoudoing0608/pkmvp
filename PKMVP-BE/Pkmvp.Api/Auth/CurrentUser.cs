namespace Pkmvp.Api.Auth
{
    public sealed class CurrentUser
    {
        public long UserId { get; private set; }
        public UserRole Role { get; private set; }
        public string TeamId { get; private set; } // null 가능(프로젝트 설정에 따라 경고만)

        public CurrentUser(long userId, UserRole role, string teamId)
        {
            UserId = userId;
            Role = role;
            TeamId = teamId;
        }
    }
}