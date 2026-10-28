namespace Products.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("HTTP {Method} {Path} отменён клиентом.", context.Request.Method, context.Request.Path);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var problem = HttpProblemDetailsFactory.Create(exception);

            if (problem.Status == StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Ошибка обработки HTTP {Method} {Path}", context.Request.Method, context.Request.Path);
            }

            problem.Instance = context.Request.Path;
            problem.Extensions["traceId"] = context.TraceIdentifier;
            context.Response.Clear();
            context.Response.StatusCode = problem.Status!.Value;
            await context.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null,
                contentType: "application/problem+json", cancellationToken: context.RequestAborted);
        }
    }
}
