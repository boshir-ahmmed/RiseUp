using System.Text;

namespace RiseUp.Framework.Constants;

public static class PasswordPolicy
{
    public const int MinimumLength = 12;
    public const string Requirements = "Use at least 12 characters, including an uppercase letter, a lowercase letter, and a number.";
    public static string BrowserPattern => @"(?=.*\p{Lu})(?=.*\p{Ll})(?=.*\p{Nd}).{" + MinimumLength + ",}";

    public static bool IsValid(string? password)
    {
        if (password is null || password.Length < MinimumLength) return false;
        var characters = password.EnumerateRunes();
        return characters.Count() >= MinimumLength && characters.Any(Rune.IsUpper)
            && characters.Any(Rune.IsLower) && characters.Any(Rune.IsDigit);
    }
}
