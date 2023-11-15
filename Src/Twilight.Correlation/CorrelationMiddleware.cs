using Microsoft.AspNetCore.Http;

namespace Twilight.Correlation;

public sealed class CorrelationMiddleware(ICorrelationProvider correlationProvider) : IMiddleware
{
    private const string CorrelationIdHeaderKey = RequestHeaderKey.CorrelationIdHeader;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderKey, out var correlationId))
        {
            correlationProvider.CorrelationId.Value = correlationId.ToString();
        }
        else
        {
            context.Request.Headers.Append(CorrelationIdHeaderKey, correlationProvider.CorrelationId.Value);
        }

        context.Response.OnStarting(async state =>
        {
            var httpContext = (HttpContext)state;

            if (httpContext.Request.Headers.TryGetValue(CorrelationIdHeaderKey, out var requestCorrelationId))
            {
                httpContext.Response.Headers.Append(CorrelationIdHeaderKey, requestCorrelationId);
            }
            else
            {
                httpContext.Response.Headers.Append(CorrelationIdHeaderKey, correlationProvider.CorrelationId.Value);
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }, context);

        await next(context);
    }
}
