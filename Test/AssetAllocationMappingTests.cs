using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class AssetAllocationMappingTests
{
    private static readonly UserID UserID = new(Guid.Parse("62ea76fd-88ef-4c31-8890-b5f41e28f8ed"));
    private static readonly EventDateTime EventDate = EventDateTimeBuilder.Create(new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc));
    private static readonly AuditDateTime AuditDate = AuditDateTimeBuilder.Create(new DateTime(2026, 6, 10, 12, 1, 0, DateTimeKind.Utc));
    private static readonly AccountID AccountID = AccountIDBuilder.Create();
    private static readonly AccountID OtherAccountID = AccountIDBuilder.Create();
    private static readonly InstrumentID InstrumentID = InstrumentIDBuilder.Create();

    [Fact]
    public void Builder_CreatesMappingToLeafNode()
    {
        var allocation = CreateAllocation();
        var holdingID = HoldingIDBuilder.Create();
        var holdings = CreateHoldings(CreateHolding(holdingID, AccountID));
        var request = new AssetAllocationMappingRequest(UserID, EventDate, "Map holding", allocation.AssetAllocationID, holdingID, LeafNodeID(allocation));

        var result = AssetAllocationMappingEventBuilder.Create(request, new ValuationSettings(EventDate, [allocation]), holdings);

        Assert.True(result.IsValid);
        Assert.Equal(nameof(AssetAllocationMappingSetEvent), result.Value!.Type);
        Assert.Equal(allocation.AssetAllocationID, result.Value.AssetAllocationID);
        Assert.Equal(holdingID, result.Value.HoldingID);
        Assert.Equal(LeafNodeID(allocation), result.Value.NodeID);
    }

    [Fact]
    public void Builder_RejectsInvalidMappingReferences()
    {
        var allocation = CreateAllocation();
        var holdingID = HoldingIDBuilder.Create();
        var holdings = CreateHoldings(CreateHolding(holdingID, OtherAccountID));
        var settings = new ValuationSettings(EventDate, [allocation]);
        var inactive = CreateAllocation(active: false);

        var inactiveAllocation = AssetAllocationMappingEventBuilder.Create(new AssetAllocationMappingRequest(UserID, EventDate, "Map holding", inactive.AssetAllocationID, holdingID, LeafNodeID(inactive)), new ValuationSettings(EventDate, [inactive]), holdings);
        var missingHolding = AssetAllocationMappingEventBuilder.Create(new AssetAllocationMappingRequest(UserID, EventDate, "Map holding", allocation.AssetAllocationID, HoldingIDBuilder.Create(), LeafNodeID(allocation)), settings, holdings);
        var wrongAccount = AssetAllocationMappingEventBuilder.Create(new AssetAllocationMappingRequest(UserID, EventDate, "Map holding", allocation.AssetAllocationID, holdingID, LeafNodeID(allocation)), settings, holdings);
        var missingNode = AssetAllocationMappingEventBuilder.Create(new AssetAllocationMappingRequest(UserID, EventDate, "Map holding", allocation.AssetAllocationID, holdingID, NodeIDBuilder.Create()), settings, holdings);
        var nonLeafNode = AssetAllocationMappingEventBuilder.Create(new AssetAllocationMappingRequest(UserID, EventDate, "Map holding", allocation.AssetAllocationID, holdingID, allocation.RootNodeID), settings, holdings);

        Assert.False(inactiveAllocation.IsValid);
        Assert.Contains($"Asset allocation '{inactive.AssetAllocationID}' is inactive.", inactiveAllocation.ValidationErrors);
        Assert.False(missingHolding.IsValid);
        Assert.Contains("No matching holding found", string.Join(' ', missingHolding.ValidationErrors));
        Assert.False(wrongAccount.IsValid);
        Assert.Contains("is not assigned to AssetAllocationID", string.Join(' ', wrongAccount.ValidationErrors));
        Assert.False(missingNode.IsValid);
        Assert.Contains("No matching node found", string.Join(' ', missingNode.ValidationErrors));
        Assert.False(nonLeafNode.IsValid);
        Assert.Contains("has child nodes and cannot be used for holding mappings", string.Join(' ', nonLeafNode.ValidationErrors));
    }

    [Fact]
    public void Aggregate_ProjectsLatestMappingPerAllocationAndHolding()
    {
        var firstAllocation = CreateAllocation();
        var secondAllocation = CreateAllocation();
        var holdingID = HoldingIDBuilder.Create();
        var holdings = CreateHoldings(CreateHolding(holdingID, AccountID));
        var settings = new ValuationSettings(EventDate, [firstAllocation, secondAllocation]);
        var firstDate = EventDateTimeBuilder.Create(EventDate.Value.AddHours(1));
        var secondDate = EventDateTimeBuilder.Create(EventDate.Value.AddHours(2));
        var firstAllocationInitial = AssetAllocationMappingEventBuilder.Create(new AssetAllocationMappingRequest(UserID, firstDate, "Map first", firstAllocation.AssetAllocationID, holdingID, LeafNodeID(firstAllocation)), settings, holdings).Value!;
        var firstAllocationLatest = AssetAllocationMappingEventBuilder.Create(new AssetAllocationMappingRequest(UserID, secondDate, "Map first again", firstAllocation.AssetAllocationID, holdingID, UnallocatedNodeID(firstAllocation)), settings, holdings).Value!;
        var secondAllocationMapping = AssetAllocationMappingEventBuilder.Create(new AssetAllocationMappingRequest(UserID, firstDate, "Map second", secondAllocation.AssetAllocationID, holdingID, LeafNodeID(secondAllocation)), settings, holdings).Value!;

        var aggregate = new AssetAllocationMappings(secondDate, [firstAllocationInitial, firstAllocationLatest, secondAllocationMapping]);

        Assert.Equal(2, aggregate.Items.Count);
        Assert.Equal(UnallocatedNodeID(firstAllocation), aggregate.Items.Single(item => item.AssetAllocationID == firstAllocation.AssetAllocationID).NodeID);
        Assert.Equal(LeafNodeID(secondAllocation), aggregate.Items.Single(item => item.AssetAllocationID == secondAllocation.AssetAllocationID).NodeID);
    }

    private static AssetAllocationCreatedEvent CreateAllocation(bool active = true)
    {
        var assetAllocationID = AssetAllocationIDBuilder.Create();
        var rootNodeID = NodeIDBuilder.Create();
        var unallocatedNodeID = NodeIDBuilder.Create();
        var leafNodeID = NodeIDBuilder.Create();

        return AssetAllocationCreatedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            AuditDate,
            "Create allocation",
            assetAllocationID,
            "Balanced",
            [AccountID],
            active,
            rootNodeID,
            [
                new AssetAllocationNode(rootNodeID, [unallocatedNodeID, leafNodeID], "Balanced", false, true, [], "#0f766e"),
                new AssetAllocationNode(unallocatedNodeID, [], "Unallocated", false, false, [], "#dc2626"),
                new AssetAllocationNode(leafNodeID, [], "Equity", false, false, [new AssetAllocationNodeAccountSetting(AccountID, 0, 0, 0, 0)], "#0f766e")
            ]).Value!;
    }

    private static Holdings CreateHoldings(params HoldingCreatedEvent[] events) =>
        new(EventDate, AuditDate, events.Cast<IHoldingEvent>().ToList());

    private static HoldingPositionAssetCreatedEvent CreateHolding(HoldingID holdingID, AccountID accountID, bool active = true) =>
        HoldingPositionAssetCreatedEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            UserID,
            EventDate,
            AuditDate,
            "Create holding",
            holdingID,
            accountID,
            InstrumentID,
            "Asset",
            active,
            false).Value!;

    private static NodeID LeafNodeID(AssetAllocationCreatedEvent allocation) =>
        allocation.Nodes.Single(node => node.Name == "Equity").NodeID;

    private static NodeID UnallocatedNodeID(AssetAllocationCreatedEvent allocation) =>
        allocation.Nodes.Single(node => node.Name == "Unallocated").NodeID;
}
