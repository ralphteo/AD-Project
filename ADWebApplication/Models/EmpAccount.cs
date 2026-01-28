using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADWebApplication.Models;
    public class EmpAccount{
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Username { get; set; } = ""; 

        [Required, StringLength(100)]
        public string FullName { get; set; } = "";

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = "";

        [StringLength(30)]
        public string? PhoneNumber { get; set; }

        [Required]
        public string PasswordHash { get; set; } = "";

        public bool IsActive { get; set; } = true;

        public List<EmpRole> EmpRoles { get; set; } = new List<EmpRole>();
    }
