namespace Tracklio.Shared.Configurations;

public class Authentication
{
    public Google Google { get; set; }
}

public class Google
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string CallbackPath { get; set; }
    public string AuthUrl { get; set; }
}