using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Pkmvp.Api.Filters
{
    /// <summary>
    /// Reject specific JSON properties in request body (defense against client-injected token fields).
    /// Runs before model binding and rewinds the body stream safely.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RejectJsonPropsAttribute : Attribute, IAsyncResourceFilter
    {
        private readonly string[] _props;

        public RejectJsonPropsAttribute(params string[] props)
        {
            _props = props ?? Array.Empty<string>();
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var req = context.HttpContext.Request;

            if (req.ContentType == null ||
                !req.ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            req.EnableBuffering();

            // ALWAYS rewind before read (ModelBinding might have touched it in some hosts/pipelines)
            if (req.Body.CanSeek) req.Body.Position = 0;

            string body;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync();
            }

            // Rewind again so MVC can read it
            if (req.Body.CanSeek) req.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(body))
            {
                await next();
                return;
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in _props)
                    {
                        if (doc.RootElement.EnumerateObject().Any(x =>
                            x.NameEquals(p) || x.Name.Equals(p, StringComparison.OrdinalIgnoreCase)))
                        {
                            context.Result = new BadRequestObjectResult(new
                            {
                                message = $"Token-injected field must not be provided: {p}"
                            });
                            return;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                context.Result = new BadRequestObjectResult(new { message = "Invalid JSON body." });
                return;
            }

            await next();
        }
    }
}
