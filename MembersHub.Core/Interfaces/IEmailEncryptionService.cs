namespace MembersHub.Core.Interfaces;

public interface IEmailEncryptionService
{
    string EncryptPassword(string plainPassword);
    string DecryptPassword(string encryptedPassword);
    bool ValidateEncryptionKey();
}