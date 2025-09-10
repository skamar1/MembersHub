using MembersHub.Core.Entities;

namespace MembersHub.Core.Interfaces;

public interface IEmailConfigurationService
{
    Task<EmailSettings?> GetActiveEmailSettingsAsync();
    Task<EmailSettings?> GetEmailSettingsByIdAsync(int id);
    Task<List<EmailSettings>> GetAllEmailSettingsAsync();
    Task<EmailSettings> CreateEmailSettingsAsync(EmailSettings settings, string currentUser);
    Task<EmailSettings> UpdateEmailSettingsAsync(EmailSettings settings, string currentUser);
    Task DeleteEmailSettingsAsync(int id);
    Task SetActiveEmailSettingsAsync(int id, string currentUser);
    Task<bool> TestEmailConnectionAsync(int settingsId);
    Task<bool> TestEmailConnectionAsync(EmailSettings settings);
}