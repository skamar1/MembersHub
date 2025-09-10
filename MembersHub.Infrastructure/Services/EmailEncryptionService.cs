using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using MembersHub.Core.Interfaces;

namespace MembersHub.Infrastructure.Services;

public class EmailEncryptionService : IEmailEncryptionService
{
    private readonly string _encryptionKey;
    
    public EmailEncryptionService(IConfiguration configuration)
    {
        _encryptionKey = configuration["EmailEncryption:Key"] ?? GenerateDefaultKey();
    }
    
    public string EncryptPassword(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword))
            return string.Empty;
            
        try
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(_encryptionKey);
            aes.GenerateIV();
            
            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainPassword);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            
            // Combine IV and encrypted data
            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);
            
            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to encrypt email password", ex);
        }
    }
    
    public string DecryptPassword(string encryptedPassword)
    {
        if (string.IsNullOrEmpty(encryptedPassword))
            return string.Empty;
            
        try
        {
            var fullData = Convert.FromBase64String(encryptedPassword);
            
            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(_encryptionKey);
            
            // Extract IV from the beginning of the data
            var iv = new byte[aes.IV.Length];
            var encryptedData = new byte[fullData.Length - iv.Length];
            Array.Copy(fullData, 0, iv, 0, iv.Length);
            Array.Copy(fullData, iv.Length, encryptedData, 0, encryptedData.Length);
            
            aes.IV = iv;
            
            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
            
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to decrypt email password", ex);
        }
    }
    
    public bool ValidateEncryptionKey()
    {
        try
        {
            var testData = "test-encryption-validation";
            var encrypted = EncryptPassword(testData);
            var decrypted = DecryptPassword(encrypted);
            
            return testData == decrypted;
        }
        catch
        {
            return false;
        }
    }
    
    private static string GenerateDefaultKey()
    {
        // Use a fixed key for development - in production, this should be in configuration
        // This ensures that encrypted data remains readable across application restarts
        var fixedKey = "MembersHubEmailEncryptionKey2024SecretKey!";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fixedKey));
        return Convert.ToBase64String(keyBytes);
    }
}