namespace Maliev.CustomerService.Data.Interfaces;

/// <summary>
/// Service for encrypting and decrypting sensitive data using AES-256
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a plaintext string using AES-256 encryption
    /// </summary>
    /// <param name="plainText">The text to encrypt</param>
    /// <returns>Base64-encoded encrypted string</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts an encrypted string using AES-256 decryption
    /// </summary>
    /// <param name="cipherText">Base64-encoded encrypted string</param>
    /// <returns>Decrypted plaintext string</returns>
    string Decrypt(string cipherText);

    /// <summary>
    /// Checks if a string appears to be encrypted (Base64 format)
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <returns>True if the value appears encrypted</returns>
    bool IsEncrypted(string value);
}
