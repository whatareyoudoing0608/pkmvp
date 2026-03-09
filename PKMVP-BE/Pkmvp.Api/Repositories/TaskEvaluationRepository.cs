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
    public class TaskEvaluationRepository : ITaskEvaluationRepository
    {
        private readonly string _cs;

        public TaskEvaluationRepository(IConfiguration cfg)
        {
            _cs = cfg.GetConnectionString("Oracle");
        }

        public async Task<IReadOnlyList<TaskEvaluationItem>> GetByTaskIdAsync(decimal taskId)
        {
            const string sql = @"
                            SELECT
                              EVAL_ID             AS EvalId,
                              TASK_ID             AS TaskId,
                              EVALUATOR_ID        AS EvaluatorId,
                              SCORE_QUALITY       AS ScoreQuality,
                              SCORE_TIMELINESS    AS ScoreTimeliness,
                              SCORE_COMMUNICATION AS ScoreCommunication,
                              COMMENT_TXT         AS CommentTxt,
                              EVALUATED_AT        AS EvaluatedAt
                            FROM TASK_EVALUATION
                            WHERE TASK_ID = :p_task_id
                            ORDER BY EVALUATED_AT DESC, EVAL_ID DESC";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            var rows = await conn.QueryAsync<TaskEvaluationItem>(sql, new { p_task_id = taskId });
            return rows.ToList();
        }

        public async Task<decimal> CreateAsync(decimal taskId, CreateTaskEvaluationRequest req)
        {
            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            const string sqlUpdate = @"
UPDATE TASK_EVALUATION
   SET SCORE_QUALITY       = :p_s1,
       SCORE_TIMELINESS    = :p_s2,
       SCORE_COMMUNICATION = :p_s3,
       COMMENT_TXT         = :p_comment,
       EVALUATED_AT        = SYSTIMESTAMP
 WHERE TASK_ID      = :p_task_id
   AND EVALUATOR_ID = :p_evaluator_id";

            const string sqlSelectId = @"
SELECT EVAL_ID
  FROM TASK_EVALUATION
 WHERE TASK_ID      = :p_task_id
   AND EVALUATOR_ID = :p_evaluator_id";

            var param = new
            {
                p_task_id = taskId,
                p_evaluator_id = req.EvaluatorId,
                p_s1 = req.ScoreQuality,
                p_s2 = req.ScoreTimeliness,
                p_s3 = req.ScoreCommunication,
                p_comment = (object)req.CommentTxt ?? DBNull.Value
            };

            // 1) 먼저 UPDATE 시도
            var updated = await conn.ExecuteAsync(sqlUpdate, param);
            if (updated > 0)
            {
                return await conn.ExecuteScalarAsync<decimal>(sqlSelectId, new
                {
                    p_task_id = taskId,
                    p_evaluator_id = req.EvaluatorId
                });
            }

            // 2) 없으면 INSERT (RETURNING)
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.BindByName = true;
                cmd.CommandText = @"
INSERT INTO TASK_EVALUATION
(
  TASK_ID, EVALUATOR_ID,
  SCORE_QUALITY, SCORE_TIMELINESS, SCORE_COMMUNICATION,
  COMMENT_TXT
)
VALUES
(
  :p_task_id, :p_evaluator_id,
  :p_s1, :p_s2, :p_s3,
  :p_comment
)
RETURNING EVAL_ID INTO :p_eval_id";

                cmd.Parameters.Add(new OracleParameter("p_task_id", taskId));
                cmd.Parameters.Add(new OracleParameter("p_evaluator_id", req.EvaluatorId));
                cmd.Parameters.Add(new OracleParameter("p_s1", req.ScoreQuality));
                cmd.Parameters.Add(new OracleParameter("p_s2", req.ScoreTimeliness));
                cmd.Parameters.Add(new OracleParameter("p_s3", req.ScoreCommunication));
                cmd.Parameters.Add(new OracleParameter("p_comment", (object)req.CommentTxt ?? DBNull.Value));

                var outParam = new OracleParameter("p_eval_id", OracleDbType.Decimal)
                {
                    Direction = System.Data.ParameterDirection.Output
                };
                cmd.Parameters.Add(outParam);

                await cmd.ExecuteNonQueryAsync();
                return Convert.ToDecimal(outParam.Value.ToString());
            }
            catch (OracleException ex) when (ex.Number == 1)
            {
                // 동시성 등으로 INSERT가 밀린 경우(이미 생김) → UPDATE 후 ID 조회
                await conn.ExecuteAsync(sqlUpdate, param);
                return await conn.ExecuteScalarAsync<decimal>(sqlSelectId, new
                {
                    p_task_id = taskId,
                    p_evaluator_id = req.EvaluatorId
                });
            }
        }
    }
}