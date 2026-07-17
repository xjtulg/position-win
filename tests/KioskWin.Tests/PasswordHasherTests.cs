using KioskWin.Core;
using Xunit;

namespace KioskWin.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Verify_accepts_correct_password()
    {
        var result = PasswordHasher.Generate("s3cret!");
        Assert.True(PasswordHasher.Verify("s3cret!", result.Hash, result.Salt));
    }

    [Fact]
    public void Verify_rejects_wrong_password()
    {
        var result = PasswordHasher.Generate("s3cret!");
        Assert.False(PasswordHasher.Verify("wrong", result.Hash, result.Salt));
    }

    [Fact]
    public void Generate_produces_different_salt_each_call()
    {
        var a = PasswordHasher.Generate("same");
        var b = PasswordHasher.Generate("same");
        Assert.NotEqual(a.Salt, b.Salt);
        Assert.NotEqual(a.Hash, b.Hash);
    }

    [Fact]
    public void Verify_rejects_empty_hash_or_salt()
    {
        Assert.False(PasswordHasher.Verify("x", "", "salt"));
        Assert.False(PasswordHasher.Verify("x", "hash", ""));
    }

    [Fact]
    public void Verify_rejects_non_hex_input_without_throwing()
    {
        Assert.False(PasswordHasher.Verify("x", "not-hex!@#", "zzzz"));
    }

    [Fact]
    public void Verify_rejects_non_hex_hash_of_correct_length()
    {
        var result = PasswordHasher.Generate("pw");
        var badHash = new string('g', 64); // 64 chars (matches SHA-256 hex length) but not valid hex
        Assert.False(PasswordHasher.Verify("pw", badHash, result.Salt));
    }
}
