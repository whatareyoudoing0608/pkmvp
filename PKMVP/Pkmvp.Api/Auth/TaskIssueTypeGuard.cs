using System;
using System.Collections.Generic;

namespace Pkmvp.Api.Auth
{
    public static class TaskIssueTypeGuard
    {
        private static readonly HashSet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "EPIC", "STORY", "TASK", "SUBTASK", "BUG", "SPIKE", "DOC", "RESEARCH"
        };

        public static string NormalizeOrDefault(string taskType)
        {
            return string.IsNullOrWhiteSpace(taskType)
                ? "TASK"
                : taskType.Trim().ToUpperInvariant();
        }

        public static bool IsAllowed(string taskType)
        {
            return !string.IsNullOrWhiteSpace(taskType) && Allowed.Contains(taskType.Trim());
        }

        public static string AllowedList()
        {
            return "EPIC, STORY, TASK, SUBTASK, BUG, SPIKE, DOC, RESEARCH";
        }
    }
}
