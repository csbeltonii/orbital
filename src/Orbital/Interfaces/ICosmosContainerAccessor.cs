using Microsoft.Azure.Cosmos;

namespace Orbital.Interfaces;

public interface ICosmosContainerAccessor
{
    public Container Container { get; }
}