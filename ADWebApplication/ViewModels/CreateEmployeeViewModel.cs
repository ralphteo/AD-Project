using System.ComponentModel.DataAnnotations;

namespace ADWebApplication.ViewModels;

public class CreateEmployeeViewModel
{
    [Required]
    public string FullName { get; set; } = "";

    [Required]
    public string Username { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public int RoleId { get; set; }

    [Required, MinLength(6)]
    public string Password { get; set; } = "";

    [Required, Compare("Password")]
    public string ConfirmPassword { get; set; } = "";
}