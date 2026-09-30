using SampleApi.Models;
using SampleApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<ItemService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("GetHealth");

app.MapGet("/api/items", (ItemService items) => Results.Ok(items.GetAll()))
    .WithName("GetItems");

app.MapGet("/api/items/{id:int}", (int id, ItemService items) =>
    {
        var item = items.GetById(id);
        return item is null ? Results.NotFound() : Results.Ok(item);
    })
    .WithName("GetItemById");

app.MapPost("/api/items", (CreateItemRequest request, ItemService items) =>
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Name is required." });
        }

        var created = items.Add(request.Name);
        return Results.Created($"/api/items/{created.Id}", created);
    })
    .WithName("CreateItem");

app.MapPost("/api/items/{id:int}/complete", (int id, ItemService items) =>
    {
        var updated = items.Complete(id);
        return updated is null ? Results.NotFound() : Results.Ok(updated);
    })
    .WithName("CompleteItem");

app.Run();

record CreateItemRequest(string Name);

public partial class Program;
// test change
