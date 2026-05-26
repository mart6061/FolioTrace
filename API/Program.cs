using API;
using Repository;
using Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "FolioTrace API",
        Version = "v1"
    });
    options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
});
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
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("v1/swagger.json", "FolioTrace API v1");
        options.RoutePrefix = "swagger";
    });
    app.UseHttpsRedirection();
}

app.UseApiExchangeCapture();
app.UseApiUnhandledExceptionLogging();

app.MapFolioTraceApi();

app.Run();
