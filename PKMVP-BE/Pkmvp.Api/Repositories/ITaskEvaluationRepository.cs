using System.Collections.Generic;
using System.Threading.Tasks;
using Pkmvp.Api.Models;

namespace Pkmvp.Api.Repositories
{
    public interface ITaskEvaluationRepository
    {
        Task<IReadOnlyList<TaskEvaluationItem>> GetByTaskIdAsync(decimal taskId);
        Task<decimal> CreateAsync(decimal taskId, CreateTaskEvaluationRequest req);
    }
}