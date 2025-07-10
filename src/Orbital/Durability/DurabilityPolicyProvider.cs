using Polly;
using Polly.Registry;

namespace Orbital.Durability;

public class DurabilityPolicyProvider(ResiliencePipelineProvider<string> resiliencePipelineProvider) : IDurabilityPolicyProvider
{
    public const string DEFAULT_POLICY_NAME = "orbital-custom-policy";

    public ResiliencePipeline? GetPolicy(string policyName) => resiliencePipelineProvider.GetPipeline(policyName);
}