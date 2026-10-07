using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SampleApi.Tests;

public class DoneItemsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DoneItemsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Done_ReturnsEmptyArray_WhenNoItemsAreDone()
    {
        var client = _factory.WithWebHostBuilder(_ => { }).CreateClient();

        var response = await client.GetAsync("/api/items/done");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var done = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, done.ValueKind);
        Assert.Equal(0, done.GetArrayLength());
    }

    [Fact]
    public async Task Done_ReturnsOnlyCompletedItems_InIdOrder()
    {
        var client = _factory.WithWebHostBuilder(_ => { }).CreateClient();

        var ids = new List<int>();
        foreach (var name in new[] { "a", "b", "c" })
        {
            var created = await client.PostAsJsonAsync("/api/items", new { name });
            ids.Add((await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
        }

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/items/{ids[2]}/complete", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/items/{ids[0]}/complete", null)).StatusCode);

        var done = await client.GetFromJsonAsync<JsonElement>("/api/items/done");
        var doneIds = done.EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();

        Assert.All(done.EnumerateArray(), e => Assert.True(e.GetProperty("done").GetBoolean()));
        Assert.Contains(ids[0], doneIds);
        Assert.Contains(ids[2], doneIds);
        Assert.DoesNotContain(ids[1], doneIds);
        Assert.Equal(doneIds.OrderBy(i => i).ToList(), doneIds);
    }
}
