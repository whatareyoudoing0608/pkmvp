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
    public class WeeklyReportRepository : IWeeklyReportRepository
    {
        private readonly string _cs;

        public WeeklyReportRepository(IConfiguration cfg)
        {
            _cs = cfg.GetConnectionString("Oracle");
        }

        public async Task<IReadOnlyList<WeeklySummaryRow>> GetWeeklySummaryAsync(DateTime anyDateInWeek, decimal? userId)
        {
            const string sql = @"
WITH
PARAM AS (
  SELECT TRUNC(:p_week, 'IW') AS WS, TRUNC(:p_week, 'IW') + 7 AS WE FROM DUAL
),
U AS (
  SELECT u.user_id, u.user_name
    FROM app_user u
   WHERE u.is_active='Y'
     AND (:p_user_id IS NULL OR u.user_id = :p_user_id)
),
WLOG AS (
  SELECT d.user_id,
         SUM(
           NVL(
             (SELECT SUM(i.minutes) FROM daily_worklog_item i WHERE i.worklog_id = d.worklog_id),
             d.total_minutes
           )
         ) AS sum_minutes
    FROM daily_worklog d, PARAM p
   WHERE d.work_date >= p.WS
     AND d.work_date <  p.WE
     AND (:p_user_id IS NULL OR d.user_id = :p_user_id)
   GROUP BY d.user_id
),
DONE_TASK AS (
  SELECT t.assignee_id AS user_id, COUNT(*) AS done_cnt
    FROM task t, PARAM p
   WHERE t.status = 'DONE'
     AND t.assignee_id IS NOT NULL
     AND t.done_date >= p.WS
     AND t.done_date <  p.WE
     AND (:p_user_id IS NULL OR t.assignee_id = :p_user_id)
   GROUP BY t.assignee_id
),
EVAL AS (
  SELECT t.assignee_id AS user_id,
         AVG((e.score_quality + e.score_timeliness + e.score_communication) / 3) AS avg_score
    FROM task_evaluation e
    JOIN task t ON t.task_id = e.task_id,
         PARAM p
   WHERE t.assignee_id IS NOT NULL
     AND e.evaluated_at >= CAST(p.WS AS TIMESTAMP)
     AND e.evaluated_at <  CAST(p.WE AS TIMESTAMP)
     AND (:p_user_id IS NULL OR t.assignee_id = :p_user_id)
   GROUP BY t.assignee_id
),
BLOCKED AS (
  SELECT t.assignee_id AS user_id, COUNT(DISTINCT tp.task_id) AS blocked_cnt
    FROM task_progress tp
    JOIN task t ON t.task_id = tp.task_id,
         PARAM p
   WHERE tp.status = 'BLOCKED'
     AND t.assignee_id IS NOT NULL
     AND tp.log_date >= p.WS
     AND tp.log_date <  p.WE
     AND (:p_user_id IS NULL OR t.assignee_id = :p_user_id)
   GROUP BY t.assignee_id
)
SELECT
  u.user_id,
  u.user_name,
  p.WS AS week_start,
  p.WE - 1 AS week_end,
  NVL(w.sum_minutes,0) AS sum_minutes,
  NVL(d.done_cnt,0)    AS done_cnt,
  ROUND(NVL(e.avg_score,0), 2) AS avg_score,
  NVL(b.blocked_cnt,0) AS blocked_cnt
FROM U u
CROSS JOIN PARAM p
LEFT JOIN WLOG w      ON w.user_id = u.user_id
LEFT JOIN DONE_TASK d ON d.user_id = u.user_id
LEFT JOIN EVAL e      ON e.user_id = u.user_id
LEFT JOIN BLOCKED b   ON b.user_id = u.user_id
ORDER BY u.user_name";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            var rows = await conn.QueryAsync<WeeklySummaryRow>(sql, new { p_week = anyDateInWeek, p_user_id = userId });
            return rows.ToList();
        }
    }
}