using FoleoTrader;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.Configure<FoleoTraderOptions>(builder.Configuration.GetSection(FoleoTraderOptions.SectionName));
builder.Services.AddSingleton<FoleoTraderMessageMonitor>();
builder.Services.AddSingleton<FoleoTraderFixApplication>();
builder.Services.AddHostedService<FoleoTraderFixAcceptorHostedService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/FoleoTrader/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=FoleoTrader}/{action=Index}/{id?}");

app.Run();
