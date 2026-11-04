using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;

namespace Web.Services.Workflow.Core;

public class RequestNumberGenerator : IRequestNumberGenerator
{
    private readonly ApplicationDbContext _context;

    public RequestNumberGenerator(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Format: {formPrefix}-{YYYYMMDD}-{NNN} (e.g. REQ-20260423-001).
    /// Must be called inside a Serializable transaction — see interface remarks.
    /// </remarks>
    public async Task<string> GenerateAsync(string formPrefix)
    {
        var today = DateTime.UtcNow;
        var datePrefix = today.ToString("yyyyMMdd");

        try
        {
            var prefix = $"{formPrefix}-{datePrefix}-";
            var existingNumbers = await _context.ApprovalRequests
                .Where(r => r.ApplicationNo.StartsWith(prefix))
                .Select(r => r.ApplicationNo)
                .ToListAsync();
            var nextNumber = existingNumbers.Select(x =>
                int.TryParse(x[prefix.Length..], out var number) ? number : 0).DefaultIfEmpty(0).Max() + 1;

            return $"{formPrefix}-{datePrefix}-{nextNumber:D3}";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate application number: {ex.Message}", ex);
        }
    }
}
