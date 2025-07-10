using Polly;

namespace Orbital.Durability;

public interface IDurabilityPolicyProvider
{
    ResiliencePipeline? GetPolicy(string policyName);
}