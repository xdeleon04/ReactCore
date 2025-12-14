using System.Text.RegularExpressions;

namespace ReactCore.Backend.Services;

public interface IPasswordValidator
{
    bool ValidatePassword(string password);
    string GetStrengthFeedback(string password);
}

public class PasswordValidator : IPasswordValidator
{
    public bool ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return false;

        // Check for mixed case
        if (!Regex.IsMatch(password, "[a-z]") || !Regex.IsMatch(password, "[A-Z]"))
            return false;

        // Check for number
        if (!Regex.IsMatch(password, "[0-9]"))
            return false;

        // Check for special character
        if (!Regex.IsMatch(password, "[^a-zA-Z0-9]"))
            return false;

        return true;
    }

    public string GetStrengthFeedback(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "Password is required.";

        if (password.Length < 8)
            return "Password must be at least 8 characters long.";

        if (!Regex.IsMatch(password, "[a-z]") || !Regex.IsMatch(password, "[A-Z]"))
            return "Password must contain both uppercase and lowercase letters.";

        if (!Regex.IsMatch(password, "[0-9]"))
            return "Password must contain at least one number.";

        if (!Regex.IsMatch(password, "[^a-zA-Z0-9]"))
            return "Password must contain at least one special character.";

        return "Strong";
    }
}
