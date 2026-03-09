using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkmvp.Api.Auth;
using Pkmvp.Api.Repositories;

namespace Pkmvp.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationRepository _notifications;
        private readonly ICurrentUserAccessor _cu;

        public NotificationsController(INotificationRepository notifications, ICurrentUserAccessor cu)
        {
            _notifications = notifications;
            _cu = cu;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] bool unreadOnly = false, [FromQuery] int limit = 50)
        {
            if (limit < 1) limit = 1;
            if (limit > 200) limit = 200;

            var me = _cu.Get();
            var rows = await _notifications.ListForUserAsync(me.UserId, unreadOnly, limit);
            return Ok(rows);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
        {
            var me = _cu.Get();
            var count = await _notifications.GetUnreadCountAsync(me.UserId);
            return Ok(new { count });
        }

        [HttpPost("{notificationId}/read")]
        public async Task<IActionResult> MarkRead(decimal notificationId)
        {
            var me = _cu.Get();
            await _notifications.MarkReadAsync(me.UserId, notificationId);
            return Ok(new { ok = true });
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var me = _cu.Get();
            var affected = await _notifications.MarkAllReadAsync(me.UserId);
            return Ok(new { ok = true, updated = affected });
        }
    }
}
