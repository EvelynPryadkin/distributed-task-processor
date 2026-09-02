using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedContracts;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<JobApi.AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddMassTransit(configurator =>
{
    configurator.UsingRabbitMq((context, rabbitMq) =>
    {
        rabbitMq.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", host =>
        {
            host.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            host.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });
    });
});

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.MapPost("/api/jobs", async (
    ProcessJobCommand command,
    JobApi.AppDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    CancellationToken cancellationToken) =>
{
    var validationErrors = new Dictionary<string, string[]>();

    if (command.JobId == Guid.Empty)
    {
        validationErrors[nameof(command.JobId)] = ["JobId must not be empty."];
    }

    if (string.IsNullOrWhiteSpace(command.Payload))
    {
        validationErrors[nameof(command.Payload)] = ["Payload is required."];
    }

    if (command.CreatedAt == default)
    {
        validationErrors[nameof(command.CreatedAt)] = ["CreatedAt is required."];
    }

    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    if (await dbContext.Jobs.AnyAsync(job => job.Id == command.JobId, cancellationToken))
    {
        return Results.Conflict(new { message = $"Job {command.JobId} already exists." });
    }

    var job = new JobRecord
    {
        Id = command.JobId,
        Payload = command.Payload,
        Status = "Pending",
        CreatedAt = command.CreatedAt
    };

    try
    {
        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException exception)
        when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        })
    {
        return Results.Conflict(new { message = $"Job {command.JobId} already exists." });
    }

    await publishEndpoint.Publish(command, cancellationToken);
    return Results.Accepted();
});

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
