namespace Web.Services.Workflow.Core;

/// <summary>
/// Generates unique sequential application numbers in the format: {PREFIX}-{YYYYMMDD}-{NNN}.
/// </summary>
/// <remarks>
/// IMPORTANT: Must be called within an active Serializable transaction to prevent
/// race conditions when multiple requests are created concurrently.
/// </remarks>
public interface IRequestNumberGenerator
{
    Task<string> GenerateAsync(string formPrefix);
}
