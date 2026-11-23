using System.Security.Claims;
using ClaimsApi.Auth;

namespace ClaimsApi.Tests.Auth;

public class ClaimsPrincipalExtensionsTest
{
    [Fact]
    public void GetUserId_NameIdentifierClaim_ReturnsId()
    {
        // Arrange
        var user = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "104")], "test")
        );

        // Act
        var id = user.GetUserId();

        // Assert
        Assert.Equal(104, id);
    }
}
