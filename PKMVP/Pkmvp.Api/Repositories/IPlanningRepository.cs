using System.Collections.Generic;
using System.Threading.Tasks;
using Pkmvp.Api.Models;

namespace Pkmvp.Api.Repositories
{
    public interface IPlanningRepository
    {
        Task<IReadOnlyList<ProjectItem>> ListProjectsAsync();
        Task<ProjectItem> GetProjectAsync(decimal projectId);
        Task<decimal> CreateProjectAsync(CreateProjectRequest req);

        Task<IReadOnlyList<BoardItem>> ListBoardsByProjectAsync(decimal projectId);
        Task<BoardItem> GetBoardAsync(decimal boardId);
        Task<decimal> CreateBoardAsync(decimal projectId, CreateBoardRequest req);

        Task<IReadOnlyList<SprintItem>> ListSprintsByBoardAsync(decimal boardId);
        Task<SprintItem> GetSprintAsync(decimal sprintId);
        Task<decimal> CreateSprintAsync(decimal boardId, CreateSprintRequest req);
        Task<bool> UpdateSprintStatusAsync(decimal sprintId, string status);

        Task<IReadOnlyList<BoardIssueItem>> ListBoardIssuesAsync(decimal boardId, decimal? sprintId, string status);
        Task<bool> PlanIssueAsync(decimal boardId, decimal taskId, decimal? sprintId);
    }
}
