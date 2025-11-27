using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Nudges.Security;

public sealed class EncryptionSettings {
    /// <summary>
    /// 32 bytes for AES-256. Store as base64 in config and decode.
    /// </summary>
    public required byte[] Key { get; init; }
}

public interface IEncryptionService {
    public string? Encrypt(string? plaintext);
    public string? Decrypt(string? ciphertext);
}

public sealed class AesGcmEncryptionService : IEncryptionService {
    private const int NonceSize = 12;   // 96-bit nonce
    private const int TagSize = 16;   // 128-bit tag (recommended)
    private readonly byte[] _key;

    public AesGcmEncryptionService(IOptions<EncryptionSettings> options) {
        _key = options.Value.Key;
        if (_key.Length != 32) {
            throw new InvalidOperationException("Encryption key must be 32 bytes for AES-256.");
        }
    }

    public string? Encrypt(string? plaintext) {
        if (string.IsNullOrEmpty(plaintext)) {
            return plaintext;
        }

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];
        var ciphertext = new byte[plaintextBytes.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var combined = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, combined, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, combined, NonceSize + TagSize, ciphertext.Length);

        return Convert.ToBase64String(combined);
    }

    public string? Decrypt(string? ciphertext) {
        if (string.IsNullOrEmpty(ciphertext)) {
            return ciphertext;
        }

        var combined = Convert.FromBase64String(ciphertext);

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipherBytes = new byte[combined.Length - NonceSize - TagSize];

        Buffer.BlockCopy(combined, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(combined, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(combined, NonceSize + TagSize, cipherBytes, 0, cipherBytes.Length);

        var plaintextBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
