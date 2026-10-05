using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SampleApi.Tests;

public class ItemCountEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ItemCountEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Count_MatchesNumberOfListedItems_AndIncreasesAfterCreate()
    {
        var client = _factory.CreateClient();

        var listed = await client.GetFromJsonAsync<JsonElement>("/api/items");
        var before = await client.GetFromJsonAsync<JsonElement>("/api/items/count");
        Assert.Equal(listed.GetArrayLength(), before.GetProperty("count").GetInt32());

        var create = await client.PostAsJsonAsync("/api/items", new { name = "counted" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var after = await client.GetFromJsonAsync<JsonElement>("/api/items/count");
        Assert.Equal(before.GetProperty("count").GetInt32() + 1, after.GetProperty("count").GetInt32());
    }
}
