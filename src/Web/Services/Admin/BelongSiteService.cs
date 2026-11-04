using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Entities.Admin;
using Web.Models.Dtos.Admin;

namespace Web.Services.Admin;

public class BelongSiteService : IBelongSiteService
{
    private readonly ApplicationDbContext _db;

    public BelongSiteService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<BelongSiteListItemDto>> GetAllAsync()
    {
        return await _db.BelongSites
            .AsNoTracking()
            .Where(b => b.IsActive)
            .Select(b => new BelongSiteListItemDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
                Description = b.Description,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            })
            .OrderBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<BelongSiteLookup>> GetLookupsAsync()
        => await _db.BelongSites
            .AsNoTracking()
            .Where(b => b.IsActive)
            .Select(b => new BelongSiteLookup(b.Id, b.Name))
            .ToListAsync();

    public async Task<BelongSite> GetByIdAsync(int id)
    {
        return await _db.BelongSites.FindAsync(id);
    }

    public async Task<BelongSite> CreateAsync(CreateBelongSiteRequestDto request)
    {
        var entity = new BelongSite
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _db.BelongSites.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(int id, UpdateBelongSiteRequestDto request)
    {
        var entity = await _db.BelongSites.FindAsync(id)
            ?? throw new KeyNotFoundException($"BelongSite with Id {id} not found.");

        entity.Name = request.Name;
        entity.Code = request.Code;
        entity.Description = request.Description;
        entity.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
    }

    public async Task SetActiveStatusAsync(int id, bool isActive)
    {
        var entity = await _db.BelongSites.FindAsync(id)
            ?? throw new KeyNotFoundException($"BelongSite with Id {id} not found.");

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
    }
}