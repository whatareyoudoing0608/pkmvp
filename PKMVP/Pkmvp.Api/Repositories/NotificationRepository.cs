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
    public class NotificationRepository : INotificationRepository
    {
        private readonly string _cs;

        public NotificationRepository(IConfiguration cfg)
        {
            _cs = cfg.GetConnectionString("Oracle");
        }

        public async Task<IReadOnlyList<NotificationItem>> ListForUserAsync(long userId, bool unreadOnly, int limit)
        {
            const string sql = @"
SELECT *
FROM (
    SELECT
        NOTIFICATION_ID AS NotificationId,
        USER_ID         AS UserId,
        TYPE            AS Type,
        TITLE           AS Title,
        MESSAGE         AS Message,
        TARGET_TYPE     AS TargetType,
        TARGET_ID       AS TargetId,
        IS_READ         AS IsRead,
        CREATED_AT      AS CreatedAt
    FROM PKMVP.NOTIFICATION
    WHERE USER_ID = :p_user_id
      AND (:p_unread_only = 0 OR IS_READ = 'N')
    ORDER BY CREATED_AT DESC, NOTIFICATION_ID DESC
)
WHERE ROWNUM <= :p_limit";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            var rows = await conn.QueryAsync<NotificationItem>(sql, new
            {
                p_user_id = userId,
                p_unread_only = unreadOnly ? 1 : 0,
                p_limit = limit
            });

            return rows.ToList();
        }

        public async Task<int> GetUnreadCountAsync(long userId)
        {
            const string sql = @"
SELECT COUNT(*)
  FROM PKMVP.NOTIFICATION
 WHERE USER_ID = :p_user_id
   AND IS_READ = 'N'";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            return await conn.ExecuteScalarAsync<int>(sql, new { p_user_id = userId });
        }

        public async Task MarkReadAsync(long userId, decimal notificationId)
        {
            const string sql = @"
UPDATE PKMVP.NOTIFICATION
   SET IS_READ = 'Y'
 WHERE USER_ID = :p_user_id
   AND NOTIFICATION_ID = :p_notification_id";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            await conn.ExecuteAsync(sql, new
            {
                p_user_id = userId,
                p_notification_id = notificationId
            });
        }

        public async Task<int> MarkAllReadAsync(long userId)
        {
            const string sql = @"
UPDATE PKMVP.NOTIFICATION
   SET IS_READ = 'Y'
 WHERE USER_ID = :p_user_id
   AND IS_READ = 'N'";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            return await conn.ExecuteAsync(sql, new { p_user_id = userId });
        }

        public async Task CreateAsync(long userId, string type, string title, string message, string targetType, decimal? targetId)
        {
            const string nextIdSql = "SELECT PKMVP.SEQ_NOTIFICATION.NEXTVAL FROM DUAL";
            const string insertSql = @"
INSERT INTO PKMVP.NOTIFICATION
(
    NOTIFICATION_ID,
    USER_ID,
    TYPE,
    TITLE,
    MESSAGE,
    TARGET_TYPE,
    TARGET_ID,
    IS_READ,
    CREATED_AT
)
VALUES
(
    :p_notification_id,
    :p_user_id,
    :p_type,
    :p_title,
    :p_message,
    :p_target_type,
    :p_target_id,
    'N',
    SYSDATE
)";

            using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            var notificationId = await conn.ExecuteScalarAsync<decimal>(nextIdSql);

            await conn.ExecuteAsync(insertSql, new
            {
                p_notification_id = notificationId,
                p_user_id = userId,
                p_type = type,
                p_title = title,
                p_message = (object)message ?? DBNull.Value,
                p_target_type = (object)targetType ?? DBNull.Value,
                p_target_id = (object)targetId ?? DBNull.Value
            });
        }

        public async Task CreateForUsersAsync(IEnumerable<long> userIds, string type, string title, string message, string targetType, decimal? targetId)
        {
            var targets = (userIds ?? Array.Empty<long>()).Distinct().ToList();
            if (targets.Count == 0)
                return;

            foreach (var userId in targets)
            {
                await CreateAsync(userId, type, title, message, targetType, targetId);
            }
        }
    }
}
