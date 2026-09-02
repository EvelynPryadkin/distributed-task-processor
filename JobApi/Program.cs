using MassTransit;
using Microsoft.EntityFrameworkCore;
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

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.MapPost("/api/jobs", async (
    ProcessJobCommand command,
    JobApi.AppDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    CancellationToken cancellationToken) =>
{
    var job = new JobRecord
    {
        Id = command.JobId,
        Payload = command.Payload,
        Status = "Pending",
        CreatedAt = command.CreatedAt
    };

    dbContext.Jobs.Add(job);
    await dbContext.SaveChangesAsync(cancellationToken);

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
