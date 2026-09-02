namespace SharedContracts;

public record ProcessJobCommand(Guid JobId, string Payload, DateTime CreatedAt);
