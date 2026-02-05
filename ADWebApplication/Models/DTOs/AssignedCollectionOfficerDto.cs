public class AssignedCollectionOfficerDto
{
    public string Username { get; set; }
    public string FullName { get; set; }

    public List<CollectionOfficerPlannedRouteDto> PlannedDates { get; set; } = new();
}
