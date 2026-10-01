using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SampleApi.Tests;

public class ItemEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ItemEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Delete_ExistingItem_Returns204_ThenGetReturns404()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/items/1");
        var after = await client.GetAsync("/api/items/1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    [Fact]
    public async Task Delete_MissingItem_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/items/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
