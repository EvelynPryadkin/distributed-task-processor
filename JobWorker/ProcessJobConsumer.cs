using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedContracts;

namespace JobWorker;

public class ProcessJobConsumer(
    AppDbContext dbContext,
    ILogger<ProcessJobConsumer> logger) : IConsumer<ProcessJobCommand>
{
    public async Task Consume(ConsumeContext<ProcessJobCommand> context)
    {
        var job = await dbContext.Jobs.SingleOrDefaultAsync(
            job => job.Id == context.Message.JobId,
            context.CancellationToken);

        if (job is null)
        {
            throw new InvalidOperationException($"Job {context.Message.JobId} was not found.");
        }

        logger.LogInformation(
            "Processing job {JobId} with payload {Payload}",
            job.Id,
            job.Payload);

        await Task.Delay(TimeSpan.FromSeconds(2), context.CancellationToken);

        job.Status = "Completed";
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Job {JobId} processed successfully", job.Id);
    }
}
