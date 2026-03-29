using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Events;

namespace API;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder RegisterRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            };

            options.GetLevel = (httpContext, elapsed, _) =>
            {
                if (httpContext.Request.Path.StartsWithSegments("/api/health"))
                {
                    return LogEventLevel.Verbose;
                }

                return elapsed > 500
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;
            };
        });
    }
    
    public static IApplicationBuilder RegisterExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseExceptionHandler(options =>
        {
            options.Run(async context =>
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (exceptionFeature?.Error is not null)
                {
                    Log.Error(exceptionFeature.Error, "An unhandled exception occurred while processing the request.");
                    var errorResponse = new ProblemDetails
                    {
                        Status = context.Response.StatusCode,
                        Title = "An unexpected error occurred.",
                        Instance = context.Request.Path,
                        Detail = "Please try again later or contact support if the issue persists."
                    };
                    await context.Response.WriteAsJsonAsync(errorResponse);
                }
            });
        });
    }
}