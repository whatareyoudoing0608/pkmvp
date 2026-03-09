using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Pkmvp.Api.Models;
using Pkmvp.Api.Auth;

namespace Pkmvp.Api.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly string _cs;

        public TaskRepository(IConfiguration cfg)
        {
            _cs = cfg.GetConnectionString("Oracle");
        }

        public async Task<IReadOnlyList<TaskItem>> GetListScopedAsync(
            string status,
            decimal? assigneeId,
            WorklogScope scope,
            long myUserId,
            IEnumerable<long> teamUserIds)
        {
            var withTypeSql = BuildListScopedSql(includeIssueColumns: true, scope, teamUserIds, out var withTypeParams, status, assigneeId, myUserId);
            var legacySql = BuildListScopedSql(includeIssueColumns: false, scope, teamUserIds, out var legacyParams, status, assigneeId, myUserId);

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            try
            {
                var rows = await conn.QueryAsync<TaskItem>(withTypeSql, withTypeParams);
                return rows.ToList();
            }
            catch (OracleException ex) when (IsInvalidIdentifier(ex))
            {
                var rows = await conn.QueryAsync<TaskItem>(legacySql, legacyParams);
                return rows.ToList();
            }
        }

        public async Task<IReadOnlyList<TaskItem>> GetListAsync(string status, decimal? assigneeId)
        {
            var withTypeSql = BuildListSql(includeIssueColumns: true);
            var legacySql = BuildListSql(includeIssueColumns: false);

            var param = new
            {
                p_status = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToUpperInvariant(),
                p_assignee_id = assigneeId
            };

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            try
            {
                var rows = await conn.QueryAsync<TaskItem>(withTypeSql, param);
                return rows.ToList();
            }
            catch (OracleException ex) when (IsInvalidIdentifier(ex))
            {
                var rows = await conn.QueryAsync<TaskItem>(legacySql, param);
                return rows.ToList();
            }
        }

        public async Task<TaskItem> GetByIdAsync(decimal taskId)
        {
            var withTypeSql = BuildGetByIdSql(includeIssueColumns: true);
            var legacySql = BuildGetByIdSql(includeIssueColumns: false);

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            try
            {
                return await conn.QueryFirstOrDefaultAsync<TaskItem>(withTypeSql, new { p_task_id = taskId });
            }
            catch (OracleException ex) when (IsInvalidIdentifier(ex))
            {
                return await conn.QueryFirstOrDefaultAsync<TaskItem>(legacySql, new { p_task_id = taskId });
            }
        }

        public async Task<decimal> CreateAsync(CreateTaskRequest req)
        {
            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.BindByName = true;
                cmd.CommandText = @"
INSERT INTO TASK
(
  TITLE, DESCRIPTION, PRIORITY, STATUS, PROGRESS_PCT,
  TASK_TYPE,
  REPORTER_ID, ASSIGNEE_ID, START_DATE, DUE_DATE
)
VALUES
(
  :p_title, :p_desc, :p_priority, :p_status, :p_progress,
  :p_task_type,
  :p_reporter_id, :p_assignee_id, :p_start_date, :p_due_date
)
RETURNING TASK_ID INTO :p_task_id";

                cmd.Parameters.Add(new OracleParameter("p_title", req.Title));
                cmd.Parameters.Add(new OracleParameter("p_desc", (object)req.Description ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("p_priority", req.Priority));
                cmd.Parameters.Add(new OracleParameter("p_status", req.Status));
                cmd.Parameters.Add(new OracleParameter("p_progress", req.ProgressPct));
                cmd.Parameters.Add(new OracleParameter("p_task_type", req.TaskType));
                cmd.Parameters.Add(new OracleParameter("p_reporter_id", req.ReporterId));
                cmd.Parameters.Add(new OracleParameter("p_assignee_id", (object)req.AssigneeId ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("p_start_date", (object)req.StartDate ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("p_due_date", (object)req.DueDate ?? DBNull.Value));

                var outParam = new OracleParameter("p_task_id", OracleDbType.Decimal)
                {
                    Direction = System.Data.ParameterDirection.Output
                };
                cmd.Parameters.Add(outParam);

                await cmd.ExecuteNonQueryAsync();
                return Convert.ToDecimal(outParam.Value.ToString());
            }
            catch (OracleException ex) when (IsInvalidIdentifier(ex))
            {
                using var cmd = conn.CreateCommand();
                cmd.BindByName = true;
                cmd.CommandText = @"
INSERT INTO TASK
(
  TITLE, DESCRIPTION, PRIORITY, STATUS, PROGRESS_PCT,
  REPORTER_ID, ASSIGNEE_ID, START_DATE, DUE_DATE
)
VALUES
(
  :p_title, :p_desc, :p_priority, :p_status, :p_progress,
  :p_reporter_id, :p_assignee_id, :p_start_date, :p_due_date
)
RETURNING TASK_ID INTO :p_task_id";

                cmd.Parameters.Add(new OracleParameter("p_title", req.Title));
                cmd.Parameters.Add(new OracleParameter("p_desc", (object)req.Description ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("p_priority", req.Priority));
                cmd.Parameters.Add(new OracleParameter("p_status", req.Status));
                cmd.Parameters.Add(new OracleParameter("p_progress", req.ProgressPct));
                cmd.Parameters.Add(new OracleParameter("p_reporter_id", req.ReporterId));
                cmd.Parameters.Add(new OracleParameter("p_assignee_id", (object)req.AssigneeId ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("p_start_date", (object)req.StartDate ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("p_due_date", (object)req.DueDate ?? DBNull.Value));

                var outParam = new OracleParameter("p_task_id", OracleDbType.Decimal)
                {
                    Direction = System.Data.ParameterDirection.Output
                };
                cmd.Parameters.Add(outParam);

                await cmd.ExecuteNonQueryAsync();
                return Convert.ToDecimal(outParam.Value.ToString());
            }
        }

        public async Task<bool> UpdateAsync(decimal taskId, UpdateTaskRequest req)
        {
            const string withTypeSql = @"
UPDATE TASK
   SET TITLE        = :p_title,
       DESCRIPTION  = :p_desc,
       PRIORITY     = :p_priority,
       STATUS       = :p_status,
       PROGRESS_PCT = :p_progress,
       TASK_TYPE    = :p_task_type,
       ASSIGNEE_ID  = :p_assignee_id,
       START_DATE   = :p_start_date,
       DUE_DATE     = :p_due_date
 WHERE TASK_ID      = :p_task_id";

            const string legacySql = @"
UPDATE TASK
   SET TITLE        = :p_title,
       DESCRIPTION  = :p_desc,
       PRIORITY     = :p_priority,
       STATUS       = :p_status,
       PROGRESS_PCT = :p_progress,
       ASSIGNEE_ID  = :p_assignee_id,
       START_DATE   = :p_start_date,
       DUE_DATE     = :p_due_date
 WHERE TASK_ID      = :p_task_id";

            var param = new
            {
                p_title = req.Title,
                p_desc = (object)req.Description ?? DBNull.Value,
                p_priority = req.Priority,
                p_status = req.Status,
                p_progress = req.ProgressPct,
                p_task_type = req.TaskType,
                p_assignee_id = (object)req.AssigneeId ?? DBNull.Value,
                p_start_date = (object)req.StartDate ?? DBNull.Value,
                p_due_date = (object)req.DueDate ?? DBNull.Value,
                p_task_id = taskId
            };

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            try
            {
                var affected = await conn.ExecuteAsync(withTypeSql, param);
                return affected > 0;
            }
            catch (OracleException ex) when (IsInvalidIdentifier(ex))
            {
                var affected = await conn.ExecuteAsync(legacySql, param);
                return affected > 0;
            }
        }

        public async Task<bool> UpdateStatusAsync(decimal taskId, string status, int? progressPct)
        {
            var finalProgress = progressPct ?? -1;
            if (string.Equals(status, "DONE", StringComparison.OrdinalIgnoreCase))
                finalProgress = 100;

            const string sql = @"
UPDATE TASK
   SET STATUS       = :p_status,
       PROGRESS_PCT = CASE WHEN :p_progress = -1 THEN PROGRESS_PCT ELSE :p_progress END
 WHERE TASK_ID      = :p_task_id";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            var affected = await conn.ExecuteAsync(sql, new
            {
                p_status = status,
                p_progress = finalProgress,
                p_task_id = taskId
            });

            return affected > 0;
        }

        private static bool IsInvalidIdentifier(OracleException ex)
        {
            return ex.Number == 904;
        }

        private static string SelectColumns(bool includeIssueColumns)
        {
            if (includeIssueColumns)
            {
                return @"
    TASK_ID                            AS TaskId,
    NVL(ISSUE_KEY, 'TASK-' || TASK_ID) AS IssueKey,
    NVL(TASK_TYPE, 'TASK')             AS TaskType,
    TITLE                              AS Title,
    DESCRIPTION                        AS Description,
    PRIORITY                           AS Priority,
    STATUS                             AS Status,
    PROGRESS_PCT                       AS ProgressPct,
    REPORTER_ID                        AS ReporterId,
    ASSIGNEE_ID                        AS AssigneeId,
    PROJECT_ID                         AS ProjectId,
    SPRINT_ID                          AS SprintId,
    STORY_POINTS                       AS StoryPoints,
    START_DATE                         AS StartDate,
    DUE_DATE                           AS DueDate,
    DONE_DATE                          AS DoneDate,
    CREATED_AT                         AS CreatedAt,
    UPDATED_AT                         AS UpdatedAt";
            }

            return @"
    TASK_ID                            AS TaskId,
    ('TASK-' || TASK_ID)               AS IssueKey,
    'TASK'                             AS TaskType,
    TITLE                              AS Title,
    DESCRIPTION                        AS Description,
    PRIORITY                           AS Priority,
    STATUS                             AS Status,
    PROGRESS_PCT                       AS ProgressPct,
    REPORTER_ID                        AS ReporterId,
    ASSIGNEE_ID                        AS AssigneeId,
    CAST(NULL AS NUMBER)               AS ProjectId,
    CAST(NULL AS NUMBER)               AS SprintId,
    CAST(NULL AS NUMBER)               AS StoryPoints,
    START_DATE                         AS StartDate,
    DUE_DATE                           AS DueDate,
    DONE_DATE                          AS DoneDate,
    CREATED_AT                         AS CreatedAt,
    UPDATED_AT                         AS UpdatedAt";
        }

        private static string BuildListSql(bool includeIssueColumns)
        {
            return $@"
SELECT
{SelectColumns(includeIssueColumns)}
FROM TASK
WHERE (:p_status IS NULL OR STATUS = :p_status)
  AND (:p_assignee_id IS NULL OR ASSIGNEE_ID = :p_assignee_id)
ORDER BY DUE_DATE NULLS LAST, TASK_ID DESC";
        }

        private static string BuildGetByIdSql(bool includeIssueColumns)
        {
            return $@"
SELECT
{SelectColumns(includeIssueColumns)}
FROM TASK
WHERE TASK_ID = :p_task_id";
        }

        private static string BuildListScopedSql(
            bool includeIssueColumns,
            WorklogScope scope,
            IEnumerable<long> teamUserIds,
            out DynamicParameters dp,
            string status,
            decimal? assigneeId,
            long myUserId)
        {
            var sql = $@"
SELECT
{SelectColumns(includeIssueColumns)}
FROM TASK
WHERE (:p_status IS NULL OR STATUS = :p_status)
  AND (:p_assignee_id IS NULL OR ASSIGNEE_ID = :p_assignee_id)
";

            dp = new DynamicParameters();
            dp.Add("p_status", string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToUpperInvariant());
            dp.Add("p_assignee_id", assigneeId);

            if (scope == WorklogScope.mine)
            {
                sql += "  AND (REPORTER_ID = :p_me OR ASSIGNEE_ID = :p_me)\n";
                dp.Add("p_me", myUserId);
            }
            else if (scope == WorklogScope.team)
            {
                var ids = (teamUserIds ?? Array.Empty<long>()).Distinct().ToList();
                if (ids.Count == 0)
                {
                    sql += "  AND 1 = 0\n";
                }
                else
                {
                    var placeholders = new List<string>();
                    for (int i = 0; i < ids.Count; i++)
                    {
                        var pn = $"p_team_{i}";
                        placeholders.Add($":{pn}");
                        dp.Add(pn, ids[i]);
                    }

                    var inList = string.Join(",", placeholders);
                    sql += $"  AND (REPORTER_ID IN ({inList}) OR ASSIGNEE_ID IN ({inList}))\n";
                }
            }

            sql += "ORDER BY DUE_DATE NULLS LAST, TASK_ID DESC";
            return sql;
        }
    }
}
