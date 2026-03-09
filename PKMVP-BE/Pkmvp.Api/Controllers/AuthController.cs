using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Pkmvp.Api.Auth;
using Pkmvp.Api.Models;
using Pkmvp.Api.Repositories;

namespace Pkmvp.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ITokenService _tokenService;
        private readonly IAuthRepository _authRepo;

        public AuthController(IConfiguration config, ITokenService tokenService, IAuthRepository authRepo)
        {
            _config = config;
            _tokenService = tokenService;
            _authRepo = authRepo;
        }

        [HttpPost("login")]
        public ActionResult<TokenResponse> Login([FromBody] LoginRequest req)
        {
            var users = _config.GetSection("Auth:Users").GetChildren()
                .Select(x => new
                {
                    LoginId = x["LoginId"],
                    Password = x["Password"],
                    UserId = long.Parse(x["UserId"]),
                    Role = x["Role"],
                    TeamId = x["TeamId"]
                })
                .ToList();

            var u = users.FirstOrDefault(x => x.LoginId == req.LoginId && x.Password == req.Password);
            if (u == null) return Unauthorized("Invalid credentials.");

            var accessMin = int.Parse(_config["Jwt:AccessTokenMinutes"] ?? "30");
            var refreshDays = int.Parse(_config["Jwt:RefreshTokenDays"] ?? "14");

            var accessExp = DateTime.UtcNow.AddMinutes(accessMin);
            var refreshExp = DateTime.UtcNow.AddDays(refreshDays);

            var accessToken = _tokenService.CreateAccessToken(u.UserId, u.Role, u.TeamId, accessExp);

            var refreshToken = NewRefreshToken();
            var refreshHash = Sha256Hex(refreshToken);

            _authRepo.InsertRefreshToken(u.UserId, refreshHash, refreshExp);

            return new TokenResponse
            {
                AccessToken = accessToken,
                AccessTokenExpiresAt = accessExp,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshExp
            };
        }

        [HttpPost("refresh")]
        public ActionResult<TokenResponse> Refresh([FromBody] RefreshRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.RefreshToken))
                return BadRequest("RefreshToken required.");

            var oldHash = Sha256Hex(req.RefreshToken);
            var row = _authRepo.GetRefreshTokenByHash(oldHash);

            if (row == null) return Unauthorized("Invalid refresh token.");
            if (string.Equals(row.Revoked_Yn, "Y", StringComparison.OrdinalIgnoreCase))
                return Unauthorized("Refresh token revoked.");
            if (row.Expires_At <= DateTime.UtcNow)
                return Unauthorized("Refresh token expired.");

            // 사용자 정보는 지금은 appsettings에서 다시 찾음(개발용). 이후 DB 사용자로 교체.
            var users = _config.GetSection("Auth:Users").GetChildren().Select(x => new
            {
                UserId = long.Parse(x["UserId"]),
                Role = x["Role"],
                TeamId = x["TeamId"]
            }).ToList();

            var u = users.FirstOrDefault(x => x.UserId == row.User_Id);
            if (u == null) return Unauthorized("User not found.");

            var accessMin = int.Parse(_config["Jwt:AccessTokenMinutes"] ?? "30");
            var refreshDays = int.Parse(_config["Jwt:RefreshTokenDays"] ?? "14");

            var accessExp = DateTime.UtcNow.AddMinutes(accessMin);
            var refreshExp = DateTime.UtcNow.AddDays(refreshDays);

            var accessToken = _tokenService.CreateAccessToken(u.UserId, u.Role, u.TeamId, accessExp);

            var newRefresh = NewRefreshToken();
            var newHash = Sha256Hex(newRefresh);

            _authRepo.RevokeAndReplace(oldHash, newHash);
            _authRepo.InsertRefreshToken(u.UserId, newHash, refreshExp);

            return new TokenResponse
            {
                AccessToken = accessToken,
                AccessTokenExpiresAt = accessExp,
                RefreshToken = newRefresh,
                RefreshTokenExpiresAt = refreshExp
            };
        }

        private static string NewRefreshToken()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static string Sha256Hex(string input)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++) sb.Append(bytes[i].ToString("x2"));
                return sb.ToString();
            }
        }
    }
}