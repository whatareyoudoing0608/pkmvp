using System.Collections.Generic;
using System.Threading.Tasks;
using Pkmvp.Api.Models;

namespace Pkmvp.Api.Repositories
{
    public interface INotificationRepository
    {
        Task<IReadOnlyList<NotificationItem>> ListForUserAsync(long userId, bool unreadOnly, int limit);
        Task<int> GetUnreadCountAsync(long userId);
        Task MarkReadAsync(long userId, decimal notificationId);
        Task<int> MarkAllReadAsync(long userId);
        Task CreateAsync(long userId, string type, string title, string message, string targetType, decimal? targetId);
        Task CreateForUsersAsync(IEnumerable<long> userIds, string type, string title, string message, string targetType, decimal? targetId);
    }
}
