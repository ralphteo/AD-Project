using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ADWebApplication.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Employee ID is required")]
        [Display(Name = "Employee ID")]
        public string EmployeeId { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        public bool? RememberMe { get; set; }
    }
}