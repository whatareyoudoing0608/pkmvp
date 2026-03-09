using System;

namespace Pkmvp.Api.Auth
{
    public static class WorklogGuard
    {
        public static bool CanEditDraft(CurrentUser me, long reporterId, string reporterTeamId, bool sameTeamByDirectory = false)
        {
            if (me == null) return false;
            if (me.Role == UserRole.ADMIN) return true;
            if (me.UserId == reporterId) return true;

            var sameTeam = IsSameTeam(me.TeamId, reporterTeamId) || sameTeamByDirectory;
            return me.Role == UserRole.MANAGER && sameTeam;
        }

        public static bool CanApproveReject(CurrentUser me, long reporterId, string reporterTeamId, bool sameTeamByDirectory = false)
        {
            if (me == null) return false;
            if (me.Role == UserRole.ADMIN) return true;

            var sameTeam = IsSameTeam(me.TeamId, reporterTeamId) || sameTeamByDirectory;
            return me.Role == UserRole.MANAGER && sameTeam;
        }

        private static bool IsSameTeam(string myTeamId, string reporterTeamId)
        {
            return !string.IsNullOrWhiteSpace(myTeamId)
                && !string.IsNullOrWhiteSpace(reporterTeamId)
                && string.Equals(myTeamId, reporterTeamId, StringComparison.OrdinalIgnoreCase);
        }
    }
}
