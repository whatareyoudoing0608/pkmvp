using System;

namespace Pkmvp.Api.Auth
{
    public static class ScopeGuard
    {
        public static bool IsAllowed(CurrentUser u, WorklogScope scope)
        {
            if (scope == WorklogScope.mine) return true;

            if (scope == WorklogScope.team)
                return u.Role == UserRole.MANAGER || u.Role == UserRole.ADMIN;

            if (scope == WorklogScope.all)
                return u.Role == UserRole.ADMIN;

            return false;
        }

        public static string GetDenyMessage(WorklogScope scope)
        {
            if (scope == WorklogScope.team) return "Scope 'team' not allowed.";
            if (scope == WorklogScope.all) return "Scope 'all' not allowed.";
            return "Scope not allowed.";
        }
    }
}
