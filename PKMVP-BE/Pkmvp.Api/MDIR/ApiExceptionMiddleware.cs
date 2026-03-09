using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Pkmvp.Api
{
    public class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                await WriteJson(context, StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                await WriteJson(context, StatusCodes.Status404NotFound, ex.Message);
            }
            catch (ArgumentException ex)
            {
                await WriteJson(context, StatusCodes.Status400BadRequest, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                await WriteJson(context, StatusCodes.Status400BadRequest, ex.Message);
            }
            catch (Exception)
            {
                await WriteJson(context, StatusCodes.Status500InternalServerError, "Internal server error.");
            }
        }

        private static async Task WriteJson(HttpContext context, int statusCode, string message)
        {
            if (context.Response.HasStarted)
                throw new InvalidOperationException("The response has already started.");

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";

            var body = JsonSerializer.Serialize(new { message = message });
            await context.Response.WriteAsync(body);
        }
    }
}
