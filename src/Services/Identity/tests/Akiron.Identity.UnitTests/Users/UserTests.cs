using Akiron.Identity.Domain.Common;
using Akiron.Identity.Domain.Users;
using AwesomeAssertions;

namespace Akiron.Identity.UnitTests.Users;

public sealed class UserTests
{
    private static readonly Email SomeEmail = Email.Create("yavuz@akiron.dev");

    [Fact]
    public void Register_KeepsTheHashItWasGiven_AndTrimsTheName()
    {
        var user = User.Register(SomeEmail, "  Yavuz Çelik ", "hash-from-the-hasher");

        user.DisplayName.Should().Be("Yavuz Çelik");
        user.PasswordHash.Should().Be("hash-from-the-hasher");
        user.Id.Value.Version.Should().Be(7);
        user.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Register_WithoutAHash_IsAProgrammingError()
    {
        var register = () => User.Register(SomeEmail, "Yavuz", " ");

        register.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Register_WithoutADisplayName_IsRejected(string? displayName)
    {
        var register = () => User.Register(SomeEmail, displayName, "hash");

        register.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be(IdentityErrorCodes.TextRequired);
    }

    [Fact]
    public void UpgradePasswordHash_ReplacesTheHash_AndStampsTheUpdate()
    {
        var user = User.Register(SomeEmail, "Yavuz", "old-hash");

        user.UpgradePasswordHash("new-hash");

        user.PasswordHash.Should().Be("new-hash");
        user.UpdatedAt.Should().NotBeNull();
    }
}
