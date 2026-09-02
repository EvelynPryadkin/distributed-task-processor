namespace SharedContracts;

public class JobRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Payload { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
