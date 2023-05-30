using Microsoft.AspNetCore.Http;

namespace Twilight.Correlation;

public sealed class CorrelationMiddleware : IMiddleware
{
    private const string CorrelationIdHeaderKey = RequestHeaderKey.CorrelationIdHeader;

    private readonly ICorrelationProvider _correlationProvider;

    public CorrelationMiddleware(ICorrelationProvider correlationProvider) => _correlationProvider = correlationProvider;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderKey, out var correlationId))
        {
            _correlationProvider.CorrelationId.Value = correlationId.ToString();
        }
        else
        {
            context.Request.Headers.Add(CorrelationIdHeaderKey, _correlationProvider.CorrelationId.Value);
        }

        context.Response.OnStarting(async state =>
        {
            var httpContext = (HttpContext)state;

            if (httpContext.Request.Headers.TryGetValue(CorrelationIdHeaderKey, out var requestCorrelationId))
            {
                httpContext.Response.Headers.Add(CorrelationIdHeaderKey, requestCorrelationId);
            }
            else
            {
                httpContext.Response.Headers.Add(CorrelationIdHeaderKey, _correlationProvider.CorrelationId.Value);
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }, context);

        await next(context);
    }
}
