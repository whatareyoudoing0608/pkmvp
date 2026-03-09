using System;

namespace Pkmvp.Api.Repositories
{
    public interface IAuthRepository
    {
        void InsertRefreshToken(long userId, string tokenHash, DateTime expiresAtUtc);
        RefreshTokenRow GetRefreshTokenByHash(string tokenHash);
        void RevokeAndReplace(string oldHash, string newHash);
    }

    public class RefreshTokenRow
    {
        public long Token_Id { get; set; }
        public long User_Id { get; set; }
        public string Token_Hash { get; set; }
        public DateTime Expires_At { get; set; }
        public string Revoked_Yn { get; set; }
    }
}