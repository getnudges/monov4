using OtpNet;

namespace AuthApi;

public interface IOtpVerifier {
    public (string, string) GetOtp();
    public bool ValidateOtp(string key, string otp);
}

public sealed class OtpVerifier : IOtpVerifier {
    private const int Step = 300;

    public (string, string) GetOtp() {
        var key = KeyGeneration.GenerateRandomKey();
        var base32Key = Base32Encoding.ToString(key);
        var otp = new Totp(key, Step);
        var code = otp.ComputeTotp();
        return (code, base32Key);
    }

    public bool ValidateOtp(string key, string otp) {
        var otpKey = Base32Encoding.ToBytes(key);
        var totp = new Totp(otpKey, Step);
        return totp.VerifyTotp(otp, out _);
    }
}
