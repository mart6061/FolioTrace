using FoleoTrader.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoleoTrader.Controllers;

public sealed class FoleoTraderController(FoleoTraderMessageMonitor monitor) : Controller
{
    public IActionResult Index() => View(new FoleoTraderMonitorViewModel(monitor.Entries));

    public IActionResult Error() => View();
}

public sealed record FoleoTraderMonitorViewModel(IReadOnlyList<FoleoTraderMessageEntry> Messages);
