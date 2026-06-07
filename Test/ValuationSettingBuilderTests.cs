using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class ValuationSettingBuilderTests
{
    [Fact]
    public void CreatedEvent_BuildsAssetAllocationWithHiddenRootAndTargets()
    {
        var accountID = AccountIDBuilder.Create();
        var assetAllocationID = AssetAllocationIDBuilder.Create();
        var rootNodeID = NodeIDBuilder.Create();
        var assetNodeID = NodeIDBuilder.Create();
        var nodes = CreateNodes(rootNodeID, assetNodeID, accountID);

        var result = AssetAllocationCreatedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            Constants.Initialisation.UserID,
            Constants.Initialisation.EventDateTime,
            Constants.Initialisation.AuditDateTime,
            "Create allocation",
            assetAllocationID,
            "Balanced",
            [accountID],
            true,
            rootNodeID,
            nodes);

        Assert.True(result.IsValid);

        var settings = new ValuationSettings(Constants.Initialisation.EventDateTime, [result.Value!]);

        var setting = Assert.Single(settings.Items);
        Assert.Equal(assetAllocationID, setting.AssetAllocationID);
        Assert.Equal("Balanced", setting.Name);
        Assert.True(setting.Active);
        Assert.Equal(rootNodeID, setting.RootNodeID);
        Assert.Equal(2, setting.Nodes.Count);
        Assert.True(setting.Nodes.Single(node => node.NodeID == rootNodeID).Hidden);
        Assert.Equal(accountID, setting.Nodes.Single(node => node.NodeID == assetNodeID).AccountSettings.Single().AccountID);
    }

    [Fact]
    public void SetEvents_UpdateAccountsAndActiveFlag()
    {
        var originalAccountID = AccountIDBuilder.Create();
        var replacementAccountID = AccountIDBuilder.Create();
        var assetAllocationID = AssetAllocationIDBuilder.Create();
        var rootNodeID = NodeIDBuilder.Create();
        var assetNodeID = NodeIDBuilder.Create();
        var created = AssetAllocationCreatedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            Constants.Initialisation.UserID,
            Constants.Initialisation.EventDateTime,
            Constants.Initialisation.AuditDateTime,
            "Create allocation",
            assetAllocationID,
            "Balanced",
            [originalAccountID],
            true,
            rootNodeID,
            CreateNodes(rootNodeID, assetNodeID, originalAccountID)).Value!;
        var accountIDsSet = AssetAllocationAccountIDsSetEventBuilder.Create(
            Constants.Initialisation.UserID,
            Constants.Initialisation.EventDateTime,
            "Set accounts",
            assetAllocationID,
            [replacementAccountID],
            new ValuationSettings(Constants.Initialisation.EventDateTime, [created])).Value!;
        var activeSet = AssetAllocationActiveSetEventBuilder.Create(
            Constants.Initialisation.UserID,
            Constants.Initialisation.EventDateTime,
            "Deactivate",
            assetAllocationID,
            false,
            new ValuationSettings(Constants.Initialisation.EventDateTime, [created, accountIDsSet])).Value!;

        var settings = new ValuationSettings(Constants.Initialisation.EventDateTime, [created, accountIDsSet, activeSet]);

        var setting = Assert.Single(settings.Items);
        Assert.Equal(replacementAccountID, Assert.Single(setting.AccountIDs));
        Assert.False(setting.Active);
    }

    [Fact]
    public void CreatedEvent_RejectsMissingChildNode()
    {
        var accountID = AccountIDBuilder.Create();
        var rootNodeID = NodeIDBuilder.Create();

        var result = AssetAllocationCreatedEventBuilder.Create(
            Constants.Initialisation.UserID,
            Constants.Initialisation.EventDateTime,
            "Create allocation",
            AssetAllocationIDBuilder.Create(),
            "Broken",
            [accountID],
            true,
            rootNodeID,
            [
                new AssetAllocationNode(rootNodeID, [NodeIDBuilder.Create()], "Root", false, true, [])
            ]);

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, message => message.Contains("references missing child NodeID", StringComparison.Ordinal));
    }

    private static List<AssetAllocationNode> CreateNodes(NodeID rootNodeID, NodeID assetNodeID, AccountID accountID) =>
    [
        new AssetAllocationNode(rootNodeID, [assetNodeID], "Root", false, true, []),
        new AssetAllocationNode(
            assetNodeID,
            [],
            "Assets",
            false,
            false,
            [new AssetAllocationNodeAccountSetting(accountID, 0.6m, 0.7m, 0.5m, 0.03m)])
    ];
}
