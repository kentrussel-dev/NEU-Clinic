using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace WebApp.Controllers
{
    [Authorize]
    public class ArchiveController : Controller
    {
        private readonly AppDbContext _context;

        public ArchiveController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var archives = await _context.Archives
                .Include(a => a.User)
                .OrderByDescending(a => a.ArchivedDate)
                .ToListAsync();

            return View(archives);
        }

        [HttpPost]
        public async Task<IActionResult> ArchiveAllExpired()
        {
            // Get all expired documents
            var expiredDocuments = await _context.HealthDetails
                .Where(h => h.LastValidationDate.HasValue &&
                           h.LastValidationDate <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var doc in expiredDocuments)
            {
                await ArchiveDocument(doc);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "All expired documents have been archived successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ArchiveAllDocuments()
        {
            try
            {
                // Get all health documents regardless of expiration
                var allDocuments = await _context.HealthDetails.ToListAsync();

                foreach (var doc in allDocuments)
                {
                    await ArchiveDocument(doc, true);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "All documents have been archived successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error archiving documents: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task ArchiveDocument(HealthDetails doc, bool clearExpiry = false)
        {
            // Archive X-Ray if exists
            if (!string.IsNullOrEmpty(doc.XRayFileUrl))
            {
                _context.Archives.Add(new Archive
                {
                    UserId = doc.UserId,
                    DocumentType = "XRay",
                    FileUrl = doc.XRayFileUrl,
                    OriginalExpiryDate = doc.LastValidationDate ?? DateTime.UtcNow,
                    ArchivedDate = DateTime.UtcNow,
                    ArchivedBy = User.Identity?.Name ?? "System"
                });
                doc.XRayFileUrl = null;
            }

            // Archive Medical Certificate if exists
            if (!string.IsNullOrEmpty(doc.MedicalCertificateUrl))
            {
                _context.Archives.Add(new Archive
                {
                    UserId = doc.UserId,
                    DocumentType = "MedicalCertificate",
                    FileUrl = doc.MedicalCertificateUrl,
                    OriginalExpiryDate = doc.LastValidationDate ?? DateTime.UtcNow,
                    ArchivedDate = DateTime.UtcNow,
                    ArchivedBy = User.Identity?.Name ?? "System"
                });
                doc.MedicalCertificateUrl = null;
            }

            // Archive Vaccination Record if exists
            if (!string.IsNullOrEmpty(doc.VaccinationRecordUrl))
            {
                _context.Archives.Add(new Archive
                {
                    UserId = doc.UserId,
                    DocumentType = "VaccinationRecord",
                    FileUrl = doc.VaccinationRecordUrl,
                    OriginalExpiryDate = doc.LastValidationDate ?? DateTime.UtcNow,
                    ArchivedDate = DateTime.UtcNow,
                    ArchivedBy = User.Identity?.Name ?? "System"
                });
                doc.VaccinationRecordUrl = null;
            }

            doc.DocumentsValid = false;
            if (clearExpiry)
            {
                doc.LastValidationDate = null;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var archive = await _context.Archives.FindAsync(id);
            if (archive == null)
            {
                return NotFound();
            }

            _context.Archives.Remove(archive);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Archived document deleted successfully!";
            return RedirectToAction(nameof(Index));
        }


    }
}