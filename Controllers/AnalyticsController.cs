using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
            await CalculateDocumentSubmissionTimeline(viewModel);

            // New method to calculate document statistics
            await CalculateDocumentStatistics(viewModel);

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

        // New method to calculate document statistics
        private async Task CalculateDocumentStatistics(HealthAnalyticsViewModel viewModel)
        {
            // Get all submitted health details
            var submittedHealthDetails = await _context.SubmittedHealthDetails.ToListAsync();

            // Initialize document stats
            var docStats = new DocumentStatisticsViewModel();

            // Initialize document type stats
            var documentTypes = new List<string> { "XRay", "MedicalCertificate", "VaccinationRecord", "OtherDocuments" };
            foreach (var docType in documentTypes)
            {
                docStats.DocumentTypeStatistics[docType] = new DocumentTypeStats { DocumentType = docType };
            }

            // Calculate total stats
            docStats.TotalSubmitted = submittedHealthDetails.Count;
            docStats.TotalApproved = submittedHealthDetails.Count(d =>
                d.XRayFileStatus == "Approved" ||
                d.MedicalCertificateStatus == "Approved" ||
                d.VaccinationRecordStatus == "Approved" ||
                d.OtherDocumentsStatus == "Approved");

            docStats.TotalRejected = submittedHealthDetails.Count(d =>
                d.XRayFileStatus == "Rejected" ||
                d.MedicalCertificateStatus == "Rejected" ||
                d.VaccinationRecordStatus == "Rejected" ||
                d.OtherDocumentsStatus == "Rejected");

            docStats.TotalPending = submittedHealthDetails.Count(d =>
                d.XRayFileStatus == "Pending" ||
                d.MedicalCertificateStatus == "Pending" ||
                d.VaccinationRecordStatus == "Pending" ||
                d.OtherDocumentsStatus == "Pending");

            // Calculate document type-specific stats
            // XRay
            var xrayStats = docStats.DocumentTypeStatistics["XRay"];
            xrayStats.Submitted = submittedHealthDetails.Count(d => !string.IsNullOrEmpty(d.XRayFileUrl));
            xrayStats.Approved = submittedHealthDetails.Count(d => !string.IsNullOrEmpty(d.XRayFileUrl) && d.XRayFileStatus == "Approved");
            xrayStats.Rejected = submittedHealthDetails.Count(d => !string.IsNullOrEmpty(d.XRayFileUrl) && d.XRayFileStatus == "Rejected");
            xrayStats.Pending = submittedHealthDetails.Count(d => !string.IsNullOrEmpty(d.XRayFileUrl) && d.XRayFileStatus == "Pending");

            // Medical Certificate
            var medCertStats = docStats.DocumentTypeStatistics["MedicalCertificate"];
            medCertStats.Submitted = submittedHealthDetails.Count(d => !string.IsNullOrEmpty(d.MedicalCertificateUrl));
            medCertStats.Approved = submittedHealthDetails.Count(d => !string.IsNullOrEmpty(d.MedicalCertificateUrl) && d.MedicalCertificateStatus == "Approved");
            medCertStats.Rejected = submittedHealthDetails.Count(d => !string.IsNullOrEmpty(d.MedicalCertificateUrl) && d.MedicalCertificateStatus == "Rejected");
            medCertStats.Pending = submittedHealthDetails.Count(d => !string.IsNullOrEmpty(d.MedicalCertificateUrl) && d.MedicalCertificateStatus == "Pending");

            // Vaccination Record
            var vacRecordStats = docStats.DocumentTypeStatistics["VaccinationRecord"];
            vacRecordStats.Submitted = submittedHealthDetails.Count(d => !string.IsNullOrEmpty(d.VaccinationRecordUrl));
            vacRecordStats.Approved = submittedHealthDetails.Count(d => !string.IsNullOrEmpty(d.VaccinationRecordUrl) && d.VaccinationRecordStatus == "Approved");
            vacRecordStats.Rejected = submittedHealthDetails.Count(d => !string.IsNullOrEmpty(d.VaccinationRecordUrl) && d.VaccinationRecordStatus == "Rejected");
            vacRecordStats.Pending = submittedHealthDetails.Count(d => !string.IsNullOrEmpty(d.VaccinationRecordUrl) && d.VaccinationRecordStatus == "Pending");

            // Other Documents
            var otherDocsStats = docStats.DocumentTypeStatistics["OtherDocuments"];
            otherDocsStats.Submitted = submittedHealthDetails.Count(d => !string.IsNullOrEmpty(d.OtherDocumentsUrl));
            otherDocsStats.Approved = submittedHealthDetails.Count(d => !string.IsNullOrEmpty(d.OtherDocumentsUrl) && d.OtherDocumentsStatus == "Approved");
            otherDocsStats.Rejected = submittedHealthDetails.Count(d => !string.IsNullOrEmpty(d.OtherDocumentsUrl) && d.OtherDocumentsStatus == "Rejected");
            otherDocsStats.Pending = submittedHealthDetails.Count(d => !string.IsNullOrEmpty(d.OtherDocumentsUrl) && d.OtherDocumentsStatus == "Pending");

            // Set the document statistics in the view model
            viewModel.DocumentStatistics = docStats;
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

        private async Task CalculateDocumentSubmissionTimeline(HealthAnalyticsViewModel viewModel)
        {
            // Group submissions by month
            var submissionsByMonth = new Dictionary<string, int>();
            var xraySubmissionsByMonth = new Dictionary<string, int>();
            var medCertSubmissionsByMonth = new Dictionary<string, int>();
            var vacRecordSubmissionsByMonth = new Dictionary<string, int>();
            var otherDocsSubmissionsByMonth = new Dictionary<string, int>();

            // Initialize with past 12 months
            DateTime now = DateTime.Today;
            for (int i = -11; i <= 0; i++)
            {
                DateTime monthDate = now.AddMonths(i);
                string monthName = monthDate.ToString("MMM yyyy");
                submissionsByMonth[monthName] = 0;
                xraySubmissionsByMonth[monthName] = 0;
                medCertSubmissionsByMonth[monthName] = 0;
                vacRecordSubmissionsByMonth[monthName] = 0;
                otherDocsSubmissionsByMonth[monthName] = 0;
            }

            // Get all submission records from the SubmittedHealthDetails table
            var submittedHealthDetails = await _context.SubmittedHealthDetails.ToListAsync();

            foreach (var submission in submittedHealthDetails)
            {
                // Use the appropriate date field from your SubmittedHealthDetails model
                DateTime submissionDate;

                // Check if your model has a SubmissionDate property and handle it appropriately
                // This is just an example - use the actual field name from your model
                if (submission.GetType().GetProperty("SubmissionDate") != null)
                {
                    // If it's a nullable DateTime
                    var dateProperty = submission.GetType().GetProperty("SubmissionDate").GetValue(submission);
                    if (dateProperty != null)
                        submissionDate = (DateTime)dateProperty;
                    else
                        submissionDate = now; // Default if null
                }
                else if (submission.GetType().GetProperty("CreatedDate") != null)
                {
                    // Try another common field name
                    var dateProperty = submission.GetType().GetProperty("CreatedDate").GetValue(submission);
                    if (dateProperty != null)
                        submissionDate = (DateTime)dateProperty;
                    else
                        submissionDate = now; // Default if null
                }
                else
                {
                    // If no date field is found, use current date as fallback
                    submissionDate = now;
                }

                // If we have a submission date within last 12 months, count it
                if (submissionDate >= now.AddMonths(-11))
                {
                    string monthName = submissionDate.ToString("MMM yyyy");

                    // Count total submissions
                    if (submissionsByMonth.ContainsKey(monthName))
                        submissionsByMonth[monthName]++;
                    else
                        submissionsByMonth[monthName] = 1;

                    // Count submissions by document type
                    if (!string.IsNullOrEmpty(submission.XRayFileUrl))
                    {
                        if (xraySubmissionsByMonth.ContainsKey(monthName))
                            xraySubmissionsByMonth[monthName]++;
                        else
                            xraySubmissionsByMonth[monthName] = 1;
                    }

                    if (!string.IsNullOrEmpty(submission.MedicalCertificateUrl))
                    {
                        if (medCertSubmissionsByMonth.ContainsKey(monthName))
                            medCertSubmissionsByMonth[monthName]++;
                        else
                            medCertSubmissionsByMonth[monthName] = 1;
                    }

                    if (!string.IsNullOrEmpty(submission.VaccinationRecordUrl))
                    {
                        if (vacRecordSubmissionsByMonth.ContainsKey(monthName))
                            vacRecordSubmissionsByMonth[monthName]++;
                        else
                            vacRecordSubmissionsByMonth[monthName] = 1;
                    }

                    if (!string.IsNullOrEmpty(submission.OtherDocumentsUrl))
                    {
                        if (otherDocsSubmissionsByMonth.ContainsKey(monthName))
                            otherDocsSubmissionsByMonth[monthName]++;
                        else
                            otherDocsSubmissionsByMonth[monthName] = 1;
                    }
                }
            }

            viewModel.SubmissionsByMonth = submissionsByMonth;
            viewModel.XRaySubmissionsByMonth = xraySubmissionsByMonth;
            viewModel.MedicalCertificateSubmissionsByMonth = medCertSubmissionsByMonth;
            viewModel.VaccinationRecordSubmissionsByMonth = vacRecordSubmissionsByMonth;
            viewModel.OtherDocumentsSubmissionsByMonth = otherDocsSubmissionsByMonth;
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

        // New action to view document statistics
        public async Task<IActionResult> DocumentStatistics()
        {
            var viewModel = await GenerateHealthAnalyticsViewModel();
            return View(viewModel.DocumentStatistics);
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


    }
}