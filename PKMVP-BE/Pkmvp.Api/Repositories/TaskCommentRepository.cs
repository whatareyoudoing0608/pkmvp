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
    public class TaskCommentRepository : ITaskCommentRepository
    {
        private readonly string _cs;

        public TaskCommentRepository(IConfiguration cfg)
        {
            _cs = cfg.GetConnectionString("Oracle");
        }

        public async Task<IReadOnlyList<TaskCommentItem>> ListByTaskIdAsync(decimal taskId)
        {
            const string sql = @"
SELECT
    COMMENT_ID        AS CommentId,
    TASK_ID           AS TaskId,
    PARENT_COMMENT_ID AS ParentCommentId,
    AUTHOR_ID         AS AuthorId,
    CONTENT           AS Content,
    EDITED_YN         AS EditedYn,
    DELETED_YN        AS DeletedYn,
    CREATED_AT        AS CreatedAt,
    UPDATED_AT        AS UpdatedAt
FROM PKMVP.TASK_COMMENT
WHERE TASK_ID = :p_task_id
  AND DELETED_YN = 'N'
ORDER BY CREATED_AT ASC, COMMENT_ID ASC";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            var rows = await conn.QueryAsync<TaskCommentItem>(sql, new { p_task_id = taskId });
            return rows.ToList();
        }

        public async Task<TaskCommentItem> GetByIdAsync(decimal taskId, decimal commentId)
        {
            const string sql = @"
SELECT
    COMMENT_ID        AS CommentId,
    TASK_ID           AS TaskId,
    PARENT_COMMENT_ID AS ParentCommentId,
    AUTHOR_ID         AS AuthorId,
    CONTENT           AS Content,
    EDITED_YN         AS EditedYn,
    DELETED_YN        AS DeletedYn,
    CREATED_AT        AS CreatedAt,
    UPDATED_AT        AS UpdatedAt
FROM PKMVP.TASK_COMMENT
WHERE TASK_ID = :p_task_id
  AND COMMENT_ID = :p_comment_id
  AND DELETED_YN = 'N'";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            return await conn.QueryFirstOrDefaultAsync<TaskCommentItem>(sql, new
            {
                p_task_id = taskId,
                p_comment_id = commentId
            });
        }

        public async Task<decimal> CreateAsync(decimal taskId, decimal? parentCommentId, long authorId, string content)
        {
            const string nextIdSql = "SELECT PKMVP.SEQ_TASK_COMMENT.NEXTVAL FROM DUAL";
            const string insertSql = @"
INSERT INTO PKMVP.TASK_COMMENT
(
    COMMENT_ID,
    TASK_ID,
    PARENT_COMMENT_ID,
    AUTHOR_ID,
    CONTENT,
    EDITED_YN,
    DELETED_YN,
    CREATED_AT,
    UPDATED_AT
)
VALUES
(
    :p_comment_id,
    :p_task_id,
    :p_parent_comment_id,
    :p_author_id,
    :p_content,
    'N',
    'N',
    SYSDATE,
    SYSDATE
)";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            var commentId = await conn.ExecuteScalarAsync<decimal>(nextIdSql);
            await conn.ExecuteAsync(insertSql, new
            {
                p_comment_id = commentId,
                p_task_id = taskId,
                p_parent_comment_id = (object)parentCommentId ?? DBNull.Value,
                p_author_id = authorId,
                p_content = content
            });

            return commentId;
        }

        public async Task<bool> UpdateAsync(decimal taskId, decimal commentId, long actorId, string content, bool isPrivileged)
        {
            const string sql = @"
UPDATE PKMVP.TASK_COMMENT
   SET CONTENT    = :p_content,
       EDITED_YN  = 'Y',
       UPDATED_AT = SYSDATE
 WHERE TASK_ID    = :p_task_id
   AND COMMENT_ID = :p_comment_id
   AND DELETED_YN = 'N'
   AND (AUTHOR_ID = :p_actor_id OR :p_is_privileged = 1)";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            var affected = await conn.ExecuteAsync(sql, new
            {
                p_content = content,
                p_task_id = taskId,
                p_comment_id = commentId,
                p_actor_id = actorId,
                p_is_privileged = isPrivileged ? 1 : 0
            });

            return affected > 0;
        }

        public async Task<bool> SoftDeleteAsync(decimal taskId, decimal commentId, long actorId, bool isPrivileged)
        {
            const string sql = @"
UPDATE PKMVP.TASK_COMMENT
   SET DELETED_YN = 'Y',
       UPDATED_AT = SYSDATE
 WHERE TASK_ID    = :p_task_id
   AND COMMENT_ID = :p_comment_id
   AND DELETED_YN = 'N'
   AND (AUTHOR_ID = :p_actor_id OR :p_is_privileged = 1)";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            var affected = await conn.ExecuteAsync(sql, new
            {
                p_task_id = taskId,
                p_comment_id = commentId,
                p_actor_id = actorId,
                p_is_privileged = isPrivileged ? 1 : 0
            });

            return affected > 0;
        }
    }
}
