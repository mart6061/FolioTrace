using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class ReportEventValidation
{
    public static List<string> ValidateBase(EventID eventID, UserID userID, AuditDateTime auditDateTime, ReportID reportID)
    {
        var messages = new List<string>();

        if (eventID is null)
            messages.Add("Event ID is required.");
        if (userID is null)
            messages.Add("User ID is required.");
        if (auditDateTime is null)
            messages.Add("Audit date/time is required.");
        if (reportID is null)
            messages.Add("Report ID is required.");

        return messages;
    }

    public static void ValidateDefinition(List<string> messages, string name, List<ReportNodeBase> nodes, ValuationSettings? valuationSettings)
    {
        if (string.IsNullOrWhiteSpace(name))
            messages.Add("Name is required.");

        if (nodes is null)
        {
            messages.Add("Nodes are required.");
            return;
        }

        var nodeIDs = new HashSet<Guid>();
        foreach (var node in nodes)
        {
            if (node.ReportNodeID is null)
                messages.Add("Every report node requires a node ID.");
            else if (!nodeIDs.Add(node.ReportNodeID.Value))
                messages.Add($"Report node ID '{node.ReportNodeID}' is duplicated.");

            if (string.IsNullOrWhiteSpace(node.Name))
                messages.Add($"Report node '{node.ReportNodeID}' requires a name.");

            if (string.IsNullOrWhiteSpace(node.Title))
                messages.Add($"Report node '{node.ReportNodeID}' requires a title.");

            if (node.DisplayOrder < 1)
                messages.Add($"Report node '{node.ReportNodeID}' requires a display order greater than zero.");

            if (node is ReportNodeChart chart)
            {
                ValidateAssetAllocation(messages, chart.AssetAllocationID, valuationSettings, chart.ReportNodeID);

                if (chart.ChartType == ReportChartType.Pie && chart.PieLevel is < 1 or > 3)
                    messages.Add($"Report node '{chart.ReportNodeID}' requires a pie level between one and three.");
            }
            else if (node is ReportNodeValuation valuation)
            {
                ValidateAssetAllocation(messages, valuation.AssetAllocationID, valuationSettings, valuation.ReportNodeID);

                if (valuation.Columns?.Any(column => column.DisplayOrder < 1) == true)
                    messages.Add($"Report node '{valuation.ReportNodeID}' has a valuation column display order less than one.");
            }
            else if (node is ReportNodeTransactions transactions)
            {
                ValidateAssetAllocation(messages, transactions.AssetAllocationID, valuationSettings, transactions.ReportNodeID);
            }
            else if (node is ReportNodeCash cash)
            {
                ValidateAssetAllocation(messages, cash.AssetAllocationID, valuationSettings, cash.ReportNodeID);
            }
        }
    }

    public static void ValidateCreatedReport(List<string> messages, ReportID reportID, ReportConfigs? reportConfigs)
    {
        if (reportConfigs?.Items.Any(item => item.ReportID == reportID) == true)
            messages.Add($"Report already exists for ReportID '{reportID}'.");
    }

    public static void ValidateExistingReport(List<string> messages, ReportID reportID, ReportConfigs? reportConfigs)
    {
        if (reportConfigs?.Items.Any(item => item.ReportID == reportID) != true)
            messages.Add($"No matching report found for ReportID '{reportID}'.");
    }

    private static void ValidateAssetAllocation(List<string> messages, AssetAllocationID assetAllocationID, ValuationSettings? valuationSettings, ReportNodeID reportNodeID)
    {
        if (assetAllocationID is null)
        {
            messages.Add($"Report node '{reportNodeID}' requires an asset allocation.");
            return;
        }

        if (valuationSettings is not null && !valuationSettings.Items.Any(item => item.AssetAllocationID == assetAllocationID && item.Active))
            messages.Add($"Report node '{reportNodeID}' references missing or inactive AssetAllocationID '{assetAllocationID}'.");
    }
}
