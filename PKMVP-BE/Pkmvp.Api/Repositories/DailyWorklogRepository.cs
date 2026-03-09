using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using Dapper.Oracle;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Pkmvp.Api.Auth;

namespace Pkmvp.Api.Repositories
{
    public class DailyWorklogRepository : IDailyWorklogRepository
    {
        private readonly IConfiguration _config;
        private readonly ITeamDirectory _teams;

        public DailyWorklogRepository(IConfiguration config, ITeamDirectory teams)
        {
            _config = config;
            _teams = teams;
        }

        private OracleConnection OpenConn()
        {
            var conn = new OracleConnection(_config.GetConnectionString("Oracle"));
            conn.Open();
            return conn;
        }

        public long CreateHeader(DateTime workDate, long reporterId, string reporterTeamId, long authorId, string summary)
        {
            using (var conn = OpenConn())
            {
                var p = new OracleDynamicParameters();
                p.Add("WORK_DATE", workDate.Date, OracleMappingType.Date, ParameterDirection.Input);
                p.Add("REPORTER_ID", reporterId, OracleMappingType.Int64, ParameterDirection.Input);
                p.Add("REPORTER_TEAM_ID", reporterTeamId, OracleMappingType.Varchar2, ParameterDirection.Input);
                p.Add("AUTHOR_ID", authorId, OracleMappingType.Int64, ParameterDirection.Input);
                p.Add("SUMMARY", summary, OracleMappingType.Varchar2, ParameterDirection.Input);
                p.Add("CREATED_BY", authorId, OracleMappingType.Int64, ParameterDirection.Input);
                p.Add("NEW_ID", dbType: OracleMappingType.Decimal, direction: ParameterDirection.Output);

                conn.Execute(@"
                    INSERT INTO PKMVP.DAILY_WORKLOG
                    ( WORK_DATE, REPORTER_ID, REPORTER_TEAM_ID, AUTHOR_ID, STATUS, SUMMARY, CREATED_AT, CREATED_BY )
                    VALUES
                    ( :WORK_DATE, :REPORTER_ID, :REPORTER_TEAM_ID, :AUTHOR_ID, 'DRAFT', :SUMMARY, SYSDATE, :CREATED_BY )
                    RETURNING WORKLOG_ID INTO :NEW_ID", p);

                return (long)p.Get<decimal>("NEW_ID");
            }
        }

        public IEnumerable<dynamic> List(DateTime fromDate, DateTime toDate, string scope, long meUserId, string meTeamId)
        {
            using (var conn = OpenConn())
            {
                var sql = @"
                    SELECT WORKLOG_ID, WORK_DATE, REPORTER_ID, REPORTER_TEAM_ID, AUTHOR_ID, STATUS, SUMMARY, CREATED_AT, UPDATED_AT
                      FROM PKMVP.DAILY_WORKLOG
                     WHERE WORK_DATE BETWEEN :FROM_DATE AND :TO_DATE ";

                var p = new DynamicParameters();
                p.Add("FROM_DATE", fromDate.Date);
                p.Add("TO_DATE", toDate.Date);

                if (scope == "mine")
                {
                    sql += " AND REPORTER_ID = :ME_USER_ID ";
                    p.Add("ME_USER_ID", meUserId);
                }
                else if (scope == "team")
                {
                    var ids = _teams.GetTeamUserIds(meTeamId);
                    if (ids == null || ids.Count == 0)
                        return Array.Empty<object>();

                    var placeholders = new List<string>();
                    for (var i = 0; i < ids.Count; i++)
                    {
                        var name = "TEAM_UID_" + i;
                        placeholders.Add(":" + name);
                        p.Add(name, ids[i]);
                    }

                    sql += " AND REPORTER_ID IN (" + string.Join(", ", placeholders) + ") ";
                }

                sql += " ORDER BY WORK_DATE DESC, WORKLOG_ID DESC ";

                return conn.Query(sql, p);
            }
        }

        public dynamic GetHeader(long worklogId)
        {
            using (var conn = OpenConn())
            {
                return conn.QueryFirstOrDefault(@"
                    SELECT WORKLOG_ID, WORK_DATE, REPORTER_ID, REPORTER_TEAM_ID, AUTHOR_ID, STATUS, SUMMARY
                      FROM PKMVP.DAILY_WORKLOG
                     WHERE WORKLOG_ID = :WORKLOG_ID",
                new { WORKLOG_ID = worklogId });
            }
        }

        public void Submit(long worklogId, long actorId)
        {
            using (var conn = OpenConn())
            {
                conn.Execute(@"
                    UPDATE PKMVP.DAILY_WORKLOG
                       SET STATUS = 'SUBMITTED',
                           UPDATED_AT = SYSDATE,
                           UPDATED_BY = :ACTOR_ID
                     WHERE WORKLOG_ID = :WORKLOG_ID
                       AND STATUS = 'DRAFT'",
                new { WORKLOG_ID = worklogId, ACTOR_ID = actorId });

                conn.Execute(@"
                    INSERT INTO PKMVP.DAILY_WORKLOG_EVAL (WORKLOG_ID, ACTION, EVALUATOR_ID, CREATED_AT)
                    VALUES (:WORKLOG_ID, 'SUBMIT', :EVALUATOR_ID, SYSDATE)",
                new { WORKLOG_ID = worklogId, EVALUATOR_ID = actorId });
            }
        }

        public void Approve(long worklogId, long evaluatorId, int? score, string commentTxt)
        {
            using (var conn = OpenConn())
            {
                conn.Execute(@"
                    UPDATE PKMVP.DAILY_WORKLOG
                       SET STATUS = 'APPROVED',
                           UPDATED_AT = SYSDATE,
                           UPDATED_BY = :EVALUATOR_ID
                     WHERE WORKLOG_ID = :WORKLOG_ID
                       AND STATUS = 'SUBMITTED'",
                new { WORKLOG_ID = worklogId, EVALUATOR_ID = evaluatorId });

                conn.Execute(@"
                    INSERT INTO PKMVP.DAILY_WORKLOG_EVAL (WORKLOG_ID, ACTION, SCORE, COMMENT_TXT, EVALUATOR_ID, CREATED_AT)
                    VALUES (:WORKLOG_ID, 'APPROVE', :SCORE, :COMMENT_TXT, :EVALUATOR_ID, SYSDATE)",
                new { WORKLOG_ID = worklogId, SCORE = score, COMMENT_TXT = commentTxt, EVALUATOR_ID = evaluatorId });
            }
        }

        public void Reject(long worklogId, long evaluatorId, int? score, string commentTxt)
        {
            using (var conn = OpenConn())
            {
                conn.Execute(@"
                    UPDATE PKMVP.DAILY_WORKLOG
                       SET STATUS = 'REJECTED',
                           UPDATED_AT = SYSDATE,
                           UPDATED_BY = :EVALUATOR_ID
                     WHERE WORKLOG_ID = :WORKLOG_ID
                       AND STATUS = 'SUBMITTED'",
                new { WORKLOG_ID = worklogId, EVALUATOR_ID = evaluatorId });

                conn.Execute(@"
                    INSERT INTO PKMVP.DAILY_WORKLOG_EVAL (WORKLOG_ID, ACTION, SCORE, COMMENT_TXT, EVALUATOR_ID, CREATED_AT)
                    VALUES (:WORKLOG_ID, 'REJECT', :SCORE, :COMMENT_TXT, :EVALUATOR_ID, SYSDATE)",
                new { WORKLOG_ID = worklogId, SCORE = score, COMMENT_TXT = commentTxt, EVALUATOR_ID = evaluatorId });
            }
        }

        public IDailyWorklogRepository.DailyWorklogHeaderRow GetHeaderAuth(long worklogId)
        {
            using (var conn = OpenConn())
            {
                return conn.QueryFirstOrDefault<IDailyWorklogRepository.DailyWorklogHeaderRow>(@"
                    SELECT
                        WORKLOG_ID       AS WorklogId,
                        REPORTER_ID      AS ReporterId,
                        REPORTER_TEAM_ID AS ReporterTeamId,
                        STATUS           AS Status
                      FROM PKMVP.DAILY_WORKLOG
                     WHERE WORKLOG_ID = :WORKLOG_ID",
                new { WORKLOG_ID = worklogId });
            }
        }

        public long AddItem(long worklogId, int seq, string title, string description, int? spentMinutes, int? progressPct, long actorId)
        {
            using (var conn = OpenConn())
            {
                var p = new OracleDynamicParameters();
                p.Add("WORKLOG_ID", worklogId, OracleMappingType.Int64, ParameterDirection.Input);
                p.Add("SEQ", seq, OracleMappingType.Int32, ParameterDirection.Input);
                p.Add("TITLE", title, OracleMappingType.Varchar2, ParameterDirection.Input);
                p.Add("DESCRIPTION", description, OracleMappingType.Varchar2, ParameterDirection.Input);
                p.Add("SPENT_MINUTES", spentMinutes, OracleMappingType.Int32, ParameterDirection.Input);
                p.Add("PROGRESS_PCT", progressPct, OracleMappingType.Int32, ParameterDirection.Input);
                p.Add("CREATED_BY", actorId, OracleMappingType.Int64, ParameterDirection.Input);
                p.Add("NEW_ID", dbType: OracleMappingType.Decimal, direction: ParameterDirection.Output);

                conn.Execute(@"
                    INSERT INTO PKMVP.DAILY_WORKLOG_ITEM
                    ( WORKLOG_ID, SEQ, TITLE, DESCRIPTION, SPENT_MINUTES, PROGRESS_PCT, CREATED_AT, CREATED_BY )
                    VALUES
                    ( :WORKLOG_ID, :SEQ, :TITLE, :DESCRIPTION, :SPENT_MINUTES, :PROGRESS_PCT, SYSDATE, :CREATED_BY )
                    RETURNING ITEM_ID INTO :NEW_ID", p);

                return (long)p.Get<decimal>("NEW_ID");
            }
        }
    }
}
