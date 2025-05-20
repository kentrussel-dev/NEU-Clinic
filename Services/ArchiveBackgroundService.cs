// Services/ArchiveBackgroundService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebApp.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Services
{
    public class ArchiveBackgroundService : BackgroundService
    {
        private readonly ILogger<ArchiveBackgroundService> _logger;
        private readonly IServiceProvider _services;

        public ArchiveBackgroundService(ILogger<ArchiveBackgroundService> logger, IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Archive Background Service running.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        // Get all expired documents that haven't been archived yet
                        var expiredDocuments = await dbContext.HealthDetails
                            .Where(h => h.LastValidationDate.HasValue &&
                                      h.LastValidationDate <= DateTime.UtcNow &&
                                      (h.XRayFileUrl != null ||
                                       h.MedicalCertificateUrl != null ||
                                       h.VaccinationRecordUrl != null))
                            .ToListAsync(stoppingToken);

                        foreach (var doc in expiredDocuments)
                        {
                            // Archive X-Ray if exists
                            if (!string.IsNullOrEmpty(doc.XRayFileUrl))
                            {
                                dbContext.Archives.Add(new Archive
                                {
                                    UserId = doc.UserId,
                                    DocumentType = "XRay",
                                    FileUrl = doc.XRayFileUrl,
                                    OriginalExpiryDate = doc.LastValidationDate.Value,
                                    ArchivedBy = "System",
                                    ArchivedDate = DateTime.UtcNow
                                });
                                doc.XRayFileUrl = null;
                            }

                            // Archive Medical Certificate if exists
                            if (!string.IsNullOrEmpty(doc.MedicalCertificateUrl))
                            {
                                dbContext.Archives.Add(new Archive
                                {
                                    UserId = doc.UserId,
                                    DocumentType = "MedicalCertificate",
                                    FileUrl = doc.MedicalCertificateUrl,
                                    OriginalExpiryDate = doc.LastValidationDate.Value,
                                    ArchivedBy = "System",
                                    ArchivedDate = DateTime.UtcNow
                                });
                                doc.MedicalCertificateUrl = null;
                            }

                            // Archive Vaccination Record if exists
                            if (!string.IsNullOrEmpty(doc.VaccinationRecordUrl))
                            {
                                dbContext.Archives.Add(new Archive
                                {
                                    UserId = doc.UserId,
                                    DocumentType = "VaccinationRecord",
                                    FileUrl = doc.VaccinationRecordUrl,
                                    OriginalExpiryDate = doc.LastValidationDate.Value,
                                    ArchivedBy = "System",
                                    ArchivedDate = DateTime.UtcNow
                                });
                                doc.VaccinationRecordUrl = null;
                            }

                            doc.DocumentsValid = false;
                        }

                        if (expiredDocuments.Count > 0)
                        {
                            await dbContext.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation($"Archived {expiredDocuments.Count} expired documents.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred archiving documents.");
                }

                // Run once per day
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}