using System.Collections.Generic;
using System.Threading.Tasks;
using Pkmvp.Api.Models;

namespace Pkmvp.Api.Repositories
{
    public interface ITaskCommentRepository
    {
        Task<IReadOnlyList<TaskCommentItem>> ListByTaskIdAsync(decimal taskId);
        Task<TaskCommentItem> GetByIdAsync(decimal taskId, decimal commentId);
        Task<decimal> CreateAsync(decimal taskId, decimal? parentCommentId, long authorId, string content);
        Task<bool> UpdateAsync(decimal taskId, decimal commentId, long actorId, string content, bool isPrivileged);
        Task<bool> SoftDeleteAsync(decimal taskId, decimal commentId, long actorId, bool isPrivileged);
    }
}
