using System;
using System.Collections.Generic;

namespace Pkmvp.Api.Auth
{
    public static class TaskStatusGuard
    {
        private static readonly HashSet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "TODO", "IN_PROGRESS", "BLOCKED", "DONE", "CANCELED"
        };

        public static bool IsAllowed(string status)
        {
            return !string.IsNullOrWhiteSpace(status) && Allowed.Contains(status);
        }

        public static string AllowedList()
        {
            return "TODO, IN_PROGRESS, BLOCKED, DONE, CANCELED";
        }
    }
}