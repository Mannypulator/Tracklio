namespace Tracklio.Shared.Domain.Entities;

public class UserDevice
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public string DeviceToken { get; set; }
    public string Platform { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}