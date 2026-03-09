using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Pkmvp.Api.Auth
{
    public sealed class HttpContextCurrentUserAccessor : ICurrentUserAccessor
    {
        private readonly IHttpContextAccessor _http;

        public HttpContextCurrentUserAccessor(IHttpContextAccessor http)
        {
            _http = http;
        }

        public CurrentUser Get()
        {
            var user = _http.HttpContext != null ? _http.HttpContext.User : null;

            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
                throw new UnauthorizedAccessException("Unauthenticated.");

            var uidStr =
                (user.FindFirst(AuthClaimTypes.UserId) != null ? user.FindFirst(AuthClaimTypes.UserId).Value : null)
                ?? (user.FindFirst(ClaimTypes.NameIdentifier) != null ? user.FindFirst(ClaimTypes.NameIdentifier).Value : null)
                ?? (user.FindFirst("sub") != null ? user.FindFirst("sub").Value : null);

            long uid;
            if (string.IsNullOrWhiteSpace(uidStr) || !long.TryParse(uidStr, out uid))
                throw new UnauthorizedAccessException("Invalid user id claim.");

            var roleStr =
                (user.FindFirst(AuthClaimTypes.Role) != null ? user.FindFirst(AuthClaimTypes.Role).Value : null)
                ?? (user.FindFirst(ClaimTypes.Role) != null ? user.FindFirst(ClaimTypes.Role).Value : null)
                ?? "USER";

            UserRole role;
            if (!Enum.TryParse(roleStr, true, out role))
                role = UserRole.USER;

            var teamId = user.FindFirst(AuthClaimTypes.TeamId) != null ? user.FindFirst(AuthClaimTypes.TeamId).Value : null;

            return new CurrentUser(uid, role, teamId);
        }
    }
}