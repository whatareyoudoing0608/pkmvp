using System;
using System.Collections.Generic;

namespace Pkmvp.Api.Auth
{
    public static class TaskWorkflowGuard
    {
        private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["TODO"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "IN_PROGRESS", "BLOCKED", "CANCELED" },
            ["IN_PROGRESS"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "BLOCKED", "DONE", "CANCELED" },
            ["BLOCKED"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "IN_PROGRESS", "CANCELED" },
            ["DONE"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "IN_PROGRESS", "CANCELED" },
            ["CANCELED"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "IN_PROGRESS" }
        };

        public static bool TryValidateTransition(string currentStatus, string nextStatus, string taskType, UserRole actorRole, out string message)
        {
            var from = NormalizeStatus(currentStatus);
            var to = NormalizeStatus(nextStatus);
            var type = TaskIssueTypeGuard.NormalizeOrDefault(taskType);

            if (!TaskStatusGuard.IsAllowed(from) || !TaskStatusGuard.IsAllowed(to))
            {
                message = "Invalid status. Allowed: " + TaskStatusGuard.AllowedList();
                return false;
            }

            if (!TaskIssueTypeGuard.IsAllowed(type))
            {
                message = "Invalid taskType. Allowed: " + TaskIssueTypeGuard.AllowedList();
                return false;
            }

            if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            {
                message = null;
                return true;
            }

            if (!AllowedTransitions.TryGetValue(from, out var targets) || !targets.Contains(to))
            {
                message = $"Workflow transition not allowed: {from} -> {to}";
                return false;
            }

            if (actorRole == UserRole.USER && string.Equals(to, "CANCELED", StringComparison.OrdinalIgnoreCase))
            {
                message = "Only MANAGER/ADMIN can move issue to CANCELED.";
                return false;
            }

            if (actorRole == UserRole.USER
                && string.Equals(from, "DONE", StringComparison.OrdinalIgnoreCase)
                && string.Equals(to, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase))
            {
                message = "Only MANAGER/ADMIN can reopen DONE issues.";
                return false;
            }

            message = null;
            return true;
        }

        private static string NormalizeStatus(string status)
        {
            return string.IsNullOrWhiteSpace(status)
                ? string.Empty
                : status.Trim().ToUpperInvariant();
        }
    }
}
