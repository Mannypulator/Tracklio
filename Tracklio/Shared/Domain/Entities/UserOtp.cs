namespace Tracklio.Shared.Domain.Entities;

public class UserOtp
{
    public Guid Id { get; set; }
    public string OneTimePassword { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    
}