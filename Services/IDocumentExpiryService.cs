// IDocumentExpiryService.cs
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

public interface IDocumentExpiryService
{
    DateTime GetCurrentExpiryDate();
    Task UpdateExpiryDate(DateTime newExpiryDate);
    Task ValidateAllDocuments();
    Task<bool> CheckDocumentsValid(string userId);
}

public class DocumentExpiryService : IDocumentExpiryService
{
    private readonly AppDbContext _context;

    public DocumentExpiryService(AppDbContext context)
    {
        _context = context;
    }

    public DateTime GetCurrentExpiryDate()
    {
        return _context.SystemConfigurations
            .FirstOrDefault()?.HealthDocumentsExpiryDate
            ?? throw new Exception("System configuration not found");
    }

    public async Task UpdateExpiryDate(DateTime newExpiryDate)
    {
        var config = await _context.SystemConfigurations.FirstOrDefaultAsync();
        if (config == null)
        {
            config = new SystemConfiguration { HealthDocumentsExpiryDate = newExpiryDate };
            _context.SystemConfigurations.Add(config);
        }
        else
        {
            config.HealthDocumentsExpiryDate = newExpiryDate;
            config.LastUpdated = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await ValidateAllDocuments();
    }

    public async Task ValidateAllDocuments()
    {
        var expiryDate = GetCurrentExpiryDate();
        var allHealthDetails = await _context.HealthDetails.ToListAsync();

        foreach (var health in allHealthDetails)
        {
            health.DocumentsValid = DateTime.UtcNow <= expiryDate;
            health.LastValidationDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> CheckDocumentsValid(string userId)
    {
        var expiryDate = GetCurrentExpiryDate();
        var healthDetails = await _context.HealthDetails
            .FirstOrDefaultAsync(h => h.UserId == userId);

        if (healthDetails == null) return false;

        healthDetails.DocumentsValid = DateTime.UtcNow <= expiryDate;
        healthDetails.LastValidationDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return healthDetails.DocumentsValid;
    }
}