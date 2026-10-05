using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SampleApi.Tests;

public class ItemExistsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ItemExistsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Exists_ReturnsOkWithNoBody_WhenItemExists()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/items/1/exists");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Exists_ReturnsNotFoundWithNoBody_WhenItemMissing()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/items/999999/exists");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }
}
