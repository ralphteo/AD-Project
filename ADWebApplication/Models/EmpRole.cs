namespace ADWebApplication.Models
{
    public class EmpRole
    {
        public int EmpAccountId { get; set; }
        public EmpAccount EmpAccount { get; set; } = null!;

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;
    }
}