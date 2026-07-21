using BGA.API.E2ETests.Infrastructure;
using System.Net;
using System.Text.Json;

namespace BGA.API.E2ETests;

public class SwaggerApiTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.HttpClient;

    [Fact]
    public async Task GET_SwaggerJson_ForProtectedEndpoint_ShouldReferenceBearerSecurityScheme()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var contentStream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        var security = document.RootElement
            .GetProperty("paths")
            .GetProperty("/events/{id}/book")
            .GetProperty("post")
            .GetProperty("security");

        Assert.Equal(JsonValueKind.Array, security.ValueKind);
        Assert.NotEmpty(security.EnumerateArray());

        var requirement = security.EnumerateArray().Single();
        Assert.True(requirement.TryGetProperty("Bearer", out var scopes));
        Assert.Equal(JsonValueKind.Array, scopes.ValueKind);
    }
}
