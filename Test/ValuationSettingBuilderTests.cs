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
        Assert.Equal(3, setting.Nodes.Count);
        var rootNode = setting.Nodes.Single(node => node.NodeID == rootNodeID);
        var specialNode = setting.Nodes.Single(node => node.Name == "Unallocated");
        Assert.True(rootNode.Hidden);
        Assert.Equal(specialNode.NodeID, rootNode.Nodes[0]);
        Assert.Equal("#dc2626", specialNode.Colour);
        Assert.Empty(specialNode.AccountSettings);
        Assert.Empty(specialNode.Nodes);
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
    public void CreatedEvent_PreservesNodeColour()
    {
        var accountID = AccountIDBuilder.Create();
        var assetAllocationID = AssetAllocationIDBuilder.Create();
        var rootNodeID = NodeIDBuilder.Create();
        var assetNodeID = NodeIDBuilder.Create();
        var nodes = CreateNodes(rootNodeID, assetNodeID, accountID);
        nodes[1] = nodes[1] with { Colour = "#123abc" };

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
        var assetNode = Assert.Single(settings.Items.Single().Nodes, node => node.NodeID == assetNodeID);
        Assert.Equal("#123abc", assetNode.Colour);
    }

    [Fact]
    public void ModifiedEvent_PreservesNodeColour()
    {
        var accountID = AccountIDBuilder.Create();
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
            [accountID],
            true,
            rootNodeID,
            CreateNodes(rootNodeID, assetNodeID, accountID)).Value!;
        var modifiedNodes = CreateNodes(rootNodeID, assetNodeID, accountID);
        modifiedNodes[1] = modifiedNodes[1] with { Colour = "#456def" };

        var result = AssetAllocationModifiedEventBuilder.Create(
            Constants.Initialisation.UserID,
            Constants.Initialisation.EventDateTime,
            "Modify allocation",
            assetAllocationID,
            "Balanced",
            [accountID],
            rootNodeID,
            modifiedNodes,
            new ValuationSettings(Constants.Initialisation.EventDateTime, [created]));

        Assert.True(result.IsValid);

        var settings = new ValuationSettings(Constants.Initialisation.EventDateTime, [created, result.Value!]);
        var assetNode = Assert.Single(settings.Items.Single().Nodes, node => node.NodeID == assetNodeID);
        Assert.Equal("#456def", assetNode.Colour);
    }

    [Fact]
    public void CreatedEvent_AllowsOldNodesWithoutColour()
    {
        var accountID = AccountIDBuilder.Create();
        var rootNodeID = NodeIDBuilder.Create();
        var assetNodeID = NodeIDBuilder.Create();

        var result = AssetAllocationCreatedEventBuilder.Create(
            Constants.Initialisation.UserID,
            Constants.Initialisation.EventDateTime,
            "Create allocation",
            AssetAllocationIDBuilder.Create(),
            "Balanced",
            [accountID],
            true,
            rootNodeID,
            CreateNodes(rootNodeID, assetNodeID, accountID));

        Assert.True(result.IsValid);
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

    [Fact]
    public void CreatedEvent_RejectsUnreachableNode()
    {
        var accountID = AccountIDBuilder.Create();
        var rootNodeID = NodeIDBuilder.Create();
        var orphanNodeID = NodeIDBuilder.Create();

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
                new AssetAllocationNode(rootNodeID, [], "Root", false, true, []),
                new AssetAllocationNode(orphanNodeID, [], "Orphan", false, false, [])
            ]);

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, message => message.Contains("is not reachable from RootNodeID", StringComparison.Ordinal));
    }

    [Fact]
    public void CreatedEvent_RejectsCycle()
    {
        var accountID = AccountIDBuilder.Create();
        var rootNodeID = NodeIDBuilder.Create();
        var firstNodeID = NodeIDBuilder.Create();
        var secondNodeID = NodeIDBuilder.Create();

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
                new AssetAllocationNode(rootNodeID, [firstNodeID], "Root", false, true, []),
                new AssetAllocationNode(firstNodeID, [secondNodeID], "First", false, false, []),
                new AssetAllocationNode(secondNodeID, [firstNodeID], "Second", false, false, [])
            ]);

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, message => message.Contains("creates a cycle", StringComparison.Ordinal));
    }

    [Fact]
    public void CreatedEvent_RejectsDuplicateParent()
    {
        var accountID = AccountIDBuilder.Create();
        var rootNodeID = NodeIDBuilder.Create();
        var firstNodeID = NodeIDBuilder.Create();
        var secondNodeID = NodeIDBuilder.Create();

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
                new AssetAllocationNode(rootNodeID, [firstNodeID, secondNodeID], "Root", false, true, []),
                new AssetAllocationNode(firstNodeID, [secondNodeID], "First", false, false, []),
                new AssetAllocationNode(secondNodeID, [], "Second", false, false, [])
            ]);

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, message => message.Contains("has multiple parents", StringComparison.Ordinal));
    }

    [Fact]
    public void CreatedEvent_RejectsDuplicateChildReference()
    {
        var accountID = AccountIDBuilder.Create();
        var rootNodeID = NodeIDBuilder.Create();
        var assetNodeID = NodeIDBuilder.Create();

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
                new AssetAllocationNode(rootNodeID, [assetNodeID, assetNodeID], "Root", false, true, []),
                new AssetAllocationNode(assetNodeID, [], "Asset", false, false, [])
            ]);

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, message => message.Contains("more than once", StringComparison.Ordinal));
    }

    [Fact]
    public void CreatedEvent_RejectsInvalidColour()
    {
        var accountID = AccountIDBuilder.Create();
        var rootNodeID = NodeIDBuilder.Create();
        var assetNodeID = NodeIDBuilder.Create();
        var nodes = CreateNodes(rootNodeID, assetNodeID, accountID);
        nodes[1] = nodes[1] with { Colour = "teal" };

        var result = AssetAllocationCreatedEventBuilder.Create(
            Constants.Initialisation.UserID,
            Constants.Initialisation.EventDateTime,
            "Create allocation",
            AssetAllocationIDBuilder.Create(),
            "Broken",
            [accountID],
            true,
            rootNodeID,
            nodes);

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, message => message.Contains("invalid Colour", StringComparison.Ordinal));
    }

    [Fact]
    public void CreatedEvent_PreservesSiblingOrder()
    {
        var accountID = AccountIDBuilder.Create();
        var assetAllocationID = AssetAllocationIDBuilder.Create();
        var rootNodeID = NodeIDBuilder.Create();
        var firstNodeID = NodeIDBuilder.Create();
        var secondNodeID = NodeIDBuilder.Create();

        var result = AssetAllocationCreatedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            Constants.Initialisation.UserID,
            Constants.Initialisation.EventDateTime,
            Constants.Initialisation.AuditDateTime,
            "Create allocation",
            assetAllocationID,
            "Ordered",
            [accountID],
            true,
            rootNodeID,
            [
                new AssetAllocationNode(rootNodeID, [secondNodeID, firstNodeID], "Root", false, true, []),
                new AssetAllocationNode(firstNodeID, [], "First", false, false, []),
                new AssetAllocationNode(secondNodeID, [], "Second", false, false, [])
            ]);

        Assert.True(result.IsValid);

        var settings = new ValuationSettings(Constants.Initialisation.EventDateTime, [result.Value!]);
        var rootNode = Assert.Single(settings.Items.Single().Nodes, node => node.NodeID == rootNodeID);
        var specialNode = Assert.Single(settings.Items.Single().Nodes, node => node.Name == "Unallocated");
        Assert.Equal([specialNode.NodeID, secondNodeID, firstNodeID], rootNode.Nodes);
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
