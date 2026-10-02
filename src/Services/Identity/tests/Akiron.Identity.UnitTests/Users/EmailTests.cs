using System.Globalization;
using Akiron.Identity.Domain.Common;
using Akiron.Identity.Domain.Users;
using AwesomeAssertions;

namespace Akiron.Identity.UnitTests.Users;

public sealed class EmailTests
{
    [Fact]
    public void Create_TrimsAndLowerCases()
    {
        Email.Create("  Info@Akiron.DEV ").Value.Should().Be("info@akiron.dev");
    }

    [Fact]
    public void Create_OnATurkishMachine_StillLowerCasesIToI()
    {
        // Culture-aware ToLower() under tr-TR maps I to dotless ı, which would make the
        // same address typed here and on the server two different accounts.
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

        try
        {
            Email.Create("INFO@AKIRON.DEV").Value.Should().Be("info@akiron.dev");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    [InlineData("two@@akiron.dev")]
    [InlineData("name@nodot")]
    [InlineData("with space@akiron.dev")]
    public void Create_WithAMalformedAddress_IsRejected(string raw)
    {
        var create = () => Email.Create(raw);

        create.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be(IdentityErrorCodes.EmailInvalidFormat);
    }

    [Fact]
    public void Create_LongerThanSmtpAllows_IsRejected()
    {
        var create = () => Email.Create($"{new string('a', 250)}@x.co");

        create.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be(IdentityErrorCodes.EmailTooLong);
    }
}
