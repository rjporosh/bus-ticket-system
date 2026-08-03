using BusTicketing.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace BusTicketing.UnitTests.Infrastructure;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _sut = new();

    [Fact]
    public void Hash_ThenVerify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _sut.Hash("Sup3rSecret!");

        _sut.Verify("Sup3rSecret!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _sut.Hash("Sup3rSecret!");

        _sut.Verify("WrongPassword", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        // Different random salts each time -> different stored hash strings, even for the same password.
        var hash1 = _sut.Hash("Sup3rSecret!");
        var hash2 = _sut.Hash("Sup3rSecret!");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Hash_ProducesThreePartFormat()
    {
        var hash = _sut.Hash("Sup3rSecret!");

        hash.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void Verify_WithMalformedHash_ReturnsFalseRatherThanThrowing()
    {
        _sut.Verify("anything", "not-a-valid-hash").Should().BeFalse();
    }
}
