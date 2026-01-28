using System.ComponentModel.DataAnnotations;

namespace ADWebApplication.Models;

public class Role
{
    [Key]
    public int Id {get; set;}

    [Required, StringLength(30)]
    public String Name {get; set;} = "";

}