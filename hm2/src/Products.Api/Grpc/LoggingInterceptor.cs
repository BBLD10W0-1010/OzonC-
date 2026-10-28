using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Products.Api.Grpc;

public sealed class LoggingInterceptor(ILogger<LoggingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var startedAt = Stopwatch.GetTimestamp();
        logger.LogInformation("gRPC {Method} Request: {Request}", context.Method, request);
        try
        {
            var response = await continuation(request, context);
            logger.LogInformation("gRPC {Method} Response: {Response}; {ElapsedMs} ms",
                context.Method, response, Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            return response;
        }
        catch (RpcException exception)
        {
            logger.LogWarning("gRPC {Method} Response error: {StatusCode}, {Detail}; {ElapsedMs} ms",
                context.Method, exception.StatusCode, exception.Status.Detail,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            throw;
        }
    }
}
