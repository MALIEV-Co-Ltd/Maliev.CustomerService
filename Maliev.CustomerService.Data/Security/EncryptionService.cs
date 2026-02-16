using System.Security.Cryptography;
using System.Text;
using Maliev.CustomerService.Data.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Maliev.CustomerService.Data.Security;

/// <summary>
/// AES-256 encryption service for protecting sensitive data at rest (Thai National IDs, etc.)
/// Encryption key is stored in Google Secret Manager and mounted at /mnt/secrets
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _encryptionKey;
    private const int KeySize = 256; // AES-256
    private const int IvSize = 16; // 128 bits for AES

    /// <summary>
    /// Initializes a new instance of the EncryptionService class
    /// </summary>
    /// <param name="configuration">Configuration containing encryption key or environment settings</param>
    public EncryptionService(IConfiguration configuration)
    {
        // Try to get encryption key from configuration (Google Secret Manager)
        var encryptionKeyBase64 = configuration["Encryption:Key"];

        if (string.IsNullOrEmpty(encryptionKeyBase64))
        {
            // For development/testing, generate a deterministic key
            // In production, this should NEVER be used - key must come from Secret Manager
            var isDevelopment = configuration["ASPNETCORE_ENVIRONMENT"] == "Development" ||
                                configuration["ASPNETCORE_ENVIRONMENT"] == "Testing";

            if (isDevelopment)
            {
                // Development-only: Use a fixed 32-byte key for testing (exactly 32 characters)
                _encryptionKey = Encoding.UTF8.GetBytes("DevEncryptionKey12345678901234!!");
            }
            else
            {
                throw new InvalidOperationException(
                    "Encryption:Key not found in configuration. " +
                    "Ensure Google Secret Manager is properly configured with the encryption key.");
            }
        }
        else
        {
            _encryptionKey = Convert.FromBase64String(encryptionKeyBase64);
        }

        // Validate key size
        if (_encryptionKey.Length != KeySize / 8)
        {
            throw new InvalidOperationException(
                $"Encryption key must be {KeySize / 8} bytes (256 bits) for AES-256. " +
                $"Current key is {_encryptionKey.Length} bytes.");
        }
    }

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Generate a random IV for each encryption operation
        aes.GenerateIV();
        var iv = aes.IV;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();

        // Write IV to the beginning of the stream
        ms.Write(iv, 0, iv.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var writer = new StreamWriter(cs))
        {
            writer.Write(plainText);
        }

        var encrypted = ms.ToArray();
        return Convert.ToBase64String(encrypted);
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        if (!IsEncrypted(cipherText))
            return cipherText; // Already decrypted

        try
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Extract IV from the beginning of the encrypted data
            var iv = new byte[IvSize];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;

            // Extract the actual encrypted data
            var cipher = new byte[fullCipher.Length - IvSize];
            Array.Copy(fullCipher, IvSize, cipher, 0, cipher.Length);

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(cipher);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs);

            return reader.ReadToEnd();
        }
        catch (FormatException)
        {
            // Not a valid Base64 string, assume it's already decrypted
            return cipherText;
        }
        catch (CryptographicException)
        {
            // Decryption failed, possibly wrong key or corrupted data
            throw new InvalidOperationException("Failed to decrypt data. The encryption key may be incorrect.");
        }
    }

    /// <inheritdoc />
    public bool IsEncrypted(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        // Check if the string is valid Base64
        try
        {
            var buffer = Convert.FromBase64String(value);
            // Valid Base64 and length suggests it's encrypted (IV + data)
            return buffer.Length > IvSize;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
