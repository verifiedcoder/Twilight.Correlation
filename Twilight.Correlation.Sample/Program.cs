using MediatR;
using MediatR.Pipeline;
using Microsoft.AspNetCore.Mvc;
using RT.Comb;
using Twilight.Correlation;

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddEndpointsApiExplorer()
       .AddSwaggerGen()
       .AddSingleton<ICombDateTimeStrategy, UnixDateTimeStrategy>()
       .AddSingleton<ICombProvider, PostgreSqlCombProvider>()
       .AddScoped<ICorrelationProvider, CorrelationProvider>()
       .AddScoped<CorrelationMiddleware>()
       .AddMediatR(config =>
       {
           config.RegisterServicesFromAssemblyContaining<Program>();

           config.AddBehavior(typeof(IRequestPreProcessor<>), typeof(CorrelationPreProcessor<>));
       });

var app = builder.Build();

app.UseSwagger()
   .UseSwaggerUI()
   .UseMiddleware<CorrelationMiddleware>()
   .UseHttpsRedirection();

app.MapGet("/weather/forecasts/{numberOfForecasts:int}", async (IMediator mediator, [FromRoute] int numberOfForecasts) 
               => await GetWeatherForecasts(mediator, numberOfForecasts))
   .WithName(nameof(GetWeatherForecasts))
   .WithOpenApi();

app.Run();

#pragma warning disable S3903 // Types should be defined in named namespaces, as listed below for browsing convenience
async Task<IResult> GetWeatherForecasts(ISender mediator, int numberOfForecasts)
{
    var request = new GetForecasts(numberOfForecasts);

    var response = await mediator.Send(request);

    return Results.Json(response);
}

public record GetForecasts(int NumberOfForecasts) : IRequest<GetForecastsResponse>
{
    public string CorrelationId { get; set; } = "";
}

public record GetForecastsResponse(IEnumerable<WeatherForecast> Payload) : Response<IEnumerable<WeatherForecast>>(Payload);

public record Response<TResponse>(TResponse Payload) : Message;

public record Message
{
    public string? CorrelationId { get; set; }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string Summary);

public sealed class GetForecastsHandler : IRequestHandler<GetForecasts, GetForecastsResponse>
{
    private const int DefaultForecastDays = 5;

    private readonly string[] _summaries =
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<GetForecastsHandler> _logger;

    public GetForecastsHandler(ILogger<GetForecastsHandler> logger) => _logger = logger;

    public async Task<GetForecastsResponse> Handle(GetForecasts request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Correlation ID is '{CorrelationID}'.", request.CorrelationId);

        var numberOfDays = request.NumberOfForecasts > 0
            ? request.NumberOfForecasts
            : DefaultForecastDays;

        var forecasts = Enumerable.Range(1, numberOfDays)
                                  .Select(GetForecast)
                                  .ToArray();

        var response = new GetForecastsResponse(forecasts);

        return await Task.FromResult(response);
    }

    private WeatherForecast GetForecast(int day)
        => new(DateOnly.FromDateTime(DateTime.Now.AddDays(day)),
               Random.Shared.Next(-20, 55),
               _summaries[Random.Shared.Next(_summaries.Length)]);
}

public sealed class CorrelationPreProcessor<TRequest> : IRequestPreProcessor<TRequest>
    where TRequest : Message
{
    private readonly ICorrelationProvider _correlationProvider;

    public CorrelationPreProcessor(ICorrelationProvider correlationProvider) => _correlationProvider = correlationProvider;

    public async Task Process(TRequest request, CancellationToken cancellationToken)
    {
        request.CorrelationId = _correlationProvider.CorrelationId.Value;

        await Task.CompletedTask;
    }
}
#pragma warning restore S3903 // Types should be defined in named namespaces
