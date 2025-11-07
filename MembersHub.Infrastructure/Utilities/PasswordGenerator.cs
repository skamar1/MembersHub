using System.Security.Cryptography;
using System.Text;

namespace MembersHub.Infrastructure.Utilities;

public static class PasswordGenerator
{
    private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
    private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string DigitChars = "0123456789";
    private const string SpecialChars = "!@#$%^&*";

    public static string GenerateSecurePassword(int length = 16)
    {
        if (length < 12)
            length = 12;

        var allChars = LowercaseChars + UppercaseChars + DigitChars + SpecialChars;
        var password = new StringBuilder();

        // Ensure at least one of each required character type
        password.Append(GetRandomChar(LowercaseChars));
        password.Append(GetRandomChar(UppercaseChars));
        password.Append(GetRandomChar(DigitChars));
        password.Append(GetRandomChar(SpecialChars));

        // Fill the rest with random characters from all sets
        for (int i = 4; i < length; i++)
        {
            password.Append(GetRandomChar(allChars));
        }

        // Shuffle the password to avoid predictable patterns
        return Shuffle(password.ToString());
    }

    private static char GetRandomChar(string chars)
    {
        var index = RandomNumberGenerator.GetInt32(0, chars.Length);
        return chars[index];
    }

    private static string Shuffle(string str)
    {
        var array = str.ToCharArray();
        int n = array.Length;

        for (int i = n - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }

        return new string(array);
    }
}
