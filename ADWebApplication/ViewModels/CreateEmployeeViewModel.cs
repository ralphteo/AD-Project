using System.ComponentModel.DataAnnotations;

namespace ADWebApplication.ViewModels
{
    public class CreateEmployeeViewModel
    {
        [Required, StringLength(100)]
        public string FullName { get; set; } = "";

        [Required, StringLength(50)]
        public string Username { get; set; } = "";

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = "";

        [StringLength(30)]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Please choose a role")]
        public string RoleName { get; set; } = "";
            
        [Required]
        [StringLength(100, MinimumLength = 6,
        ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}