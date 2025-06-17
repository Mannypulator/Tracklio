namespace Tracklio.Shared.Services;

public class Util
{
    public static string GenerateOtp()
    {
        var min = 1000000;
        var max = 9999999;

        var random = new Random();
        return random.Next(min, max).ToString();
    }
    
}