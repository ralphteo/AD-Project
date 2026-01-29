using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADWebApplication.Models;
[Table("role")]
public class Role
{
    [Key]
    [Column("roleId")]
    public int RoleId {get; set;}

    [Column("name")]
    [Required, StringLength(30)]
    public String Name {get; set;} = "";

    public ICollection<Employee> Employees {get; set;} = new List<Employee>();
}
