using System.ComponentModel.DataAnnotations;

namespace TaskManager.Model
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set;}
        [Required]
        public string PasswordHash { get; set;}

        public string? RefreshToken { get; set;}
        public DateTime RefreshTokenExpiryTime { get; set;}
    }
}
