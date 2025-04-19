using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WebApp.Data;
using WebApp.Models;
using WebApp.Models.ViewModels;

namespace WebApp.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AnalyticsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public AnalyticsController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Main entry point for analytics
        public async Task<IActionResult> Index()
        {
            var viewModel = await GenerateHealthAnalyticsViewModel();
            return View(viewModel);
        }

        private async Task<HealthAnalyticsViewModel> GenerateHealthAnalyticsViewModel()
        {
            var viewModel = new HealthAnalyticsViewModel();

            var users = await _context.Users
                .Include(u => u.HealthDetails)
                .Include(u => u.PersonalDetails)
                .ToListAsync();

            var userRoles = new Dictionary<string, List<string>>();
            foreach (var user in users)
            {
                userRoles[user.Id] = (await _userManager.GetRolesAsync(user)).ToList();
            }

            var students = users.Where(u => userRoles.ContainsKey(u.Id) && userRoles[u.Id].Contains("Student")).ToList();
            var medicalStaff = users.Where(u => userRoles.ContainsKey(u.Id) && userRoles[u.Id].Contains("MedicalStaff")).ToList();

            viewModel.TotalStudents = students.Count;
            viewModel.TotalMedicalStaff = medicalStaff.Count;
            viewModel.TotalUsers = users.Count;

            CalculateEmergencyContactStats(viewModel, students);
            CalculateBloodTypeStats(viewModel, students);
            CalculateHealthAlertsStats(viewModel, students);
            CalculateMedicalDocumentsStats(viewModel, students);
            CalculateDepartmentStats(viewModel, students);
            CalculateDocumentExpirationStats(viewModel, students);
            CalculateDocumentSubmissionTimeline(viewModel, students);

            viewModel.BloodTypeDistribution = students
                .Where(s => s.HealthDetails != null && !string.IsNullOrEmpty(s.HealthDetails.BloodType))
                .GroupBy(s => s.HealthDetails.BloodType)
                .ToDictionary(g => g.Key, g => g.Count());

            return viewModel;
        }

        private void CalculateEmergencyContactStats(HealthAnalyticsViewModel viewModel, List<Users> students)
        {
            var stat = new RequirementStatistic
            {
                Total = students.Count,
                Completed = students.Count(s =>
                    s.HealthDetails != null &&
                    !string.IsNullOrEmpty(s.HealthDetails.EmergencyContactName) &&
                    !string.IsNullOrEmpty(s.HealthDetails.EmergencyContactPhone))
            };
            viewModel.RequirementStats["EmergencyContact"] = stat;
        }

        private void CalculateBloodTypeStats(HealthAnalyticsViewModel viewModel, List<Users> students)
        {
            var stat = new RequirementStatistic
            {
                Total = students.Count,
                Completed = students.Count(s => s.HealthDetails != null && !string.IsNullOrEmpty(s.HealthDetails.BloodType))
            };
            viewModel.RequirementStats["BloodType"] = stat;
        }

        private void CalculateHealthAlertsStats(HealthAnalyticsViewModel viewModel, List<Users> students)
        {
            int totalAlerts = 0;
            var alertsDistribution = new Dictionary<string, int>();

            foreach (var student in students.Where(s => s.HealthDetails != null && !string.IsNullOrEmpty(s.HealthDetails.HealthAlerts)))
            {
                try
                {
                    var alerts = JsonSerializer.Deserialize<List<string>>(student.HealthDetails.HealthAlerts);
                    if (alerts != null)
                    {
                        totalAlerts += alerts.Count;
                        foreach (var alert in alerts)
                        {
                            if (!alertsDistribution.ContainsKey(alert))
                                alertsDistribution[alert] = 0;
                            alertsDistribution[alert]++;
                        }
                    }
                }
                catch { /* Ignore deserialization errors */ }
            }

            viewModel.TotalHealthAlerts = totalAlerts;
            viewModel.HealthAlertsDistribution = alertsDistribution;
        }

        private void CalculateMedicalDocumentsStats(HealthAnalyticsViewModel viewModel, List<Users> students)
        {
            var xRayStat = new RequirementStatistic
            {
                Total = students.Count,
                Completed = students.Count(s => s.HealthDetails != null && !string.IsNullOrEmpty(s.HealthDetails.XRayFileUrl))
            };
            viewModel.RequirementStats["XRay"] = xRayStat;

            var medCertStat = new RequirementStatistic
            {
                Total = students.Count,
                Completed = students.Count(s => s.HealthDetails != null && !string.IsNullOrEmpty(s.HealthDetails.MedicalCertificateUrl))
            };
            viewModel.RequirementStats["MedicalCertificate"] = medCertStat;

            var vaccinationRecordStat = new RequirementStatistic
            {
                Total = students.Count,
                Completed = students.Count(s => s.HealthDetails != null && !string.IsNullOrEmpty(s.HealthDetails.VaccinationRecordUrl))
            };
            viewModel.RequirementStats["VaccinationRecord"] = vaccinationRecordStat;
        }

        private void CalculateDepartmentStats(HealthAnalyticsViewModel viewModel, List<Users> students)
        {
            // List of all colleges
            var allColleges = new List<string>
            {
                "College of Accountancy",
                "College of Agriculture",
                "College of Arts and Science",
                "College of Business Administration",
                "College of Communication",
                "College of Informatics and Computing Studies",
                "College of Criminology",
                "College of Education",
                "College of Engineering and Architecture",
                "College of Medical Technology",
                "College of Midwifery",
                "College of Music",
                "College of Nursing",
                "College of Physical Therapy",
                "College of Respiratory Therapy",
                "School of International Relations"
            };

            // Initialize dictionary with all colleges (even those with no students)
            var departmentGroups = new Dictionary<string, DepartmentStats>();
            foreach (var college in allColleges)
            {
                departmentGroups[college] = new DepartmentStats
                {
                    TotalStudents = 0,
                    CompletedHealthRequirements = 0
                };
            }

            // Now populate with actual data where available
            foreach (var student in students.Where(s => s.PersonalDetails != null && !string.IsNullOrEmpty(s.PersonalDetails.Department)))
            {
                var dept = student.PersonalDetails.Department;
                if (!departmentGroups.ContainsKey(dept))
                {
                    departmentGroups[dept] = new DepartmentStats
                    {
                        TotalStudents = 0,
                        CompletedHealthRequirements = 0
                    };
                }

                departmentGroups[dept].TotalStudents++;

                if (student.HealthDetails != null &&
                    !string.IsNullOrEmpty(student.HealthDetails.BloodType) &&
                    !string.IsNullOrEmpty(student.HealthDetails.ImmunizationHistory) &&
                    !string.IsNullOrEmpty(student.HealthDetails.EmergencyContactName) &&
                    !string.IsNullOrEmpty(student.HealthDetails.EmergencyContactPhone))
                {
                    departmentGroups[dept].CompletedHealthRequirements++;
                }
            }

            viewModel.DepartmentStatistics = departmentGroups;
        }

        // Action to show detailed student health requirement status
        public async Task<IActionResult> StudentHealthStatus(string requirement = null)
        {
            var users = await _context.Users
                .Include(u => u.HealthDetails)
                .Include(u => u.PersonalDetails)
                .ToListAsync();

            var userRoles = new Dictionary<string, List<string>>();
            foreach (var user in users)
            {
                userRoles[user.Id] = (await _userManager.GetRolesAsync(user)).ToList();
            }

            var students = users.Where(u => userRoles.ContainsKey(u.Id) && userRoles[u.Id].Contains("Student")).ToList();

            // Build view model
            var viewModel = new StudentHealthStatusViewModel
            {
                Students = students.Select(s => new StudentHealthStatusModel
                {
                    Id = s.Id,
                    FullName = s.FullName ?? s.UserName,
                    Email = s.Email,
                    Department = s.PersonalDetails?.Department,
                    ProfilePictureUrl = s.ProfilePictureUrl ?? "/default-profile.png",
                    BloodType = s.HealthDetails?.BloodType,
                    HasEmergencyContact = !string.IsNullOrEmpty(s.HealthDetails?.EmergencyContactName) &&
                                         !string.IsNullOrEmpty(s.HealthDetails?.EmergencyContactPhone),
                    HasXRay = !string.IsNullOrEmpty(s.HealthDetails?.XRayFileUrl),
                    HasMedicalCertificate = !string.IsNullOrEmpty(s.HealthDetails?.MedicalCertificateUrl),
                    HasVaccinationRecord = !string.IsNullOrEmpty(s.HealthDetails?.VaccinationRecordUrl),
                    HealthAlerts = s.HealthDetails?.HealthAlertsList ?? new List<string>(),
                    CompletionPercentage = CalculateCompletionPercentage(s.HealthDetails)
                }).ToList(),
                FilterRequirement = requirement
            };

            return View(viewModel);
        }

        private int CalculateCompletionPercentage(HealthDetails healthDetails)
        {
            if (healthDetails == null)
                return 0;

            int totalFields = 5; // Number of key health requirements we're tracking
            int completedFields = 0;

            // Check each required field
            if (!string.IsNullOrEmpty(healthDetails.BloodType)) completedFields++;
            if (!string.IsNullOrEmpty(healthDetails.EmergencyContactName) &&
                !string.IsNullOrEmpty(healthDetails.EmergencyContactPhone)) completedFields++;
            if (!string.IsNullOrEmpty(healthDetails.ImmunizationHistory)) completedFields++;
            if (!string.IsNullOrEmpty(healthDetails.XRayFileUrl)) completedFields++;
            if (!string.IsNullOrEmpty(healthDetails.MedicalCertificateUrl)) completedFields++;
            if (!string.IsNullOrEmpty(healthDetails.VaccinationRecordUrl)) completedFields++;

            return (int)((double)completedFields / totalFields * 100);
        }

        private void CalculateDocumentExpirationStats(HealthAnalyticsViewModel viewModel, List<Users> students)
        {
            DateTime today = DateTime.Today;

            // Define the current academic year's end date (July of the current or next year)
            int currentYear = today.Month >= 8 ? today.Year + 1 : today.Year; // If we're in/after August, use next year's July
            DateTime academicYearEnd = new DateTime(currentYear, 7, 31);

            // For the "soon to expire" counter, we'll use 60 days before academic year end
            DateTime soonToExpireThreshold = academicYearEnd.AddDays(-60);

            // Count documents expiring at end of academic year
            viewModel.SoonToExpireMedicalCertificates = students.Count(s =>
                s.HealthDetails?.MedicalCertificateUrl != null &&
                (s.HealthDetails?.MedicalCertificateExpiryDate == null || // Count missing expiry dates
                 (s.HealthDetails.MedicalCertificateExpiryDate <= academicYearEnd &&
                  s.HealthDetails.MedicalCertificateExpiryDate >= soonToExpireThreshold)));

            viewModel.SoonToExpireVaccinationRecords = students.Count(s =>
                s.HealthDetails?.VaccinationRecordUrl != null &&
                (s.HealthDetails?.VaccinationRecordExpiryDate == null ||
                 (s.HealthDetails.VaccinationRecordExpiryDate <= academicYearEnd &&
                  s.HealthDetails.VaccinationRecordExpiryDate >= soonToExpireThreshold)));

            viewModel.SoonToExpireXRays = students.Count(s =>
                s.HealthDetails?.XRayFileUrl != null &&
                (s.HealthDetails?.XRayExpiryDate == null ||
                 (s.HealthDetails.XRayExpiryDate <= academicYearEnd &&
                  s.HealthDetails.XRayExpiryDate >= soonToExpireThreshold)));

            // Group by month for the next 12 months for expiring documents 
            var expiringDocsByMonth = new Dictionary<string, int>();

            // Populate next 12 months starting from current month
            for (int i = 0; i < 12; i++)
            {
                DateTime monthStart = today.AddMonths(i).Date.AddDays(1 - today.AddMonths(i).Day);
                DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);
                string monthName = monthStart.ToString("MMM yyyy");

                // Get documents that need renewal in this month (either real expiry date or forced July renewal)
                int expiringDocs = students.Count(s =>
                    (s.HealthDetails?.MedicalCertificateExpiryDate >= monthStart && s.HealthDetails.MedicalCertificateExpiryDate <= monthEnd) ||
                    (s.HealthDetails?.VaccinationRecordExpiryDate >= monthStart && s.HealthDetails.VaccinationRecordExpiryDate <= monthEnd) ||
                    (s.HealthDetails?.XRayExpiryDate >= monthStart && s.HealthDetails.XRayExpiryDate <= monthEnd) ||
                    // Special handling for July (academic year end)
                    (monthName.StartsWith("Jul") && (
                        (s.HealthDetails?.MedicalCertificateUrl != null && (s.HealthDetails?.MedicalCertificateExpiryDate == null || s.HealthDetails.MedicalCertificateExpiryDate.Value.Year == monthStart.Year)) ||
                        (s.HealthDetails?.VaccinationRecordUrl != null && (s.HealthDetails?.VaccinationRecordExpiryDate == null || s.HealthDetails.VaccinationRecordExpiryDate.Value.Year == monthStart.Year)) ||
                        (s.HealthDetails?.XRayFileUrl != null && (s.HealthDetails?.XRayExpiryDate == null || s.HealthDetails.XRayExpiryDate.Value.Year == monthStart.Year))
                    )));

                expiringDocsByMonth[monthName] = expiringDocs;
            }

            viewModel.ExpiringDocumentsByMonth = expiringDocsByMonth;
        }

        private void CalculateDocumentSubmissionTimeline(HealthAnalyticsViewModel viewModel, List<Users> students)
        {
            // Group submissions by month
            var submissionsByMonth = new Dictionary<string, int>();

            // Initialize with past 12 months
            DateTime now = DateTime.Today;
            for (int i = -11; i <= 0; i++)
            {
                DateTime monthDate = now.AddMonths(i);
                string monthName = monthDate.ToString("MMM yyyy");
                submissionsByMonth[monthName] = 0;
            }

            // Add a submission date update in your application logic when documents are uploaded
            foreach (var student in students)
            {
                // Count any document submission that has a value
                if (student.HealthDetails != null)
                {
                    // Try to use the explicit submission date if available
                    DateTime? submissionDate = student.HealthDetails.DocumentSubmissionDate;

                    if (submissionDate == null)
                    {
                        // If no explicit submission date, infer from document URLs (use most recent non-null date)
                        List<DateTime?> docDates = new List<DateTime?>();

                        // Add logic to parse dates from filenames or last modified dates if available
                        // For now we'll just add a fallback
                        if (!string.IsNullOrEmpty(student.HealthDetails.MedicalCertificateUrl) ||
                            !string.IsNullOrEmpty(student.HealthDetails.VaccinationRecordUrl) ||
                            !string.IsNullOrEmpty(student.HealthDetails.XRayFileUrl))
                        {
                            // If we can't determine exact date but have documents, use 60 days ago as estimate
                            submissionDate = now.AddDays(-60);
                        }
                    }

                    // If we have a submission date within last 12 months, count it
                    if (submissionDate.HasValue && submissionDate.Value >= now.AddMonths(-11))
                    {
                        string monthName = submissionDate.Value.ToString("MMM yyyy");
                        if (submissionsByMonth.ContainsKey(monthName))
                            submissionsByMonth[monthName]++;
                        else
                            submissionsByMonth[monthName] = 1;
                    }
                }
            }

            viewModel.SubmissionsByMonth = submissionsByMonth;
        }
    }
}