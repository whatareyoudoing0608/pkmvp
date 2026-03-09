using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Pkmvp.Api.Models;

namespace Pkmvp.Api.Repositories
{
    public class PlanningRepository : IPlanningRepository
    {
        private readonly string _cs;

        public PlanningRepository(IConfiguration cfg)
        {
            _cs = cfg.GetConnectionString("Oracle");
        }

        public async Task<IReadOnlyList<ProjectItem>> ListProjectsAsync()
        {
            const string sql = @"
SELECT
    PROJECT_ID   AS ProjectId,
    PROJECT_KEY  AS ProjectKey,
    NAME         AS Name,
    DESCRIPTION  AS Description,
    LEAD_USER_ID AS LeadUserId,
    CREATED_AT   AS CreatedAt,
    UPDATED_AT   AS UpdatedAt
FROM PKMVP.PROJECT
ORDER BY PROJECT_KEY";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();
            var rows = await conn.QueryAsync<ProjectItem>(sql);
            return rows.ToList();
        }

        public async Task<ProjectItem> GetProjectAsync(decimal projectId)
        {
            const string sql = @"
SELECT
    PROJECT_ID   AS ProjectId,
    PROJECT_KEY  AS ProjectKey,
    NAME         AS Name,
    DESCRIPTION  AS Description,
    LEAD_USER_ID AS LeadUserId,
    CREATED_AT   AS CreatedAt,
    UPDATED_AT   AS UpdatedAt
FROM PKMVP.PROJECT
WHERE PROJECT_ID = :p_project_id";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();
            return await conn.QueryFirstOrDefaultAsync<ProjectItem>(sql, new { p_project_id = projectId });
        }

        public async Task<decimal> CreateProjectAsync(CreateProjectRequest req)
        {
            const string nextIdSql = "SELECT PKMVP.SEQ_PROJECT.NEXTVAL FROM DUAL";
            const string insertSql = @"
INSERT INTO PKMVP.PROJECT
(
    PROJECT_ID,
    PROJECT_KEY,
    NAME,
    DESCRIPTION,
    LEAD_USER_ID,
    CREATED_AT,
    UPDATED_AT
)
VALUES
(
    :p_project_id,
    :p_project_key,
    :p_name,
    :p_description,
    :p_lead_user_id,
    SYSDATE,
    SYSDATE
)";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            var projectId = await conn.ExecuteScalarAsync<decimal>(nextIdSql);
            await conn.ExecuteAsync(insertSql, new
            {
                p_project_id = projectId,
                p_project_key = req.ProjectKey,
                p_name = req.Name,
                p_description = (object)req.Description ?? DBNull.Value,
                p_lead_user_id = (object)req.LeadUserId ?? DBNull.Value
            });

            return projectId;
        }

        public async Task<IReadOnlyList<BoardItem>> ListBoardsByProjectAsync(decimal projectId)
        {
            const string sql = @"
SELECT
    BOARD_ID    AS BoardId,
    PROJECT_ID  AS ProjectId,
    NAME        AS Name,
    BOARD_TYPE  AS BoardType,
    CREATED_AT  AS CreatedAt,
    UPDATED_AT  AS UpdatedAt
FROM PKMVP.BOARD
WHERE PROJECT_ID = :p_project_id
ORDER BY BOARD_ID";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();
            var rows = await conn.QueryAsync<BoardItem>(sql, new { p_project_id = projectId });
            return rows.ToList();
        }

        public async Task<BoardItem> GetBoardAsync(decimal boardId)
        {
            const string sql = @"
SELECT
    BOARD_ID    AS BoardId,
    PROJECT_ID  AS ProjectId,
    NAME        AS Name,
    BOARD_TYPE  AS BoardType,
    CREATED_AT  AS CreatedAt,
    UPDATED_AT  AS UpdatedAt
FROM PKMVP.BOARD
WHERE BOARD_ID = :p_board_id";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();
            return await conn.QueryFirstOrDefaultAsync<BoardItem>(sql, new { p_board_id = boardId });
        }

        public async Task<decimal> CreateBoardAsync(decimal projectId, CreateBoardRequest req)
        {
            const string nextIdSql = "SELECT PKMVP.SEQ_BOARD.NEXTVAL FROM DUAL";
            const string insertSql = @"
INSERT INTO PKMVP.BOARD
(
    BOARD_ID,
    PROJECT_ID,
    NAME,
    BOARD_TYPE,
    CREATED_AT,
    UPDATED_AT
)
VALUES
(
    :p_board_id,
    :p_project_id,
    :p_name,
    :p_board_type,
    SYSDATE,
    SYSDATE
)";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            var boardId = await conn.ExecuteScalarAsync<decimal>(nextIdSql);
            await conn.ExecuteAsync(insertSql, new
            {
                p_board_id = boardId,
                p_project_id = projectId,
                p_name = req.Name,
                p_board_type = req.BoardType
            });

            return boardId;
        }

        public async Task<IReadOnlyList<SprintItem>> ListSprintsByBoardAsync(decimal boardId)
        {
            const string sql = @"
SELECT
    SPRINT_ID   AS SprintId,
    BOARD_ID    AS BoardId,
    NAME        AS Name,
    GOAL        AS Goal,
    START_DATE  AS StartDate,
    END_DATE    AS EndDate,
    STATUS      AS Status,
    CREATED_AT  AS CreatedAt,
    UPDATED_AT  AS UpdatedAt
FROM PKMVP.SPRINT
WHERE BOARD_ID = :p_board_id
ORDER BY SPRINT_ID DESC";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();
            var rows = await conn.QueryAsync<SprintItem>(sql, new { p_board_id = boardId });
            return rows.ToList();
        }

        public async Task<SprintItem> GetSprintAsync(decimal sprintId)
        {
            const string sql = @"
SELECT
    SPRINT_ID   AS SprintId,
    BOARD_ID    AS BoardId,
    NAME        AS Name,
    GOAL        AS Goal,
    START_DATE  AS StartDate,
    END_DATE    AS EndDate,
    STATUS      AS Status,
    CREATED_AT  AS CreatedAt,
    UPDATED_AT  AS UpdatedAt
FROM PKMVP.SPRINT
WHERE SPRINT_ID = :p_sprint_id";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();
            return await conn.QueryFirstOrDefaultAsync<SprintItem>(sql, new { p_sprint_id = sprintId });
        }

        public async Task<decimal> CreateSprintAsync(decimal boardId, CreateSprintRequest req)
        {
            const string nextIdSql = "SELECT PKMVP.SEQ_SPRINT.NEXTVAL FROM DUAL";
            const string insertSql = @"
INSERT INTO PKMVP.SPRINT
(
    SPRINT_ID,
    BOARD_ID,
    NAME,
    GOAL,
    START_DATE,
    END_DATE,
    STATUS,
    CREATED_AT,
    UPDATED_AT
)
VALUES
(
    :p_sprint_id,
    :p_board_id,
    :p_name,
    :p_goal,
    :p_start_date,
    :p_end_date,
    'PLANNED',
    SYSDATE,
    SYSDATE
)";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            var sprintId = await conn.ExecuteScalarAsync<decimal>(nextIdSql);
            await conn.ExecuteAsync(insertSql, new
            {
                p_sprint_id = sprintId,
                p_board_id = boardId,
                p_name = req.Name,
                p_goal = (object)req.Goal ?? DBNull.Value,
                p_start_date = (object)req.StartDate ?? DBNull.Value,
                p_end_date = (object)req.EndDate ?? DBNull.Value
            });

            return sprintId;
        }

        public async Task<bool> UpdateSprintStatusAsync(decimal sprintId, string status)
        {
            const string sql = @"
UPDATE PKMVP.SPRINT
   SET STATUS = :p_status,
       UPDATED_AT = SYSDATE
 WHERE SPRINT_ID = :p_sprint_id";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            var affected = await conn.ExecuteAsync(sql, new
            {
                p_status = status,
                p_sprint_id = sprintId
            });

            return affected > 0;
        }

        public async Task<IReadOnlyList<BoardIssueItem>> ListBoardIssuesAsync(decimal boardId, decimal? sprintId, string status)
        {
            const string sql = @"
SELECT
    t.TASK_ID      AS TaskId,
    NVL(t.ISSUE_KEY, 'TASK-' || t.TASK_ID) AS IssueKey,
    t.TASK_TYPE    AS TaskType,
    t.TITLE        AS Title,
    t.DESCRIPTION  AS Description,
    t.STATUS       AS Status,
    t.PRIORITY     AS Priority,
    t.PROGRESS_PCT AS ProgressPct,
    t.REPORTER_ID  AS ReporterId,
    t.ASSIGNEE_ID  AS AssigneeId,
    t.PROJECT_ID   AS ProjectId,
    t.SPRINT_ID    AS SprintId,
    t.STORY_POINTS AS StoryPoints,
    t.DUE_DATE     AS DueDate,
    t.UPDATED_AT   AS UpdatedAt
FROM TASK t
JOIN PKMVP.BOARD b ON b.PROJECT_ID = t.PROJECT_ID
WHERE b.BOARD_ID = :p_board_id
  AND (:p_sprint_id IS NULL OR t.SPRINT_ID = :p_sprint_id)
  AND (:p_status IS NULL OR t.STATUS = :p_status)
ORDER BY NVL(t.DUE_DATE, DATE '2999-12-31'), t.TASK_ID DESC";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            var rows = await conn.QueryAsync<BoardIssueItem>(sql, new
            {
                p_board_id = boardId,
                p_sprint_id = sprintId,
                p_status = string.IsNullOrWhiteSpace(status) ? null : status
            });

            return rows.ToList();
        }

        public async Task<bool> PlanIssueAsync(decimal boardId, decimal taskId, decimal? sprintId)
        {
            const string sql = @"
UPDATE TASK t
   SET t.PROJECT_ID = (SELECT b.PROJECT_ID FROM PKMVP.BOARD b WHERE b.BOARD_ID = :p_board_id),
       t.SPRINT_ID  = :p_sprint_id,
       t.UPDATED_AT = SYSDATE
 WHERE t.TASK_ID = :p_task_id
   AND (:p_sprint_id IS NULL OR EXISTS (
       SELECT 1 FROM PKMVP.SPRINT s
        WHERE s.SPRINT_ID = :p_sprint_id
          AND s.BOARD_ID = :p_board_id
   ))";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            var affected = await conn.ExecuteAsync(sql, new
            {
                p_board_id = boardId,
                p_task_id = taskId,
                p_sprint_id = sprintId
            });

            return affected > 0;
        }
    }
}
