using System.Text.Json.Serialization;

using BenefitsIntelligence.Api.Endpoints;
using BenefitsIntelligence.Application.Policies;
using BenefitsIntelligence.Application.Processing;
using BenefitsIntelligence.Domain.Policies;
using BenefitsIntelligence.Infrastructure;
using BenefitsIntelligence.Infrastructure.Persistence;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(new ReviewPolicy(
    builder.Configuration.GetValue("Review:ConfidenceThreshold", ReviewPolicy.DefaultConfidenceThreshold)));
builder.Services.AddScoped<PolicyUploadService>();
builder.Services.AddScoped<ProcessingResultHandler>();

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddDocumentStorage(builder.Configuration);
builder.Services.AddRabbitMqMessaging(builder.Configuration);

var app = builder.Build();

if (app.Configuration.GetValue<bool>(DatabaseMigration.MigrateOnStartupKey))
{
    await app.Services.MigrateDatabaseAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/health");
app.MapPolicyEndpoints();

app.Run();
