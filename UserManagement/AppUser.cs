using Microsoft.AspNetCore.Identity;

namespace UserManagement
{
    public class AppUser : IdentityUser
    {
        public Guid? Motorok { get; set; }
        public bool[]? MotorokViewAccess { get; set; } = new bool[14];
        public int MaterialStrengthId { get; set; }
        public bool[]? MaterialStrengthViewAccess { get; set; } = new bool[14];
        public int Tokens { get; set; }
        public string? Role { get; set; }
    }
}
