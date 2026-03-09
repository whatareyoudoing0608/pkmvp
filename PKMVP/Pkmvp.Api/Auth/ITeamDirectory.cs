using System.Collections.Generic;

namespace Pkmvp.Api.Auth
{
    public interface ITeamDirectory
    {
        bool IsUserInTeam(long userId, string teamId);
        IReadOnlyList<long> GetTeamUserIds(string teamId);
    }
}
