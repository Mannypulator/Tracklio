using System.Security.Cryptography;

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
    
    public static string GenerateSecureOtp(int length)
    {
        if (length < 4 || length > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(length), 
                "OTP length must be between 4 and 10 digits");
        }

        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
            
        // Convert to positive integer
        var randomValue = Math.Abs(BitConverter.ToInt32(bytes, 0));
            
        // Calculate min and max values based on length
        var min = (int)Math.Pow(10, length - 1);
        var max = (int)Math.Pow(10, length) - 1;
            
        // Scale the random value to our range
        var otp = (randomValue % (max - min + 1)) + min;
            
        return otp.ToString();
    }
    
}