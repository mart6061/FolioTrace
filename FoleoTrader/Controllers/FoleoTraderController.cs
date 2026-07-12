using FoleoTrader.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoleoTrader.Controllers;

public sealed class FoleoTraderController(FoleoTraderMessageMonitor monitor, TradeFileSimulator tradeFileSimulator) : Controller
{
    public IActionResult Index() => View(new FoleoTraderMonitorViewModel(monitor.Entries));

    public IActionResult FTP() => View(new FoleoTraderFTPViewModel(
        tradeFileSimulator.Files.OrderByDescending(file => file.ReceivedAtUtc).ToList()));

    public IActionResult Error() => View();
}

public sealed record FoleoTraderMonitorViewModel(IReadOnlyList<FoleoTraderMessageEntry> Messages);
public sealed record FoleoTraderFTPViewModel(IReadOnlyList<ReceivedTradeFile> Files);
