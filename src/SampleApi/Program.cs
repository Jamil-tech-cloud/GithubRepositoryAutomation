using Microsoft.OpenApi;
using SampleApi.Models;
using SampleApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sample Items API",
        Version = "v1",
        Description = "Demo API for the AI-assisted delivery POC. Items are kept in memory and reset on every deploy."
    });
});
builder.Services.AddSingleton<ItemService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sample Items API v1"));

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("GetHealth")
    .WithSummary("Health check")
    .WithDescription("Returns 200 when the service is running. Used by the deploy pipeline.");

app.MapGet("/api/items", (ItemService items) => Results.Ok(items.GetAll()))
    .WithName("GetItems")
    .WithSummary("List all items")
    .WithDescription("Returns every item, ordered by id.");

app.MapGet("/api/items/{id:int}", (int id, ItemService items) =>
    {
        var item = items.GetById(id);
        return item is null ? Results.NotFound() : Results.Ok(item);
    })
    .WithName("GetItemById")
    .WithSummary("Get an item by id")
    .WithDescription("Returns the item with the given id, or 404 if it does not exist.");

app.MapGet("/api/items/{id:int}/exists", (int id, ItemService items) =>
        items.GetById(id) is null ? Results.NotFound() : Results.Ok())
    .WithName("ItemExists")
    .WithSummary("Check whether an item exists")
    .WithDescription("Returns 200 with no body if the item exists, otherwise 404.");

app.MapPost("/api/items", (CreateItemRequest request, ItemService items) =>
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Name is required." });
        }

        var created = items.Add(request.Name);
        return Results.Created($"/api/items/{created.Id}", created);
    })
    .WithName("CreateItem")
    .WithSummary("Create an item")
    .WithDescription("Creates a new, not-yet-done item. Returns 400 if the name is blank.");

app.MapPost("/api/items/{id:int}/complete", (int id, ItemService items) =>
    {
        var updated = items.Complete(id);
        return updated is null ? Results.NotFound() : Results.Ok(updated);
    })
    .WithName("CompleteItem")
    .WithSummary("Mark an item as done")
    .WithDescription("Sets done=true on the item with the given id, or returns 404 if it does not exist.");

app.MapDelete("/api/items/{id:int}", (int id, ItemService items) =>
        items.Delete(id) ? Results.NoContent() : Results.NotFound())
    .WithName("DeleteItem");

app.Run();

record CreateItemRequest(string Name);

public partial class Program;
