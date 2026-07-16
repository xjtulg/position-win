using System.Security.Cryptography;
using System.Text;

namespace PositionKiosk.Core;

public sealed record PasswordHashResult(string Hash, string Salt);

public static class PasswordHasher
{
    private const int SaltBytes = 16;

    public static PasswordHashResult Generate(string password)
    {
        Span<byte> salt = stackalloc byte[SaltBytes];
        RandomNumberGenerator.Fill(salt);
        var saltArray = salt.ToArray();
        return new PasswordHashResult(ComputeHash(password, saltArray), Convert.ToHexString(saltArray));
    }

    public static bool Verify(string password, string expectedHashHex, string saltHex)
    {
        if (string.IsNullOrEmpty(expectedHashHex) || string.IsNullOrEmpty(saltHex))
            return false;

        byte[] salt;
        try { salt = Convert.FromHexString(saltHex); }
        catch { return false; }

        var actualHashHex = ComputeHash(password, salt);

        if (actualHashHex.Length != expectedHashHex.Length)
            return false;

        var actual = Convert.FromHexString(actualHashHex);

        byte[] expected;
        try { expected = Convert.FromHexString(expectedHashHex); }
        catch { return false; }

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static string ComputeHash(string password, byte[] salt)
    {
        var pw = Encoding.UTF8.GetBytes(password);
        var buf = new byte[pw.Length + salt.Length];
        Buffer.BlockCopy(pw, 0, buf, 0, pw.Length);
        Buffer.BlockCopy(salt, 0, buf, pw.Length, salt.Length);
        return Convert.ToHexString(SHA256.HashData(buf));
    }
}
