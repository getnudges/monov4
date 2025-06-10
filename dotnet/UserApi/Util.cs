namespace UserApi;

public static class Util {

    public static string MaskString(string input, int n) {
        if (n >= input.Length) {
            return input;
        }

        var maskedPart = new string('*', input.Length - n);
        var unmaskedPart = input[^n..];

        return maskedPart + unmaskedPart;
    }
}
