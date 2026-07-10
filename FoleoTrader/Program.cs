using FoleoTrader;

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

app.MapPost("/trade-files", async (FolioTrace.Aggregates.TradeFileDeliveryRequest request, TradeFileSimulator simulator, CancellationToken cancellationToken) =>
{
    await simulator.ReceiveAsync(request, cancellationToken);
    return Results.Accepted();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=FoleoTrader}/{action=Index}/{id?}");

app.Run();
