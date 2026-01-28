using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADWebApplication.Models;
    [Table("employee")]
    public class Employee{
        
        [Column("employeeId")]
        public int Id {get; set;}
        [Key]
        [Column("username")]
        [Required, StringLength(50)]
        public string Username { get; set; } = ""; 

        [Column("fullname")]
        [Required, StringLength(100)]
        public string FullName { get; set; } = "";

        [Column("email")]
        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = "";

        [Column("passwordHash")]
        [Required]
        public string PasswordHash { get; set; } = "";

        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        [Column("roleId")]
        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;
    }
