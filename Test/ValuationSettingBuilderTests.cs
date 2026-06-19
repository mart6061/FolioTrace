using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Test;

public sealed class ValuationSettingBuilderTests
{
    [Fact]
    public void SeedData_CreatesDetailedSummaryAndAssetsListAssetAllocationConfigurations()
    {
        var events = SeedRepository.CreateInitialAssetAllocationCreatedEvents();
        Assert.Equal(3, events.Count);
        var settings = new ValuationSettings(Constants.Initialisation.EventDateTime, events.Cast<IValuationSettingEvent>().ToList());

        Assert.Equal(3, settings.Items.Count);
        var detailed = settings.Items.Single(setting => setting.Name == "Detailed");
        var summary = settings.Items.Single(setting => setting.Name == "Summary");
        var assetsList = settings.Items.Single(setting => setting.Name == "Assets List");

        Assert.True(detailed.Active);
        Assert.True(summary.Active);
        Assert.True(assetsList.Active);
        Assert.Equal(detailed.AccountIDs.ToHashSet(), summary.AccountIDs.ToHashSet());
        Assert.Equal(detailed.AccountIDs.ToHashSet(), assetsList.AccountIDs.ToHashSet());

        AssertSeedAllocationHasUnallocatedNode(detailed);
        AssertSeedAllocationHasUnallocatedNode(summary);
        AssertSeedAllocationHasUnallocatedNode(assetsList);

        var equities = detailed.Nodes.Single(node => node.Name == "Equities");
        Assert.Null(equities.Colour);
        Assert.Equal(["UK", "Europe", "US", "Other"], NodeNames(detailed, equities.Nodes));
        Assert.Equal(["#1d4ed8", "#2563eb", "#3b82f6", "#60a5fa"], NodeColours(detailed, equities.Nodes));

        var fixedInterest = detailed.Nodes.Single(node => node.Name == "Fixed Interest");
        Assert.Null(fixedInterest.Colour);
        Assert.Equal(["Government", "Corp"], NodeNames(detailed, fixedInterest.Nodes));
        Assert.Equal(["#047857", "#34d399"], NodeColours(detailed, fixedInterest.Nodes));

        Assert.Equal("#7c3aed", detailed.Nodes.Single(node => node.Name == "Alternative").Colour);
        Assert.Equal("#64748b", detailed.Nodes.Single(node => node.Name == "Memo").Colour);
        Assert.Equal("#059669", detailed.Nodes.Single(node => node.Name == "Cash").Colour);

        var uk = detailed.Nodes.Single(node => node.Name == "UK");
        Assert.Equal(
            ["Basic Materials", "Consumer Discretionary", "Consumer Staples", "Energy", "Financials", "Health Care", "Industrials", "Real Estate", "Technology", "Telecommunications"],
            NodeNames(detailed, uk.Nodes));
        Assert.Equal(
            ["Mining", "Chemicals", "Forestry & Paper"],
            NodeNames(detailed, detailed.Nodes.Single(node => node.Name == "Basic Materials").Nodes));
        Assert.Equal(
            ["Software & Computer Services", "Technology Hardware & Equipment"],
            NodeNames(detailed, detailed.Nodes.Single(node => node.Name == "Technology").Nodes));
        Assert.Equal(
            ["Telecommunication Service Providers"],
            NodeNames(detailed, detailed.Nodes.Single(node => node.Name == "Telecommunications").Nodes));

        Assert.Equal(["Equities", "Fixed Interest", "Alternative", "Memo", "Cash"], summary.Nodes.Where(node => node.Name != "Unallocated").Select(node => node.Name).ToList());
        Assert.Equal(["#2563eb", "#059669", "#7c3aed", "#64748b", "#059669"], summary.Nodes.Where(node => node.Name != "Unallocated").Select(node => node.Colour).ToList());

        Assert.Equal(["Assets", "Cash"], assetsList.Nodes.Where(node => node.Name != "Unallocated").Select(node => node.Name).ToList());
        Assert.Equal(["#000000", "#6b7280"], assetsList.Nodes.Where(node => node.Name != "Unallocated").Select(node => node.Colour).ToList());
    }

    [Fact]
    public void CreatedEvent_BuildsAssetAllocationWithVirtualRootAndTargets()
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
        Assert.Equal(result.Value!.EventID, setting.LastEventID);
        Assert.Equal(result.Value.AuditDateTime.Value, setting.LastAuditDateTime.Value);
        Assert.Equal(rootNodeID, setting.RootNodeID);
        Assert.Equal(2, setting.Nodes.Count);
        var specialNode = setting.Nodes.Single(node => node.Name == "Unallocated");
        Assert.DoesNotContain(setting.Nodes, node => node.NodeID == rootNodeID);
        Assert.Equal(specialNode.NodeID, setting.Nodes[0].NodeID);
        Assert.Equal("#dc2626", specialNode.Colour);
        Assert.Empty(specialNode.AccountSettings);
        Assert.Empty(specialNode.Nodes);
        Assert.Equal(accountID, setting.Nodes.Single(node => node.NodeID == assetNodeID).AccountSettings.Single().AccountID);
    }

    private static void AssertSeedAllocationHasUnallocatedNode(ValuationSetting setting)
    {
        var unallocatedNode = Assert.Single(setting.Nodes, node => node.Name == "Unallocated");
        Assert.DoesNotContain(setting.Nodes, node => node.NodeID == setting.RootNodeID);
        Assert.Equal(unallocatedNode.NodeID, setting.Nodes[0].NodeID);
        Assert.Equal("#dc2626", unallocatedNode.Colour);
        Assert.Empty(unallocatedNode.AccountSettings);
    }

    private static List<string> NodeNames(ValuationSetting setting, IEnumerable<NodeID> nodeIDs) =>
        nodeIDs
            .Select(nodeID => setting.Nodes.Single(node => node.NodeID == nodeID).Name)
            .ToList();

    private static List<string?> NodeColours(ValuationSetting setting, IEnumerable<NodeID> nodeIDs) =>
        nodeIDs
            .Select(nodeID => setting.Nodes.Single(node => node.NodeID == nodeID).Colour)
            .ToList();

    [Fact]
    public void CreatedEvent_IgnoresLegacyEffectiveDateArgument()
    {
        var accountID = AccountIDBuilder.Create();
        var rootNodeID = NodeIDBuilder.Create();
        var assetNodeID = NodeIDBuilder.Create();

        var result = AssetAllocationCreatedEventBuilder.Create(
            Constants.Initialisation.UserID,
            Constants.Initialisation.EventDateTime,
            new EventDateTime(new DateTime(2026, 6, 13, 10, 0, 0, DateTimeKind.Utc)),
            "Create allocation",
            AssetAllocationIDBuilder.Create(),
            "Balanced",
            [accountID],
            true,
            rootNodeID,
            CreateNodes(rootNodeID, assetNodeID, accountID));

        Assert.True(result.IsValid);

        var settings = new ValuationSettings(Constants.Initialisation.EventDateTime, [result.Value!]);
        var setting = Assert.Single(settings.Items);
        Assert.Equal(result.Value!.EventID, setting.LastEventID);
        Assert.Equal(result.Value.AuditDateTime.Value, setting.LastAuditDateTime.Value);
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
                new AssetAllocationNode(NodeIDBuilder.Create(), [NodeIDBuilder.Create()], "Assets", false, false, [])
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
                new AssetAllocationNode(orphanNodeID, [orphanNodeID], "Orphan", false, false, [])
            ]);

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, message => message.Contains("is not reachable from a top-level node", StringComparison.Ordinal));
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
                new AssetAllocationNode(firstNodeID, [secondNodeID], "First", false, false, []),
                new AssetAllocationNode(NodeIDBuilder.Create(), [secondNodeID], "Other", false, false, []),
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
                new AssetAllocationNode(NodeIDBuilder.Create(), [assetNodeID, assetNodeID], "Parent", false, false, []),
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
        var specialNode = Assert.Single(settings.Items.Single().Nodes, node => node.Name == "Unallocated");
        Assert.Equal([specialNode.NodeID, secondNodeID, firstNodeID], settings.Items.Single().Nodes.Select(node => node.NodeID).Take(3).ToList());
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
