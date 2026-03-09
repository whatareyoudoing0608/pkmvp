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
    public class TaskProgressRepository : ITaskProgressRepository
    {
        private readonly string _cs;

        public TaskProgressRepository(IConfiguration cfg)
        {
            _cs = cfg.GetConnectionString("Oracle");
        }

        public async Task<IReadOnlyList<TaskProgressItem>> GetByTaskIdAsync(decimal taskId)
        {
            const string sql = @"
SELECT
    PROGRESS_ID   AS ProgressId,
    TASK_ID       AS TaskId,
    AUTHOR_ID     AS AuthorId,
    LOG_DATE      AS LogDate,
    STATUS        AS Status,
    PROGRESS_PCT  AS ProgressPct,
    SPENT_MINUTES AS SpentMinutes,
    COMMENT_TXT   AS CommentTxt,
    CREATED_AT    AS CreatedAt
FROM TASK_PROGRESS
WHERE TASK_ID = :p_task_id
ORDER BY LOG_DATE DESC, PROGRESS_ID DESC";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            var rows = await conn.QueryAsync<TaskProgressItem>(sql, new { p_task_id = taskId });
            return rows.ToList();
        }

        public async Task<decimal> CreateAsync(decimal taskId, CreateTaskProgressRequest req)
        {
            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.BindByName = true;
            cmd.CommandText = @"
INSERT INTO TASK_PROGRESS
(
  TASK_ID, AUTHOR_ID, LOG_DATE, STATUS, PROGRESS_PCT, SPENT_MINUTES, COMMENT_TXT
)
VALUES
(
  :p_task_id, :p_author_id, :p_log_date, :p_status, :p_progress, :p_minutes, :p_comment
)
RETURNING PROGRESS_ID INTO :p_progress_id";

            cmd.Parameters.Add(new OracleParameter("p_task_id", taskId));
            cmd.Parameters.Add(new OracleParameter("p_author_id", req.AuthorId));
            cmd.Parameters.Add(new OracleParameter("p_log_date", (object)(req.LogDate ?? DateTime.Today) ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("p_status", req.Status));
            cmd.Parameters.Add(new OracleParameter("p_progress", req.ProgressPct));
            cmd.Parameters.Add(new OracleParameter("p_minutes", req.SpentMinutes));
            cmd.Parameters.Add(new OracleParameter("p_comment", (object)req.CommentTxt ?? DBNull.Value));

            var outParam = new OracleParameter("p_progress_id", OracleDbType.Decimal)
            {
                Direction = System.Data.ParameterDirection.Output
            };
            cmd.Parameters.Add(outParam);

            await cmd.ExecuteNonQueryAsync();
            return Convert.ToDecimal(outParam.Value.ToString());
        }
    }
}