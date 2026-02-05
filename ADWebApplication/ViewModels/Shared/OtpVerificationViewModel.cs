using System.ComponentModel.DataAnnotations;

namespace ADWebApplication.ViewModels
{
    public class OtpVerificationViewModel
    {
        [Required(ErrorMessage = "Otp is Required")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Otp must be 6 digits")]
        public string? OtpCode { get; set; }
    }
}