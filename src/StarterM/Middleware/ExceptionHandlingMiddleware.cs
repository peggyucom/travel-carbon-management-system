using System.Net;
using System.Text.Json;

namespace StarterM.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "未處理的例外: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var statusCode = exception switch
            {
                InvalidOperationException => HttpStatusCode.BadRequest,
                UnauthorizedAccessException => HttpStatusCode.Forbidden,
                KeyNotFoundException => HttpStatusCode.NotFound,
                _ => HttpStatusCode.InternalServerError
            };

            // 如果是 API 請求，回傳 JSON
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = (int)statusCode;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    success = false,
                    error = new
                    {
                        code = statusCode.ToString(),
                        message = _env.IsDevelopment() ? exception.Message : "伺服器發生錯誤，請稍後再試"
                    }
                };

                await context.Response.WriteAsJsonAsync(response);
            }
            else
            {
                // MVC 請求導向錯誤頁面
                context.Response.Redirect($"/Home/Error?code={(int)statusCode}");
            }
        }
    }
}
