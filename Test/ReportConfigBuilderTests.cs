using FolioTrace.Aggregates;
using FolioTrace;
using FolioTrace.Types;
using Repository;
using System.Text.Json;

namespace Test;

public sealed class ReportConfigBuilderTests
{
    [Fact]
    public void SeedData_CreatesCurrentReportConfiguration()
    {
        var events = SeedRepository.CreateInitialReportCreatedEvents();
        Assert.Equal(2, events.Count);
        var reports = new ReportConfigs(Constants.Initialisation.EventDateTime, events.Cast<IReportEvent>().ToList());

        var report = reports.Items.Single(report => report.Name == "Current");
        var createdEvent = events.Single(@event => @event.Name == "Current");
        Assert.Equal("Current", report.Name);
        Assert.True(report.Active);
        Assert.Equal(createdEvent.EventID, report.LastEventID);
        Assert.Equal(createdEvent.AuditDateTime.Value, report.LastAuditDateTime.Value);
        Assert.Collection(
            report.Nodes,
            node => Assert.IsType<ReportNodeCoverPage>(node),
            node => Assert.IsType<ReportNodeIndex>(node),
            node => Assert.IsType<ReportNodeChart>(node),
            node => Assert.IsType<ReportNodeValuation>(node),
            node => Assert.IsType<ReportNodeTransactions>(node),
            node => Assert.IsType<ReportNodeCash>(node));

        var chart = Assert.IsType<ReportNodeChart>(report.Nodes[2]);
        Assert.Equal(ReportChartType.Pie, chart.ChartType);
        Assert.Equal(1, chart.PieLevel);

        var valuation = Assert.IsType<ReportNodeValuation>(report.Nodes[3]);
        Assert.Equal(ReportNodePageOrientation.Landscape, valuation.PageOrientation);
        Assert.Equal(ReportConfigBuilder.DefaultValuationColumns(), valuation.Columns);
        Assert.True(valuation.DisplayHoldings);

        var allocationBackedNodes = report.Nodes
            .OfType<ReportNodeChart>()
            .Select(node => node.AssetAllocationID)
            .Concat(report.Nodes.OfType<ReportNodeValuation>().Select(node => node.AssetAllocationID))
            .Concat(report.Nodes.OfType<ReportNodeTransactions>().Select(node => node.AssetAllocationID))
            .Concat(report.Nodes.OfType<ReportNodeCash>().Select(node => node.AssetAllocationID))
            .Distinct()
            .ToList();

        Assert.Single(allocationBackedNodes);
        Assert.Equal(SeedRepository.CreateInitialAssetAllocationCreatedEvents().Single(@event => @event.Name == "Detailed").AssetAllocationID, allocationBackedNodes.Single());
    }

    [Fact]
    public void SeedData_CreatesIMReportConfigurationForModelPortfolio()
    {
        var allocationEvents = SeedRepository.CreateInitialAssetAllocationCreatedEvents();
        var summaryAssetAllocationID = allocationEvents.Single(@event => @event.Name == "Summary").AssetAllocationID;
        var detailedAssetAllocationID = allocationEvents.Single(@event => @event.Name == "Detailed").AssetAllocationID;
        var events = SeedRepository.CreateInitialReportCreatedEvents();
        var reports = new ReportConfigs(Constants.Initialisation.EventDateTime, events.Cast<IReportEvent>().ToList());

        var report = reports.Items.Single(report => report.Name == "IM");
        Assert.True(report.Active);
        Assert.Collection(
            report.Nodes,
            node =>
            {
                var valuation = Assert.IsType<ReportNodeValuation>(node);
                Assert.Equal(1, valuation.DisplayOrder);
                Assert.Equal("Summary Valuation", valuation.Name);
                Assert.Equal(summaryAssetAllocationID, valuation.AssetAllocationID);
                Assert.Equal(ReportNodePageOrientation.Landscape, valuation.PageOrientation);
                Assert.False(valuation.DisplayHoldings);
                Assert.True(valuation.ColourBullet);
                Assert.False(valuation.ColourText);
            },
            node =>
            {
                var valuation = Assert.IsType<ReportNodeValuation>(node);
                Assert.Equal(2, valuation.DisplayOrder);
                Assert.Equal("Detailed Valuation", valuation.Name);
                Assert.Equal(detailedAssetAllocationID, valuation.AssetAllocationID);
                Assert.Equal(ReportNodePageOrientation.Landscape, valuation.PageOrientation);
                Assert.True(valuation.DisplayHoldings);
                Assert.True(valuation.ColourBullet);
                Assert.False(valuation.ColourText);
            });
    }

    [Fact]
    public void ReportCreatedEvent_SerializesNodeTypeDiscriminators()
    {
        var createdEvent = SeedRepository.CreateInitialReportCreatedEvents().Single(@event => @event.Name == "Current");

        var json = JsonSerializer.Serialize(createdEvent);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("ReportNodeCoverPage", document.RootElement.GetProperty("Nodes")[0].GetProperty("$type").GetString());

        var roundTripped = JsonSerializer.Deserialize<ReportCreatedEvent>(json);

        Assert.NotNull(roundTripped);
        Assert.IsType<ReportNodeCoverPage>(roundTripped.Nodes[0]);
        Assert.IsType<ReportNodeChart>(roundTripped.Nodes[2]);
        var valuation = Assert.IsType<ReportNodeValuation>(roundTripped.Nodes[3]);
        Assert.True(valuation.DisplayHoldings);
    }

    [Fact]
    public void ReportCreatedEvent_SerializesProfitLossNodeMethod()
    {
        var assetAllocationID = AssetAllocationIDBuilder.Create();
        var createdEvent = ReportCreatedEventBuilder.Create(
            Constants.Initialisation.UserID,
            ReportIDBuilder.Create(),
            "Profit Loss Report",
            true,
            [
                new ReportNodeProfitLoss(
                    ReportNodeIDBuilder.Create(),
                    1,
                    "Profit Loss",
                    "Profit Loss",
                    assetAllocationID,
                    ReportProfitLossMethod.LIFO)
            ]).Value!;

        var json = JsonSerializer.Serialize(createdEvent);
        using var document = JsonDocument.Parse(json);
        var node = document.RootElement.GetProperty("Nodes")[0];
        Assert.Equal("ReportNodeProfitLoss", node.GetProperty("$type").GetString());
        Assert.Equal("LIFO", node.GetProperty("ProfitLossMethod").GetString());

        var roundTripped = JsonSerializer.Deserialize<ReportCreatedEvent>(json);

        Assert.NotNull(roundTripped);
        var profitLoss = Assert.IsType<ReportNodeProfitLoss>(roundTripped.Nodes[0]);
        Assert.Equal(ReportProfitLossMethod.LIFO, profitLoss.ProfitLossMethod);
    }
}
