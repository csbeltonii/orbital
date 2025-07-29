using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Cosmos;
using Orbital.Interfaces;
using Orbital.Samples.Models.HierarchicalContainerExample;

namespace Orbital.Samples.Models;

public static class OrganizationItemEndpoints
{
    private static PartitionKey BuildPartitionKey(string orgId, string id) 
        => new PartitionKeyBuilder()
           .Add(orgId)
           .Add(id)
           .Build();

    public static RouteGroupBuilder MapOrganizationItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/hierarchical-container");

        group.MapGet("/{id}", GetAsync).WithName("Get Organization Item");
        group.MapPost("/", CreateAsync).WithName("Create Organization Item");
        group.MapPut("/", UpdateAsync).WithName("Update Organization Item");
        group.MapDelete("/{id}/{orgId}", DeleteAsync).WithName("Delete Organization Item");

        return group;
    }

    private static async Task<IResult> GetAsync(
        string id,
        [FromQuery] string orgId,
        IRepository<OrganizationItem, HierarchicalContainer> repository)
    {
        var item = await repository.GetAsync(
            id,
            BuildPartitionKey(orgId, id)
        );

        return item is not null ? Results.Ok(item) : Results.NotFound();
    }

    private static async Task<IResult> CreateAsync(
        OrganizationItem item,
        IRepository<OrganizationItem, HierarchicalContainer> repository)
    {
        await repository.CreateAsync(
            item,
            BuildPartitionKey(item.OrgId, item.Id)
        );

        return Results.Created($"/organization-item/{item.Id}?orgId={item.OrgId}", item);
    }

    private static async Task<IResult> UpdateAsync(
        OrganizationItem item,
        IRepository<OrganizationItem, HierarchicalContainer> repository)
    {
        await repository.UpsertAsync(
            item, 
            BuildPartitionKey(item.OrgId, item.Id),
            item.Etag);

        return Results.Ok(item);
    }

    private static async Task<IResult> DeleteAsync(
        string id,
        string orgId,
        IRepository<OrganizationItem, HierarchicalContainer> repository)
    {
        await repository.DeleteAsync(id, BuildPartitionKey(orgId, id));
        return Results.NoContent();
    }
}