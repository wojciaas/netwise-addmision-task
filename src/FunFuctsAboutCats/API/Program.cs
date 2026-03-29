using API;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .WriteTo.File(
        path: "logs/startup-logs-.json",
        rollingInterval: RollingInterval.Day, 
        formatter: new CompactJsonFormatter()
    )   
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting web host");
    
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));

    builder.Services.AddOpenApi();
    
    builder.Services.Configure<FactFileRepositoryOptions>(builder.Configuration.GetSection(FactFileRepositoryOptions.SectionName));

    builder.Services.AddScoped<IFactRepository, FactFileRepository>();

    var app = builder.Build();

    app.MapOpenApi();
    app.MapScalarApiReference();
    
    app.UseSerilogRequestLogging(options =>
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
    
    app.UseHttpsRedirection();

    var api = app.MapGroup("api")
        .WithTags("Fun facts about cats");

    api.MapGet("/health", () => Results.Ok("Healthy"))
        .WithTags("Health");
    
    var facts = api.MapGroup("facts")
        .WithTags("Facts");
    
    facts.MapPost("", async (IFactRepository factRepository, CancellationToken cancellationToken) =>
    {
        var client = new HttpClient();
        var response = await client.GetAsync("https://catfact.ninja/fact", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return Results.Problem(
                "Failed to fetch a cat fact from the external API.", 
                statusCode: StatusCodes.Status503ServiceUnavailable
            );
        }
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken: cancellationToken);
        await factRepository.AddFactAsync(content, cancellationToken);
        
        return Results.Created("/api/facts", content);
    })
    .WithName("AddFact")
    .WithSummary("Add a random cat fact to the database")
    .WithDescription("Fetches a random cat fact from the external API \"https://catfact.ninja/fact\" and adds it to the database.")
    .Produces(StatusCodes.Status201Created)
    .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)
    .WithTags("Facts");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Server terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}