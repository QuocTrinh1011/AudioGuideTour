using System.ComponentModel.DataAnnotations;

namespace AudioGuideAdmin.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }
}