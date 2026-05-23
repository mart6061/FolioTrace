using API;
using Repository;
using Scalar.AspNetCore;
using Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<ApiVersionInfo>();
builder.Services.AddSingleton<BuildCoordinator>();
builder.Services.AddSingleton(
    builder.Configuration
        .GetSection(AggregateMaintenanceOptions.SectionName)
        .Get<AggregateMaintenanceOptions>() ?? new AggregateMaintenanceOptions());
builder.Services.AddFolioTraceRepository(builder.Configuration);
builder.Services.AddFolioTraceServices();
builder.Services.AddHostedService<AggregateMaintenanceHostedService>();

var app = builder.Build();

app.UsePathBase("/API");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseHttpsRedirection();
}

app.UseApiExchangeCapture();
app.UseApiUnhandledExceptionLogging();

app.MapFolioTraceApi();

app.Run();
