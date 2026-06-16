using System;
using System.Security.Cryptography;
using System.Text;
using CFCHub.Application.Common.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace CFCHub.Infrastructure.Security;

public class DataProtectionService : IDataProtectionService
{
    private readonly ISecretsManagerService _secretsManagerService;
    private readonly IMemoryCache _memoryCache;
    private readonly string _keyPrefix;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;

    public DataProtectionService(
        ISecretsManagerService secretsManagerService,
        IMemoryCache memoryCache,
        IConfiguration configuration)
    {
        _secretsManagerService = secretsManagerService;
        _memoryCache = memoryCache;
        _keyPrefix = configuration["CFCHUB_DATA_PROTECTION_KEY_PREFIX"] ?? "cfchub/dataprotection/";
    }

    public string Encrypt(string plaintext, string tenantId)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext;

        var keyMaterial = GetKeyMaterial(tenantId);
        byte[] key = Convert.FromBase64String(keyMaterial);
        
        if (key.Length != KeySize)
        {
            throw new System.Security.SecurityException("Invalid encryption key size.");
        }

        byte[] nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] ciphertext = new byte[plaintextBytes.Length];
        byte[] tag = new byte[TagSize];

        using (var aesGcm = new AesGcm(key, TagSize))
        {
            aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
        }

        byte[] result = new byte[NonceSize + ciphertext.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, NonceSize + ciphertext.Length, TagSize);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertext, string tenantId)
    {
        if (string.IsNullOrEmpty(ciphertext)) return ciphertext;

        byte[] cipherBytes;
        try
        {
            cipherBytes = Convert.FromBase64String(ciphertext);
        }
        catch (FormatException)
        {
            return ciphertext;
        }

        if (cipherBytes.Length < NonceSize + TagSize)
        {
            throw new System.Security.SecurityException("Invalid ciphertext length.");
        }

        var keyMaterial = GetKeyMaterial(tenantId);
        
        try
        {
            return TryDecrypt(cipherBytes, keyMaterial);
        }
        catch (AuthenticationTagMismatchException)
        {
            var prevKeyMaterial = GetPreviousKeyMaterial(tenantId);
            if (prevKeyMaterial != null)
            {
                try
                {
                    return TryDecrypt(cipherBytes, prevKeyMaterial);
                }
                catch (AuthenticationTagMismatchException)
                {
                    throw new System.Security.SecurityException("Decryption failed. Invalid key.");
                }
            }
            throw new System.Security.SecurityException("Decryption failed. Invalid key.");
        }
    }

    private string TryDecrypt(byte[] cipherBytes, string keyMaterial)
    {
        byte[] key = Convert.FromBase64String(keyMaterial);
        
        byte[] nonce = new byte[NonceSize];
        Buffer.BlockCopy(cipherBytes, 0, nonce, 0, NonceSize);

        int ciphertextSize = cipherBytes.Length - NonceSize - TagSize;
        byte[] ciphertextBytes = new byte[ciphertextSize];
        Buffer.BlockCopy(cipherBytes, NonceSize, ciphertextBytes, 0, ciphertextSize);

        byte[] tag = new byte[TagSize];
        Buffer.BlockCopy(cipherBytes, NonceSize + ciphertextSize, tag, 0, TagSize);

        byte[] plaintextBytes = new byte[ciphertextSize];

        using (var aesGcm = new AesGcm(key, TagSize))
        {
            aesGcm.Decrypt(nonce, ciphertextBytes, tag, plaintextBytes);
        }

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    private string GetKeyMaterial(string tenantId)
    {
        string cacheKey = $"dp_key_{tenantId}";
        
        if (!_memoryCache.TryGetValue(cacheKey, out string? keyMaterial))
        {
            string secretName = $"{_keyPrefix}{tenantId}";
            keyMaterial = _secretsManagerService.GetSecret(secretName);
            
            if (string.IsNullOrEmpty(keyMaterial))
            {
                throw new System.Security.SecurityException($"Encryption key not found for tenant {tenantId}.");
            }

            _memoryCache.Set(cacheKey, keyMaterial, TimeSpan.FromSeconds(300));
        }

        return keyMaterial!;
    }

    private string? GetPreviousKeyMaterial(string tenantId)
    {
        string cacheKey = $"dp_key_prev_{tenantId}";
        
        if (!_memoryCache.TryGetValue(cacheKey, out string? keyMaterial))
        {
            string secretName = $"{_keyPrefix}{tenantId}/prev";
            keyMaterial = _secretsManagerService.GetSecret(secretName);
            
            if (!string.IsNullOrEmpty(keyMaterial))
            {
                _memoryCache.Set(cacheKey, keyMaterial, TimeSpan.FromSeconds(300));
            }
            else
            {
                 _memoryCache.Set(cacheKey, "not_found", TimeSpan.FromSeconds(300));
                 return null;
            }
        }
        
        if (keyMaterial == "not_found") return null;

        return keyMaterial;
    }
}
