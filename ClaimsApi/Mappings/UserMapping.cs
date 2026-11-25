using ClaimsApi.Dtos.Auth;
using ClaimsApi.Models;

namespace ClaimsApi.Mappings;

public static class UserMappings
{
    public static RegisterResponse ToRegisterResponse(this User user)
    {
        return new RegisterResponse(
            Id: user.Id,
            Username: user.Username,
            Role: user.Role,
            CreatedAt: user.CreatedAt
        );
    }
}
