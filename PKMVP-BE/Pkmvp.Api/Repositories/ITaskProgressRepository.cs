using System.Collections.Generic;
using System.Threading.Tasks;
using Pkmvp.Api.Models;

namespace Pkmvp.Api.Repositories
{
    public interface ITaskProgressRepository
    {
        Task<IReadOnlyList<TaskProgressItem>> GetByTaskIdAsync(decimal taskId);
        Task<decimal> CreateAsync(decimal taskId, CreateTaskProgressRequest req);
    }
}