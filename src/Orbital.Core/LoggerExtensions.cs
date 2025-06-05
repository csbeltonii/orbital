using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Orbital.Core;

internal static class LoggerExtensions
{
    public static void LogStatistics(this ILogger logger,
                                     string methodName,
                                     string typeName,
                                     string partitionKey,
                                     HttpStatusCode statusCode,
                                     long elapsedMs,
                                     double requestCharge, 
                                     CosmosDiagnostics cosmosDiagnostics) =>
        logger.LogInformation(
            "{MethodName}: {Type} {PartitionKey} returned {StatusCode} in {Time}ms. Request charge: {RequestCharge} RUs. Retries: {RetryCount}.",
            methodName, 
            typeName, 
            partitionKey, 
            statusCode, 
            elapsedMs, 
            requestCharge, 
            cosmosDiagnostics.GetFailedRequestCount()
        );

    public static void LogStatistics(this ILogger logger,
                                     string methodName,
                                     string typeName,
                                     string partitionKey,
                                     HttpStatusCode statusCode,
                                     long elapsedMs,
                                     CosmosDiagnostics cosmosDiagnostics)
    {
        logger.LogInformation(
            "{MethodName}: {Type} {PartitionKey} returned {StatusCode} in {Time}ms. Retries: {RetryCount}.",
            methodName,
            typeName,
            partitionKey,
            statusCode,
            elapsedMs,
            cosmosDiagnostics.GetFailedRequestCount()
        );
    }
}