using System.ComponentModel.DataAnnotations;

namespace ADWebApplication.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Employee ID is required")]
        public string EmployeeId { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; }
    }
}