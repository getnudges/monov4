using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Nudges.Security;

public sealed class HashService(IOptions<HashSettings> options) {
    private readonly byte[] _key = options.Value.HashKey; // HMAC key

    public string ComputeHash(string normalizedPhoneNumber) {
        using var hmac = new HMACSHA256(_key);
        var bytes = Encoding.UTF8.GetBytes(normalizedPhoneNumber);
        var hash = hmac.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed class HashSettings {
    public required byte[] HashKey { get; init; }
}

