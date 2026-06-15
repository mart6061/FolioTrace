using FolioTrace.Aggregates;
using Repository;

namespace Test;

public sealed class ReportConfigBuilderTests
{
    [Fact]
    public void SeedData_CreatesCurrentReportConfiguration()
    {
        var events = SeedRepository.CreateInitialReportCreatedEvents();
        var createdEvent = Assert.Single(events);
        var reports = new ReportConfigs(createdEvent.EventDateTime, events.Cast<IReportEvent>().ToList());

        var report = Assert.Single(reports.Items);
        Assert.Equal("Current", report.Name);
        Assert.True(report.Active);
        Assert.Equal(createdEvent.EventDateTime.Value.Date, report.EffectiveDateTime.Value);
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

        var allocationBackedNodes = report.Nodes
            .OfType<ReportNodeChart>()
            .Select(node => node.AssetAllocationID)
            .Concat(report.Nodes.OfType<ReportNodeValuation>().Select(node => node.AssetAllocationID))
            .Concat(report.Nodes.OfType<ReportNodeTransactions>().Select(node => node.AssetAllocationID))
            .Concat(report.Nodes.OfType<ReportNodeCash>().Select(node => node.AssetAllocationID))
            .Distinct()
            .ToList();

        Assert.Single(allocationBackedNodes);
        Assert.Equal(SeedRepository.CreateInitialAssetAllocationCreatedEvents().Single().AssetAllocationID, allocationBackedNodes.Single());
    }
}
