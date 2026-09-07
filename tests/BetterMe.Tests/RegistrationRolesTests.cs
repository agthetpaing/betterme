using BetterMe.Shared.Auth;
using BetterMe.Shared.Enums;

namespace BetterMe.Tests;

public class RegistrationRolesTests
{
    [Theory]
    [InlineData(UserRole.Patient, true)]
    [InlineData(UserRole.Psychologist, true)]
    [InlineData(UserRole.Admin, false)]
    public void IsSelfAssignable_known_roles(UserRole role, bool expected)
    {
        Assert.Equal(expected, RegistrationRoles.IsSelfAssignable(role));
    }

    [Fact]
    public void IsSelfAssignable_rejects_undefined_enum_value()
    {
        Assert.False(RegistrationRoles.IsSelfAssignable((UserRole)99));
    }
}
