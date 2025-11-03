using System.Net;
using System.Text.Json;

namespace ShowroomCar.Api.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        { _next = next; _logger = logger; }

        public async Task Invoke(HttpContext ctx)
        {
            try { await _next(ctx); }
            catch (Exception ex)
            {
                var traceId = ctx.TraceIdentifier;
                _logger.LogError(ex, "Unhandled error, traceId={TraceId}", traceId);
                ctx.Response.ContentType = "application/json";
                ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                var payload = new { error = "InternalServerError", traceId };
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload));
            }
        }
    }

    public static class ErrorHandlingExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app) =>
            app.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
