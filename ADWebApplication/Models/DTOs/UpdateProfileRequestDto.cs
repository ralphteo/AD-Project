using System.ComponentModel.DataAnnotations;

namespace ADWebApplication.Models.DTOs
{
    public class UpdateProfileRequestDto
    {
        [StringLength(50)]
        public string? PhoneNumber { get; set; }

        public int? RegionId { get; set; }
    }
}
