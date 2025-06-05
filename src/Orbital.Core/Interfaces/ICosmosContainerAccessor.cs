using Microsoft.Azure.Cosmos;

namespace Orbital.Core.Interfaces;

public interface ICosmosContainerAccessor
{
    public Container Container { get; }
}