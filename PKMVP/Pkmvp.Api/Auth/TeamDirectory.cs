using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Pkmvp.Api.Auth
{
    /// <summary>
    /// Resolve team membership from configuration section: Auth:Users
    /// Example:
    /// Auth:Users: [{ UserId: "1", Role:"ADMIN", TeamId:"IT" }, ...]
    /// </summary>
    public sealed class TeamDirectory : ITeamDirectory
    {
        private readonly IConfiguration _cfg;
        private readonly object _lock = new object();

        private Dictionary<string, HashSet<long>> _teamToUsers;

        public TeamDirectory(IConfiguration cfg)
        {
            _cfg = cfg;
        }

        private void EnsureLoaded()
        {
            if (_teamToUsers != null) return;

            lock (_lock)
            {
                if (_teamToUsers != null) return;

                var dict = new Dictionary<string, HashSet<long>>(StringComparer.OrdinalIgnoreCase);

                var users = _cfg.GetSection("Auth:Users").GetChildren();
                foreach (var u in users)
                {
                    var uidStr = u["UserId"];
                    var teamId = u["TeamId"];

                    if (string.IsNullOrWhiteSpace(uidStr) || string.IsNullOrWhiteSpace(teamId))
                        continue;

                    if (!long.TryParse(uidStr, out var uid))
                        continue;

                    if (!dict.TryGetValue(teamId, out var set))
                    {
                        set = new HashSet<long>();
                        dict[teamId] = set;
                    }
                    set.Add(uid);
                }

                _teamToUsers = dict;
            }
        }

        public bool IsUserInTeam(long userId, string teamId)
        {
            if (string.IsNullOrWhiteSpace(teamId)) return false;
            EnsureLoaded();

            return _teamToUsers.TryGetValue(teamId, out var set) && set.Contains(userId);
        }

        public IReadOnlyList<long> GetTeamUserIds(string teamId)
        {
            if (string.IsNullOrWhiteSpace(teamId)) return Array.Empty<long>();
            EnsureLoaded();

            if (_teamToUsers.TryGetValue(teamId, out var set))
                return set.ToList();

            return Array.Empty<long>();
        }
    }
}
