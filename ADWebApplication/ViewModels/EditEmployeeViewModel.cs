using System.ComponentModel.DataAnnotations;

namespace ADWebApplication.ViewModels;

public class EditEmployeeViewModel
{
    [Required]
    public string Username { get; set; } = "";  // PK

    [Required]
    public string FullName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    public bool IsActive { get; set; }

    [Required]
    public int RoleId { get; set; }   // ✅ add this

    // optional reset password
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [Compare("NewPassword")]
    [DataType(DataType.Password)]
    public string? ConfirmNewPassword { get; set; }
}