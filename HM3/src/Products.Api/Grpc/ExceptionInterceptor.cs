using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Products.Api.Application;

namespace Products.Api.Grpc;

public sealed class ExceptionInterceptor(ILogger<ExceptionInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (ValidationException exception)
        {
            var errors = string.Join("; ", exception.Errors.Select(x => $"{x.PropertyName}: {x.ErrorMessage}"));
            throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
        }
        catch (ProductNotFoundException exception)
        {
            throw new RpcException(new Status(StatusCode.NotFound, exception.Message));
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Запрос отменён."));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ошибка обработки gRPC {Method}", context.Method);
            throw new RpcException(new Status(StatusCode.Internal, "Внутренняя ошибка сервера."));
        }
    }
}
