using Microsoft.Azure.Cosmos;
using Orbital.Interfaces;
using Orbital.Sample.WebApi.SimpleContainerExample;

namespace Orbital.Sample.WebApi;

public static class SampleItemEndpoints
{
    public static RouteGroupBuilder MapSampleItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/simple-container");

        group.MapGet("/{id}", GetAsync).WithName("Get Sample Item");
        group.MapPost("/", CreateAsync).WithName("Create Sample Item");
        group.MapPut("/", UpdateAsync).WithName("Update Sample Item");
        group.MapDelete("/{id}", DeleteAsync).WithName("Delete Sample Item");

        return group;
    }

    private static async Task<IResult> GetAsync(string id, IRepository<SampleItem, SimpleContainer> repository)
    {
        var item = await repository.GetAsync(id, new PartitionKey(id));
        return item is not null ? Results.Ok(item) : Results.NotFound();
    }

    private static async Task<IResult> CreateAsync(SampleItem item, IRepository<SampleItem, SimpleContainer> repository)
    {
        await repository.CreateAsync(item, new PartitionKey(item.Id));
        return Results.Created($"/simple-container/{item.Id}", item);
    }

    private static  async Task<IResult> UpdateAsync(SampleItem item, IRepository<SampleItem, SimpleContainer> repository)
    {
        await repository.UpsertAsync(item, new PartitionKey(item.Id), item.Etag!);
        return Results.Ok(item);
    }

    private static async Task<IResult> DeleteAsync(string id, IRepository<SampleItem, SimpleContainer> repository)
    {
        await repository.DeleteAsync(id, new PartitionKey(id));
        return Results.NoContent();
    }
}