using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

var api = app.MapGroup("api")
    .WithTags("Fun facts about cats");

api.MapGet("/health", () => Results.Ok("Healthy"))
    .WithTags("Health");

var facts = api.MapGroup("facts")
    .WithTags("Facts");

app.Run();