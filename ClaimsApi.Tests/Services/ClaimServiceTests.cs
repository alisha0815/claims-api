using ClaimsApi.Tests.Fixtures;

namespace ClaimsApi.Tests.Services;

public class ClaimServiceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public void Container_Starts()
    {
        Assert.NotNull(fixture.ConnectionString);
    }
}
