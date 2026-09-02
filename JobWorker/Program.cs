using JobWorker;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddHostedService<Worker>();
builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<ProcessJobConsumer>();

    configurator.UsingRabbitMq((context, rabbitMq) =>
    {
        rabbitMq.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", host =>
        {
            host.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            host.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });

        rabbitMq.ReceiveEndpoint("process-job-queue", endpoint =>
        {
            endpoint.ConfigureConsumer<ProcessJobConsumer>(context);
        });
    });
});

var host = builder.Build();
host.Run();
