using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Pkmvp.Api.Models;

namespace Pkmvp.Api.Swagger
{
    /// <summary>
    /// Hides token-injected IDs from Swagger request schemas.
    /// (We only remove from specific *request* DTOs to avoid hiding response fields.)
    /// </summary>
    public sealed class HideTokenInjectedIdsSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema?.Properties == null) return;

            static void Remove(OpenApiSchema s, params string[] names)
            {
                foreach (var n in names)
                {
                    if (s.Properties.ContainsKey(n)) s.Properties.Remove(n);
                }
            }

            // Task domain requests
            if (context.Type == typeof(CreateTaskRequest))
                Remove(schema, "reporterId", "ReporterId");

            if (context.Type == typeof(CreateTaskProgressRequest))
                Remove(schema, "authorId", "AuthorId");

            if (context.Type == typeof(CreateTaskEvaluationRequest))
                Remove(schema, "evaluatorId", "EvaluatorId");

            // DailyWorklog domain requests (types exist in Models; remove only if present)
            if (context.Type == typeof(ApproveRejectRequest))
                Remove(schema, "evaluatorId", "EvaluatorId", "authorId", "AuthorId", "actorId", "ActorId");

            if (context.Type == typeof(CreateDailyWorklogItemRequest))
                Remove(schema, "authorId", "AuthorId", "actorId", "ActorId", "reporterId", "ReporterId");
        }
    }
}
