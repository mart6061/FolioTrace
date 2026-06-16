using FolioTrace.Aggregates;
using FolioTrace;
using Repository;
using System.Text.Json;

namespace Test;

public sealed class ReportConfigBuilderTests
{
    [Fact]
    public void SeedData_CreatesCurrentReportConfiguration()
    {
        var events = SeedRepository.CreateInitialReportCreatedEvents();
        var createdEvent = Assert.Single(events);
        var reports = new ReportConfigs(Constants.Initialisation.EventDateTime, events.Cast<IReportEvent>().ToList());

        var report = Assert.Single(reports.Items);
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

    [Fact]
    public void ReportCreatedEvent_SerializesNodeTypeDiscriminators()
    {
        var createdEvent = SeedRepository.CreateInitialReportCreatedEvents().Single();

        var json = JsonSerializer.Serialize(createdEvent);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("ReportNodeCoverPage", document.RootElement.GetProperty("Nodes")[0].GetProperty("$type").GetString());

        var roundTripped = JsonSerializer.Deserialize<ReportCreatedEvent>(json);

        Assert.NotNull(roundTripped);
        Assert.IsType<ReportNodeCoverPage>(roundTripped.Nodes[0]);
        Assert.IsType<ReportNodeChart>(roundTripped.Nodes[2]);
        Assert.IsType<ReportNodeValuation>(roundTripped.Nodes[3]);
    }
}
