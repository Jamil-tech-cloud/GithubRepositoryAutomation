using SampleApi.Services;
using Xunit;

namespace SampleApi.Tests;

public class ItemServiceTests
{
    [Fact]
    public void GetAll_ReturnsSeededItems()
    {
        var service = new ItemService();

        var items = service.GetAll();

        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void Add_AssignsIncrementingId_AndDefaultsToNotDone()
    {
        var service = new ItemService();

        var created = service.Add("New item");

        Assert.False(created.Done);
        Assert.Equal("New item", created.Name);
        Assert.Equal(created, service.GetById(created.Id));
    }

    [Fact]
    public void Complete_MarksExistingItemDone()
    {
        var service = new ItemService();
        var created = service.Add("To finish");

        var completed = service.Complete(created.Id);

        Assert.NotNull(completed);
        Assert.True(completed!.Done);
    }

    [Fact]
    public void Complete_ReturnsNull_ForUnknownId()
    {
        var service = new ItemService();

        var result = service.Complete(9999);

        Assert.Null(result);
    }

    [Fact]
    public void Delete_ExistingItem_RemovesItAndReturnsTrue()
    {
        var service = new ItemService();
        var created = service.Add("To delete");

        Assert.True(service.Delete(created.Id));
        Assert.Null(service.GetById(created.Id));
    }

    [Fact]
    public void Delete_MissingItem_ReturnsFalse()
    {
        var service = new ItemService();

        Assert.False(service.Delete(9999));
    }
}
