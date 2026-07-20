using FoleoTrader;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.Configure<FoleoTraderOptions>(builder.Configuration.GetSection(FoleoTraderOptions.SectionName));
builder.Services.AddSingleton<FoleoTraderMessageMonitor>();
builder.Services.AddSingleton<FoleoTraderFixApplication>();
builder.Services.AddHostedService<FoleoTraderFixAcceptorHostedService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TradeFileSimulator>();
builder.Services.AddHostedService<TradeFileConfirmationHostedService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/FoleoTrader/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapPost("/trade-files", async (HttpRequest request, TradeFileSimulator simulator, CancellationToken cancellationToken) =>
{
    var form = await request.ReadFormAsync(cancellationToken);
    var metadata = JsonSerializer.Deserialize<FolioTrace.Aggregates.TradeFileDeliveryMetadata>(form["metadata"].ToString())
        ?? throw new BadHttpRequestException("TradeFile metadata is required.");
    if (form.Files.GetFile("file") is null)
        throw new BadHttpRequestException("TradeFile content is required.");
    await simulator.ReceiveAsync(metadata, cancellationToken);
    return Results.Accepted();
}).DisableAntiforgery();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=FoleoTrader}/{action=Index}/{id?}");

app.Run();
