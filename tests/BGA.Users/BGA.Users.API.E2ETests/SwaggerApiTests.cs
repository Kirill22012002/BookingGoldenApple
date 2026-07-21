using BGA.Users.API.E2ETests.Infrastructure;
using System.Net;
using System.Text.Json;

namespace BGA.Users.API.E2ETests;

public class SwaggerApiTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.HttpClient;

    [Fact]
    public async Task GET_SwaggerJson_ShouldContainAuthEndpoints()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var contentStream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        var paths = document.RootElement
            .GetProperty("paths")
            .EnumerateObject()
            .Select(path => path.Name)
            .ToArray();

        Assert.Contains("/auth/register", paths);
        Assert.Contains("/auth/login", paths);
    }
}
