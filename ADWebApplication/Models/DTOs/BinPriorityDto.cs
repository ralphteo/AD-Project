using System.Security.Principal;

public class BinPriorityDto
{
    public int BinId {get; set;}
    public int DaysTo80 {get; set;}
    public bool IsHighPriority => DaysTo80 <= 1;

}