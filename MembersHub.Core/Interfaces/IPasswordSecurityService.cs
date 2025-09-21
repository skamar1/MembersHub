namespace MembersHub.Core.Interfaces;

public interface IPasswordSecurityService
{
    Task<bool> IsPasswordCompromisedAsync(string password);
    Task<bool> HasPasswordBeenUsedBeforeAsync(int userId, string password);
    Task AddPasswordToHistoryAsync(int userId, string passwordHash);
    Task<List<DateTime>> GetPasswordHistoryAsync(int userId, int limit = 10);
    Task CleanupOldPasswordHistoryAsync(int daysToKeep = 365);
    Task<PasswordStrengthResult> AnalyzePasswordStrengthAsync(string password);
}

public class PasswordStrengthResult
{
    public int Score { get; set; }
    public PasswordStrengthLevel Level { get; set; }
    public List<string> Suggestions { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public bool MeetsMinimumRequirements { get; set; }
}

public enum PasswordStrengthLevel
{
    VeryWeak = 0,
    Weak = 1,
    Fair = 2,
    Strong = 3,
    VeryStrong = 4
}