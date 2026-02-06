public class AssignedCollectionOfficerDto
{
    public required string Username { get; set; }
    public required string FullName { get; set; }

    public List<CollectionOfficerPlannedRouteDto> PlannedDates { get; set; } = new();
}
