using BetterMe.Shared.Enums;

namespace BetterMe.Shared.Auth;

public static class RegistrationRoles
{
    public static bool IsSelfAssignable(UserRole role) =>
        role is UserRole.Patient or UserRole.Psychologist;
}
