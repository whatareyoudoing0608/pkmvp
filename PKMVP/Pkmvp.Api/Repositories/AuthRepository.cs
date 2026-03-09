using System;
using Dapper;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace Pkmvp.Api.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IConfiguration _config;

        public AuthRepository(IConfiguration config)
        {
            _config = config;
        }

        private OracleConnection OpenConn()
        {
            var conn = new OracleConnection(_config.GetConnectionString("Oracle"));
            conn.Open();
            return conn;
        }

        public void InsertRefreshToken(long userId, string tokenHash, DateTime expiresAtUtc)
        {
            using (var conn = OpenConn())
            {
                conn.Execute(@"
INSERT INTO PKMVP.AUTH_REFRESH_TOKEN (USER_ID, TOKEN_HASH, EXPIRES_AT, REVOKED_YN, CREATED_AT)
VALUES (:USER_ID, :TOKEN_HASH, :EXPIRES_AT, 'N', SYSDATE)",
                    new
                    {
                        USER_ID = userId,
                        TOKEN_HASH = tokenHash,
                        EXPIRES_AT = expiresAtUtc
                    });
            }
        }

        public RefreshTokenRow GetRefreshTokenByHash(string tokenHash)
        {
            using (var conn = OpenConn())
            {
                return conn.QueryFirstOrDefault<RefreshTokenRow>(@"
SELECT TOKEN_ID, USER_ID, TOKEN_HASH, EXPIRES_AT, REVOKED_YN
  FROM PKMVP.AUTH_REFRESH_TOKEN
 WHERE TOKEN_HASH = :TOKEN_HASH",
                    new { TOKEN_HASH = tokenHash });
            }
        }

        public void RevokeAndReplace(string oldHash, string newHash)
        {
            using (var conn = OpenConn())
            {
                conn.Execute(@"
UPDATE PKMVP.AUTH_REFRESH_TOKEN
   SET REVOKED_YN = 'Y',
       REVOKED_AT = SYSDATE,
       REPLACED_BY_HASH = :NEW_HASH
 WHERE TOKEN_HASH = :OLD_HASH
   AND NVL(REVOKED_YN,'N') = 'N'",
                    new { OLD_HASH = oldHash, NEW_HASH = newHash });
            }
        }
    }
}