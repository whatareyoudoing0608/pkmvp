using System.Collections.Generic;
using System.Threading.Tasks;
using Pkmvp.Api.Models;
using Pkmvp.Api.Auth;

namespace Pkmvp.Api.Repositories
{
    public interface ITaskRepository
    {
        Task<IReadOnlyList<TaskItem>> GetListAsync(string status, decimal? assigneeId);
        Task<TaskItem> GetByIdAsync(decimal taskId);

        Task<decimal> CreateAsync(CreateTaskRequest req);
        Task<bool> UpdateAsync(decimal taskId, UpdateTaskRequest req);
        Task<bool> UpdateStatusAsync(decimal taskId, string status, int? progressPct);
        Task<IReadOnlyList<TaskItem>> GetListScopedAsync(
                                                        string status,
                                                        decimal? assigneeId,
                                                        WorklogScope scope,
                                                        long myUserId,
                                                        IEnumerable<long> teamUserIds
);
    }
}